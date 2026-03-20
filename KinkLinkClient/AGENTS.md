# KinkLinkClient Agent Guide

## Project Overview
Dalamud plugin for FFXIV (net10.0-windows) providing client-side functionality with SignalR communication and external plugin integration.

## Build Commands
```bash
# Build project
dotnet build KinkLinkClient.csproj

# Note: Requires Dalamud environment for testing/deployment
# Use DalamudPackager for plugin packaging
```

## Key Dependencies
- Dalamud.NET.Sdk for FFXIV plugin development
- SignalR Client for server communication
- Glamourer.Api and Penumbra.Api for external plugin integration
- Serilog for logging
- Svg.Skia for SVG handling

## Architecture Patterns
- Manager classes for major subsystems (`ConnectionManager`, `WindowManager`, etc.)
- Handler pattern for network message processing
- ImGui-based user interfaces
- Dependency injection for external plugin APIs
- Unsafe code blocks for FFXIV memory access

## Code Style
- Explicit imports (ImplicitUsings disabled)
- Constructor injection with parameter alignment
- Record types for data structures
- `IDrawable` interface for UI components
- Separate files for handlers and managers

## Key Directories
- `Managers/` - Core subsystem managers
- `Handlers/Network/` - Network message handlers
- `Utils/` - Utility classes and helpers
- `Domain/` - Client-side domain models
- `Dependencies/` - External plugin integrations

## External Plugin Integration
- Glamourer for character appearance modifications
- Penumbra for mod management
- CustomizePlus for character customization
- Moodles for status effects

## UI Development
- Custom ImGui interfaces following Dalamud patterns
- Window management system
- Style system with KinkLinkStyle

## Configuration
Plugin targets x64 architecture with unsafe code enabled. Requires Dalamud environment for execution.

## Build Environment Notes
- **WSL Compilation Issues**: Dalamud compilation errors in WSL are expected due to Windows-specific dependencies
- **Manual Verification**: If build errors occur in WSL, ask the user to manually verify in a Windows environment with Dalamud installed
- **Target Platform**: This plugin is specifically designed for Windows FFXIV environments