# KinkLinkServer Agent Guide

## Project Overview
ASP.NET Core SignalR server (net9.0) for FFXIV plugin communication with JWT authentication and PostgreSQL database.

## Build Commands
```bash
# Build project
dotnet build KinkLinkServer.csproj

# Run server
dotnet run --project KinkLinkServer.csproj

# Run with specific configuration
dotnet run --project KinkLinkServer.csproj --configuration Release
```

## Key Dependencies
- ASP.NET Core 9.0 with SignalR and MessagePack protocol
- PostgreSQL with Npgsql and DbUp for migrations
- JWT Bearer authentication
- Serilog for structured logging
- Prometheus metrics collection

## Architecture Patterns
- Constructor injection for all dependencies
- Handler pattern for SignalR operations (e.g., `AddFriendHandler`)
- Separate `*Service` classes for business logic
- Hub methods authorize with JWT tokens
- Database migrations embedded as SQL resources

## Code Style
- File-scoped namespaces
- `record` types for domain models
- Align constructor parameters vertically
- Use expression body members for single-line methods
- Private fields use `_camelCase` prefix

## Key Directories
- `SignalR/Hubs/` - Hub implementations
- `SignalR/Handlers/` - Request handlers
- `Services/` - Business logic services
- `Domain/` - Domain models and interfaces
- `Infrastructure/` - Middleware and utilities

## Testing
Tests are in KinkLinkServerTests project using NUnit framework.

## Configuration
Server runs on port 5006, designed for reverse proxy with TLS termination.