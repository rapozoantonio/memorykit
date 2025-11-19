# Deployment Guide

## Prerequisites

- Azure subscription
- Azure CLI installed
- .NET 9.0 SDK
- Docker (for containerization)

## Azure Resources Required

### 1. Azure App Service
```bash
az appservice plan create \
  --name memorykit-plan \
  --resource-group memorykit-rg \
  --sku P1V2

az webapp create \
  --resource-group memorykit-rg \
  --plan memorykit-plan \
  --name memorykit-api
```

### 2. Azure Cache for Redis
```bash
az redis create \
  --resource-group memorykit-rg \
  --name memorykit-redis \
  --location eastus \
  --sku premium \
  --vm-size p1
```

### 3. Azure Storage Account
```bash
az storage account create \
  --resource-group memorykit-rg \
  --name memorykitstorage \
  --sku Standard_ZRS

# For Table Storage
az storage account create \
  --resource-group memorykit-rg \
  --name memorykittables \
  --sku Standard_ZRS
```

### 4. Azure AI Search
```bash
az search service create \
  --resource-group memorykit-rg \
  --name memorykit-search \
  --location eastus \
  --sku standard
```

### 5. Azure OpenAI
```bash
az cognitiveservices account create \
  --resource-group memorykit-rg \
  --name memorykit-openai \
  --kind OpenAI \
  --sku s0 \
  --location eastus \
  --custom-domain memorykit
```

## Environment Configuration

Create `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Azure": {
    "Redis": {
      "ConnectionString": "memorykit-redis.redis.cache.windows.net:6379,password=..."
    },
    "Storage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=memorykitstorage;..."
    },
    "TableStorage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=memorykittables;..."
    },
    "AiSearch": {
      "ServiceName": "memorykit-search",
      "ApiKey": "..."
    }
  },
  "AzureOpenAI": {
    "Endpoint": "https://memorykit-openai.openai.azure.com/",
    "ApiKey": "...",
    "DeploymentName": "gpt-4",
    "EmbeddingDeployment": "text-embedding-3-small"
  },
  "Authentication": {
    "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
    "Audience": "api://memorykit"
  }
}
```

## Building & Deployment

### Using GitHub Actions

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
      
      - name: Test
        run: dotnet test --no-build --verbosity normal
      
      - name: Publish
        run: dotnet publish src/MemoryKit.API/MemoryKit.API.csproj -c Release -o ./publish
      
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: memorykit-api
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          package: ./publish
```

### Docker Deployment

Create `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY . .
RUN dotnet restore
RUN dotnet publish src/MemoryKit.API/MemoryKit.API.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 80 443
ENTRYPOINT ["dotnet", "MemoryKit.API.dll"]
```

Build and push:
```bash
docker build -t memorykit:latest .
docker tag memorykit:latest memorykit.azurecr.io/memorykit:latest
docker push memorykit.azurecr.io/memorykit:latest
```

## Scaling Configuration

### App Service Auto-Scaling

```bash
az monitor autoscale create \
  --resource-group memorykit-rg \
  --resource memorykit-api \
  --resource-type "Microsoft.Web/serverfarms" \
  --name memorykit-autoscale \
  --min-count 2 \
  --max-count 10 \
  --count 2

az monitor autoscale rule create \
  --resource-group memorykit-rg \
  --autoscale-name memorykit-autoscale \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1
```

## Monitoring

### Application Insights

```bash
az monitor app-insights component create \
  --resource-group memorykit-rg \
  --application memorykit \
  --location eastus
```

Update `appsettings.json`:
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "..."
  }
}
```

## Health Checks

Configure health endpoint monitoring in Azure:

```bash
az webapp config set \
  --resource-group memorykit-rg \
  --name memorykit-api \
  --health-check-path /health
```

## Database Migrations

For schema updates:
```bash
cd src/MemoryKit.Infrastructure
dotnet ef database update
```

## Backup Strategy

### Data Backup

```bash
# Redis backup
az redis export \
  --resource-group memorykit-rg \
  --name memorykit-redis \
  --files prefix_0.rdb

# Storage account backup
az storage account blob service-properties \
  update \
  --resource-group memorykit-rg \
  --account-name memorykitstorage \
  --enable-delete-retention true
```

## Security Hardening

1. **Enable HTTPS only**
```bash
az webapp update \
  --resource-group memorykit-rg \
  --name memorykit-api \
  --https-only true
```

2. **Configure SSL certificate**
```bash
az webapp config ssl upload \
  --resource-group memorykit-rg \
  --name memorykit-api \
  --certificate-file memorykit.pfx \
  --certificate-password <password>
```

3. **Enable authentication**
- Configure Azure AD integration
- Set up API scopes and roles

4. **Network security**
- Configure VNet integration
- Set up Application Gateway with WAF
- Restrict storage account access

## Performance Optimization

### Redis Configuration
- Enable clustering for high availability
- Configure eviction policy: `allkeys-lru`
- Set timeout: 300 seconds

### Table Storage Optimization
- Partition by user ID for parallel access
- Batch operations where possible
- Use table partitioning strategy

### AI Search Optimization
- Enable query caching
- Configure semantic search indexing
- Optimize index refresh frequency

## Monitoring & Alerting

Set up alerts for:
- CPU > 80%
- Memory > 85%
- HTTP errors > 1%
- Response time > 2s
- Redis evictions > 100/min

## Rollback Strategy

1. Keep previous app version in staging slot
2. Use deployment slots for zero-downtime deployments
3. Maintain database migration rollback scripts

## Cost Optimization

| Resource | Configuration | Monthly Cost |
|----------|---|---|
| App Service | P1V2 (2-10 instances) | ~$75 |
| Redis | Premium 6GB | ~$75 |
| Table Storage | Pay-as-you-go | ~$2 |
| Blob Storage | Hot tier | ~$1 |
| AI Search | Standard | ~$250 |
| Azure OpenAI | Pay-per-token | ~$50 |
| **Total** | | **~$453** |

## Compliance & GDPR

- Enable Azure Storage encryption at rest
- Configure data retention policies
- Implement GDPR-compliant deletion endpoints
- Set up audit logging
- Configure access reviews

## Support & Troubleshooting

- Enable diagnostic logging to Application Insights
- Configure error alerting
- Set up SLA monitoring
- Create runbooks for common issues

