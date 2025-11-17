#!/bin/bash
# ============================================================================
# MemoryKit Azure Infrastructure Deployment Script
# ============================================================================

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
RESOURCE_GROUP_NAME="${RESOURCE_GROUP_NAME:-memorykit-rg}"
LOCATION="${LOCATION:-eastus}"
ENVIRONMENT="${ENVIRONMENT:-dev}"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}MemoryKit Azure Deployment${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Environment: ${ENVIRONMENT}"
echo "Resource Group: ${RESOURCE_GROUP_NAME}"
echo "Location: ${LOCATION}"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Please install it from: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Login check
echo -e "${YELLOW}Checking Azure login status...${NC}"
az account show > /dev/null 2>&1 || {
    echo -e "${YELLOW}Not logged in. Please login to Azure...${NC}"
    az login
}

SUBSCRIPTION=$(az account show --query name -o tsv)
echo -e "${GREEN}Using subscription: ${SUBSCRIPTION}${NC}"
echo ""

# Create resource group if it doesn't exist
echo -e "${YELLOW}Creating resource group if not exists...${NC}"
az group create \
    --name "${RESOURCE_GROUP_NAME}" \
    --location "${LOCATION}" \
    --output none

echo -e "${GREEN}Resource group ready: ${RESOURCE_GROUP_NAME}${NC}"
echo ""

# Deploy infrastructure
echo -e "${YELLOW}Deploying infrastructure...${NC}"
echo "This may take 10-15 minutes..."
echo ""

DEPLOYMENT_NAME="memorykit-deployment-$(date +%Y%m%d-%H%M%S)"

az deployment group create \
    --name "${DEPLOYMENT_NAME}" \
    --resource-group "${RESOURCE_GROUP_NAME}" \
    --template-file main.bicep \
    --parameters parameters.${ENVIRONMENT}.json \
    --output json > deployment-output.json

echo -e "${GREEN}Deployment completed successfully!${NC}"
echo ""

# Extract outputs
echo -e "${YELLOW}Deployment Outputs:${NC}"
echo "-----------------------------------"

APP_URL=$(jq -r '.properties.outputs.appServiceUrl.value' deployment-output.json)
APP_INSIGHTS_KEY=$(jq -r '.properties.outputs.appInsightsInstrumentationKey.value' deployment-output.json)
APP_INSIGHTS_CONN=$(jq -r '.properties.outputs.appInsightsConnectionString.value' deployment-output.json)

echo "App Service URL: ${APP_URL}"
echo "Application Insights Key: ${APP_INSIGHTS_KEY}"
echo "Application Insights Connection: ${APP_INSIGHTS_CONN}"
echo ""

# Save outputs to .env file
echo -e "${YELLOW}Saving outputs to .env.${ENVIRONMENT}...${NC}"
cat > "../.env.${ENVIRONMENT}" <<EOF
# MemoryKit Azure Configuration
# Generated on: $(date)

AZURE_SUBSCRIPTION_ID=$(az account show --query id -o tsv)
AZURE_RESOURCE_GROUP=${RESOURCE_GROUP_NAME}
AZURE_LOCATION=${LOCATION}

APP_SERVICE_URL=${APP_URL}
APPLICATIONINSIGHTS_CONNECTION_STRING=${APP_INSIGHTS_CONN}
APPLICATIONINSIGHTS_INSTRUMENTATION_KEY=${APP_INSIGHTS_KEY}

# Update these values in Azure App Service Configuration after deployment
EOF

echo -e "${GREEN}Configuration saved to .env.${ENVIRONMENT}${NC}"
echo ""

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Next steps:"
echo "1. Review the outputs above"
echo "2. Configure API keys in Azure App Service"
echo "3. Deploy your application code"
echo ""
echo "To deploy the API:"
echo "  cd ../src/MemoryKit.API"
echo "  dotnet publish -c Release -o ./publish"
echo "  az webapp deployment source config-zip --resource-group ${RESOURCE_GROUP_NAME} --name <app-service-name> --src ./publish.zip"
echo ""
