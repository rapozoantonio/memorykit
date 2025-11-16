# MemoryKit Deployment Guide

## Prerequisites

Before deploying MemoryKit, ensure you have:

- Azure subscription with appropriate permissions
- .NET 8.0 SDK installed
- Azure CLI installed and configured
- Git for version control

## Local Development

### 1. Clone the Repository

```bash
git clone https://github.com/rapozoantonio/memorykit.git
cd memorykit
```

### 2. Configure Settings

Create `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379",
    "AzureStorage": "UseDevelopmentStorage=true"
  },
  "AzureSearch": {
    "Endpoint": "https://localhost:8080",
    "ApiKey": "dev-key"
  },
  "SemanticKernel": {
    "Endpoint": "https://api.openai.com/v1",
    "ApiKey": "your-api-key"
  }
}
```

### 3. Run with In-Memory Storage

```bash
cd src/MemoryKit.API
dotnet run --environment Development
```

The API will be available at `https://localhost:5001`

## Azure Deployment

### 1. Provision Azure Resources

```bash
# Login to Azure
az login

# Create resource group
az group create \
  --name memorykit-rg \
  --location eastus

# Create Azure Storage
az storage account create \
  --name memorykitstorage \
  --resource-group memorykit-rg \
  --location eastus \
  --sku Standard_LRS

# Create Azure Cache for Redis
az redis create \
  --name memorykit-redis \
  --resource-group memorykit-rg \
  --location eastus \
  --sku Basic \
  --vm-size c0

# Create Azure AI Search
az search service create \
  --name memorykit-search \
  --resource-group memorykit-rg \
  --location eastus \
  --sku basic
```

### 2. Configure Application Settings

Update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "memorykit-redis.redis.cache.windows.net:6380,ssl=true,password={key}",
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=memorykitstorage;AccountKey={key}"
  },
  "AzureSearch": {
    "Endpoint": "https://memorykit-search.search.windows.net",
    "ApiKey": "{key}"
  },
  "SemanticKernel": {
    "Endpoint": "https://your-openai.openai.azure.com/",
    "ApiKey": "{key}"
  }
}
```

### 3. Deploy to Azure App Service

```bash
# Create App Service Plan
az appservice plan create \
  --name memorykit-plan \
  --resource-group memorykit-rg \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name memorykit-api \
  --resource-group memorykit-rg \
  --plan memorykit-plan \
  --runtime "DOTNET|8.0"

# Deploy application
dotnet publish -c Release
cd bin/Release/net8.0/publish
zip -r ../../../deploy.zip .
az webapp deployment source config-zip \
  --resource-group memorykit-rg \
  --name memorykit-api \
  --src ../../../deploy.zip
```

### 4. Configure Environment Variables

```bash
az webapp config appsettings set \
  --resource-group memorykit-rg \
  --name memorykit-api \
  --settings \
    ConnectionStrings__Redis="your-redis-connection" \
    ConnectionStrings__AzureStorage="your-storage-connection" \
    AzureSearch__Endpoint="your-search-endpoint" \
    AzureSearch__ApiKey="your-search-key"
```

## Docker Deployment

### 1. Build Docker Image

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MemoryKit.API/MemoryKit.API.csproj", "MemoryKit.API/"]
RUN dotnet restore "MemoryKit.API/MemoryKit.API.csproj"
COPY . .
WORKDIR "/src/src/MemoryKit.API"
RUN dotnet build "MemoryKit.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MemoryKit.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MemoryKit.API.dll"]
```

```bash
# Build image
docker build -t memorykit:1.0.0 .

# Run container
docker run -d \
  -p 8080:80 \
  -e ConnectionStrings__Redis="your-connection" \
  -e ConnectionStrings__AzureStorage="your-connection" \
  memorykit:1.0.0
```

### 2. Deploy to Azure Container Registry

```bash
# Create ACR
az acr create \
  --name memorykitacr \
  --resource-group memorykit-rg \
  --sku Basic \
  --admin-enabled true

# Login to ACR
az acr login --name memorykitacr

# Tag and push image
docker tag memorykit:1.0.0 memorykitacr.azurecr.io/memorykit:1.0.0
docker push memorykitacr.azurecr.io/memorykit:1.0.0
```

## Kubernetes Deployment

### deployment.yaml

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: memorykit-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: memorykit-api
  template:
    metadata:
      labels:
        app: memorykit-api
    spec:
      containers:
      - name: memorykit-api
        image: memorykitacr.azurecr.io/memorykit:1.0.0
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__Redis
          valueFrom:
            secretKeyRef:
              name: memorykit-secrets
              key: redis-connection
---
apiVersion: v1
kind: Service
metadata:
  name: memorykit-api-service
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 80
  selector:
    app: memorykit-api
```

```bash
kubectl apply -f deployment.yaml
```

## Monitoring and Logging

### Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app memorykit-insights \
  --location eastus \
  --resource-group memorykit-rg
```

Add to `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "your-connection-string"
  }
}
```

### Health Checks

MemoryKit includes built-in health check endpoints:

- `/health` - Overall health
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

## Security Considerations

1. **Always use HTTPS** in production
2. **Store secrets** in Azure Key Vault
3. **Enable Azure AD** authentication
4. **Configure CORS** appropriately
5. **Implement rate limiting**
6. **Enable DDoS protection**
7. **Use managed identities** for Azure resources

## Scaling

### Horizontal Scaling

```bash
az webapp scale \
  --resource-group memorykit-rg \
  --name memorykit-api \
  --instance-count 3
```

### Auto-scaling

```bash
az monitor autoscale create \
  --resource-group memorykit-rg \
  --resource memorykit-api \
  --min-count 2 \
  --max-count 10 \
  --count 2
```

## Backup and Disaster Recovery

1. **Azure Storage**: Enable geo-redundancy
2. **Redis**: Configure persistence
3. **AI Search**: Regular index backups
4. **Database**: Automated backups with point-in-time restore

## Troubleshooting

### View Logs

```bash
# Stream logs
az webapp log tail \
  --resource-group memorykit-rg \
  --name memorykit-api

# Download logs
az webapp log download \
  --resource-group memorykit-rg \
  --name memorykit-api
```

### Common Issues

1. **Redis Connection Timeout**: Check firewall rules and SSL settings
2. **Storage Access Denied**: Verify managed identity permissions
3. **High Latency**: Enable CDN for static content
4. **Memory Issues**: Adjust app service plan size

## CI/CD Pipeline

GitHub Actions workflow example:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.x'
    - name: Build
      run: dotnet build --configuration Release
    - name: Test
      run: dotnet test
    - name: Publish
      run: dotnet publish -c Release -o publish
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: memorykit-api
        package: publish
```

## Cost Optimization

- Use Azure Reserved Instances for 40-60% savings
- Implement caching to reduce AI Search queries
- Use Azure Functions for batch processing
- Monitor and optimize storage access patterns
- Implement tiered storage for cold data

## Support

For deployment assistance, contact:
- GitHub Issues: https://github.com/rapozoantonio/memorykit/issues
- Email: support@memorykit.example.com
