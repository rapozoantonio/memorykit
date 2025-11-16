# 🧠 MemoryKit

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](https://github.com/rapozoantonio/memorykit/releases)

**MemoryKit** is a cognitive-inspired memory management system for AI applications, designed to emulate human memory structures for intelligent conversation and context retention.

## 🎯 Overview

MemoryKit implements a hierarchical memory system inspired by cognitive neuroscience, providing:

- **Working Memory**: Short-term, active conversation context (Redis-based)
- **Episodic Memory**: Long-term storage of conversation episodes (Azure AI Search)
- **Semantic Memory**: Extracted facts and knowledge (Azure Storage)
- **Procedural Memory**: Learned patterns and behaviors (Azure Table Storage)

## ✨ Features

- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **Scalable**: Azure-native implementation with cloud-ready infrastructure
- **Intelligent**: AI-powered importance scoring and query classification
- **Flexible**: In-memory implementations available for testing and development
- **Type-Safe**: Built with C# and .NET for enterprise-grade reliability

## 🚀 Quick Start

```bash
# Clone the repository
git clone https://github.com/rapozoantonio/memorykit.git
cd memorykit

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the API
cd src/MemoryKit.API
dotnet run
```

## 📋 Requirements

- .NET 8.0 or later
- Azure subscription (for production deployment)
- Redis (for working memory)
- Azure Storage Account
- Azure AI Search

## 🏗️ Architecture

MemoryKit follows Clean Architecture principles with four main layers:

1. **Domain Layer**: Core entities, value objects, and domain logic
2. **Application Layer**: Use cases, DTOs, and application services
3. **Infrastructure Layer**: External service implementations (Azure, Semantic Kernel)
4. **API Layer**: RESTful API endpoints and controllers

## 📚 Documentation

- [Architecture Overview](docs/ARCHITECTURE.md)
- [API Reference](docs/API.md)
- [Deployment Guide](docs/DEPLOYMENT.md)
- [Cognitive Model](docs/COGNITIVE_MODEL.md)

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👤 Author

**Antonio Rapozo**

- GitHub: [@rapozoantonio](https://github.com/rapozoantonio)

## 🙏 Acknowledgments

This project draws inspiration from cognitive neuroscience research on human memory systems.

---

**Status**: Implementation Ready  
**Version**: 1.0.0  
**Last Updated**: November 16, 2025
