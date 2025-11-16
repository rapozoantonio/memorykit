# GitHub Workflows

## CI/CD Pipeline

This directory contains GitHub Actions workflows for continuous integration and deployment.

### ci.yml

Runs on every push and pull request:
- Build solution
- Run unit tests
- Code analysis
- Build Docker image

### release.yml

Runs on version tags:
- Build and test
- Create GitHub release
- Push Docker image to registry
- Deploy to Azure

## Secrets Required

Configure these in GitHub Settings:

- `AZURE_PUBLISH_PROFILE` - App Service publish profile
- `REGISTRY_USERNAME` - Container registry username
- `REGISTRY_PASSWORD` - Container registry password
- `AZURE_CREDENTIALS` - Azure service principal credentials

## Local Development

### Pre-commit Hooks

Install pre-commit hooks:
```bash
pip install pre-commit
pre-commit install
```

### Testing

Run all tests:
```bash
dotnet test
```

Run specific test category:
```bash
dotnet test --filter Category=Unit
```

## Version Management

Use semantic versioning: `MAJOR.MINOR.PATCH`

Tag releases:
```bash
git tag v1.0.0
git push origin v1.0.0
```

