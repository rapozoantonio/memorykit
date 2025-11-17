// ============================================================================
// MemoryKit Azure Infrastructure (Bicep)
// Production-ready deployment for Azure
// ============================================================================

@description('Environment name (dev, staging, prod)')
param environment string = 'dev'

@description('Azure region location')
param location string = resourceGroup().location

@description('Resource name prefix')
param namePrefix string = 'memorykit'

// ============================================================================
// Variables
// ============================================================================
var uniqueSuffix = uniqueString(resourceGroup().id)
var appServicePlanName = '${namePrefix}-plan-${environment}-${uniqueSuffix}'
var appServiceName = '${namePrefix}-api-${environment}-${uniqueSuffix}'
var redisCacheName = '${namePrefix}-cache-${environment}-${uniqueSuffix}'
var storageAccountName = '${namePrefix}storage${environment}${uniqueSuffix}'
var searchServiceName = '${namePrefix}-search-${environment}-${uniqueSuffix}'
var openAIName = '${namePrefix}-openai-${environment}-${uniqueSuffix}'
var appInsightsName = '${namePrefix}-insights-${environment}-${uniqueSuffix}'
var keyVaultName = '${namePrefix}-kv-${environment}-${uniqueSuffix}'

// ============================================================================
// 1. Application Insights (Monitoring)
// ============================================================================
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${namePrefix}-logs-${environment}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ============================================================================
// 2. App Service Plan
// ============================================================================
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: environment == 'prod' ? 'P1V2' : 'B1'
    tier: environment == 'prod' ? 'PremiumV2' : 'Basic'
    capacity: environment == 'prod' ? 2 : 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// ============================================================================
// 3. App Service (API)
// ============================================================================
resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsights__InstrumentationKey'
          value: applicationInsights.properties.InstrumentationKey
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
        }
        {
          name: 'AZURE_REDIS_CONNECTION_STRING'
          value: '${redisCache.properties.hostName}:${redisCache.properties.sslPort},password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
        {
          name: 'AZURE_STORAGE_CONNECTION_STRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${az.environment().suffixes.storage}'
        }
        {
          name: 'AZURE_SEARCH_ENDPOINT'
          value: 'https://${searchService.name}.search.windows.net'
        }
        {
          name: 'AZURE_SEARCH_API_KEY'
          value: searchService.listAdminKeys().primaryKey
        }
        {
          name: 'AZURE_OPENAI_ENDPOINT'
          value: openAI.properties.endpoint
        }
        {
          name: 'AZURE_OPENAI_API_KEY'
          value: openAI.listKeys().key1
        }
        {
          name: 'AZURE_OPENAI_DEPLOYMENT_NAME'
          value: 'gpt-4'
        }
        {
          name: 'AZURE_OPENAI_EMBEDDING_DEPLOYMENT'
          value: 'text-embedding-ada-002'
        }
      ]
    }
  }
}

// ============================================================================
// 4. Redis Cache (Working Memory)
// ============================================================================
resource redisCache 'Microsoft.Cache/redis@2022-06-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: environment == 'prod' ? 'Premium' : 'Basic'
      family: environment == 'prod' ? 'P' : 'C'
      capacity: environment == 'prod' ? 1 : 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

// ============================================================================
// 5. Storage Account (Blob + Table)
// ============================================================================
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: take(storageAccountName, 24)
  location: location
  sku: {
    name: environment == 'prod' ? 'Standard_ZRS' : 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

// Blob container for episodic memory
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storageAccount
  name: 'default'
}

resource conversationsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = {
  parent: blobService
  name: 'conversations'
  properties: {
    publicAccess: 'None'
  }
}

// Table service for facts and patterns
resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2022-09-01' = {
  parent: storageAccount
  name: 'default'
}

resource factsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2022-09-01' = {
  parent: tableService
  name: 'facts'
}

resource patternsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2022-09-01' = {
  parent: tableService
  name: 'patterns'
}

// ============================================================================
// 6. Azure AI Search (Episodic Memory Vector Search)
// ============================================================================
resource searchService 'Microsoft.Search/searchServices@2022-09-01' = {
  name: searchServiceName
  location: location
  sku: {
    name: environment == 'prod' ? 'standard' : 'basic'
  }
  properties: {
    replicaCount: environment == 'prod' ? 3 : 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// ============================================================================
// 7. Azure OpenAI (Embeddings & Completions)
// ============================================================================
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIName
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: 'Enabled'
  }
}

// GPT-4 deployment
resource gpt4Deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'gpt-4'
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4'
      version: '0613'
    }
  }
}

// Text embedding deployment
resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'text-embedding-ada-002'
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
  }
  dependsOn: [
    gpt4Deployment
  ]
}

// ============================================================================
// 8. Key Vault (Secrets Management)
// ============================================================================
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: take(keyVaultName, 24)
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}

// ============================================================================
// Outputs
// ============================================================================
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output appInsightsConnectionString string = applicationInsights.properties.ConnectionString
output redisHostName string = redisCache.properties.hostName
output storageAccountName string = storageAccount.name
output searchServiceName string = searchService.name
output openAIEndpoint string = openAI.properties.endpoint
output keyVaultUri string = keyVault.properties.vaultUri
