# Screenbox Project Structure

This document provides a comprehensive overview of the Screenbox project's architecture and organization.

## Overview

Screenbox is a modern media player for Windows built using the Universal Windows Platform (UWP) and LibVLCSharp. The project follows a clean architecture pattern with clear separation between the UI layer and business logic.

## Solution Structure

The solution contains two main projects:

```
Screenbox.sln
├── Screenbox/           # Main UWP application (UI layer)
└── Screenbox.Core/      # Core business logic library
```

### Main Project: Screenbox
The main UWP application containing all user interface components and platform-specific code.

### Core Library: Screenbox.Core
Contains the business logic, services, and view models that can be shared across different platforms.

## Project Architecture

The application follows the **MVVM (Model-View-ViewModel)** pattern with **Dependency Injection** for loose coupling:

- **Models**: Data structures and business entities
- **Views**: XAML pages and user controls  
- **ViewModels**: Presentation logic and data binding
- **Services**: Business logic and data access
- **Messages**: Communication between components via messaging

## Detailed Directory Structure

### Root Directory

```
Screenbox/
├── .config/                # Development tool configurations
│   └── dotnet-tools.json   # .NET CLI tools (XAML Styler)
├── .github/                # GitHub configuration
│   ├── ISSUE_TEMPLATE/     # Bug report and feature request templates
│   └── workflows/          # CI/CD GitHub Actions
├── assets/                 # Documentation and marketing assets
├── docs/                   # Project documentation
├── scripts/                # Build and maintenance scripts
├── Screenbox/              # Main UWP application
├── Screenbox.Core/         # Core business logic library
├── README.md               # Project overview and quick start
├── LICENSE                 # GPL-3.0 license
├── NOTICE.md               # Third-party software notices
├── PRIVACY.md              # Privacy policy
├── crowdin.yml             # Translation service configuration
├── nuget.config            # NuGet package source configuration
└── Screenbox.sln           # Visual Studio solution file
```

### Screenbox Project (UI Layer)

```
Screenbox/
├── Assets/                 # Application assets
│   ├── Fonts/              # Custom font files
│   ├── Icons/              # Application and file association icons
│   └── Visualizers/        # Audio visualization components
├── Commands/               # UI command implementations
├── Controls/               # Custom user controls and components
│   ├── Animations/         # Animation definitions
│   ├── Extensions/         # Control extension methods
│   ├── Interactions/       # Behavior implementations
│   └── Templates/          # XAML data templates
├── Converters/             # XAML value converters
├── Helpers/                # UI utility classes
├── Pages/                  # Application pages
│   └── Search/             # Search-related pages
├── Services/               # UI-specific services
├── Strings/                # Localization resources
│   ├── en-US/              # English (default language)
│   └── [locale]/           # Other supported languages
├── Styles/                 # XAML styling resources
├── App.xaml[.cs]           # Application entry point
├── Package.appxmanifest    # UWP application manifest
└── Screenbox.csproj        # Project configuration
```

### Screenbox.Core Project (Business Logic)

```
Screenbox.Core/
├── Common/                 # Shared interfaces and utilities
├── Enums/                  # Application enumerations
├── Events/                 # Custom event argument classes
├── Factories/              # Object creation factories
├── Helpers/                # Utility classes and extensions
├── Messages/               # MVVM messaging system
├── Models/                 # Data models and DTOs
├── Playback/               # Media playback components
├── Services/               # Business logic services
├── ViewModels/             # MVVM view models
└── Screenbox.Core.csproj   # Project configuration
```

## Key Components

### Services Architecture

The application uses **dependency injection** for service management. Core services include:

#### Core Services (Screenbox.Core)
- **`IFilesService`**: File system operations and media discovery
- **`ILibraryService`**: Media library management and indexing
- **`ISearchService`**: Content search functionality
- **`ICastService`**: Chromecast and media streaming
- **`ISettingsService`**: Application settings management
- **`LibVlcService`**: VLC media player integration
- **`INotificationService`**: User notifications
- **`IWindowService`**: Window management operations

#### UI Services (Screenbox)
- **`ResourceService`**: Localization and resource management

### MVVM Pattern Implementation

The application implements MVVM using **CommunityToolkit.Mvvm**:

#### ViewModels
- **Page ViewModels**: `MainPageViewModel`, `PlayerPageViewModel`, `SettingsPageViewModel`, etc.
- **Control ViewModels**: `PlayerControlsViewModel`, `SeekBarViewModel`, `VolumeViewModel`
- **Content ViewModels**: `MediaViewModel`, `AlbumViewModel`, `ArtistViewModel`

#### Models
- **Media Models**: `MediaInfo`, `VideoInfo`, `MusicInfo`
- **Library Models**: `PlaylistInfo`, `PersistentStorageLibrary`
- **System Models**: `Language`, `Renderer`

#### Messaging System
Uses **CommunityToolkit.Mvvm** messaging for decoupled communication:
- **Playback Messages**: `PlayMediaMessage`, `TogglePlayPauseMessage`
- **UI Messages**: `PlayerControlsVisibilityChangedMessage`, `SettingsChangedMessage`
- **Notification Messages**: Various notification types

### Media Playback Architecture

Built on **LibVLCSharp** with custom abstractions:

- **`IMediaPlayer`**: Media player abstraction
- **`VlcMediaPlayer`**: VLC implementation
- **`PlaybackItem`**: Media item wrapper
- **Track Lists**: Audio, video, and subtitle track management
- **`DisplayRequestTracker`**: Screen wake management

### Localization System

Multi-language support through:
- **`.resw` files**: Windows resource files for strings
- **Crowdin integration**: Automated translation workflow
- **`ResourceService`**: Runtime resource access
- **IETF language tags**: Standardized locale identification

## Technology Stack

### Core Technologies
- **UWP (Universal Windows Platform)**: Windows application framework
- **C# 10.0**: Primary programming language
- **XAML**: User interface markup
- **LibVLCSharp 3.7.0**: Media playback engine
- **CommunityToolkit.Mvvm**: MVVM framework

### Development Tools
- **Visual Studio 2022**: Primary IDE
- **MSBuild**: Build system
- **XAML Styler**: Code formatting
- **dotnet CLI**: Command-line tools

### Key Dependencies
- **Microsoft.UI.Xaml**: Modern UI controls
- **CommunityToolkit.Uwp**: Additional UWP utilities
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **TagLibSharp**: Audio metadata reading
- **protobuf-net**: Data serialization

### External Services
- **Crowdin**: Translation management
- **AppCenter**: Analytics and crash reporting
- **Sentry**: Error monitoring and performance tracking

## Build Configuration

### Supported Platforms
- **x86**: 32-bit Intel/AMD processors
- **x64**: 64-bit Intel/AMD processors
- **ARM64**: 64-bit ARM processors (e.g., Surface Pro X)

### Build Modes
- **Debug**: Development builds with debugging symbols
- **Release**: Optimized production builds
- **StoreUpload**: Microsoft Store submission packages

### Package Formats
- **MSIX/MSIXBUNDLE**: Modern Windows app packages
- **APPX/APPXUPLOAD**: Legacy UWP packages

## Development Patterns

### Code Organization
- **Separation of Concerns**: Clear boundaries between UI and business logic
- **Dependency Injection**: Loose coupling through IoC container
- **Factory Pattern**: Object creation through dedicated factories
- **Repository Pattern**: Data access abstraction (in services)
- **Command Pattern**: UI actions through command implementations

### Naming Conventions
- **PascalCase**: Classes, methods, properties, enums
- **camelCase**: Local variables, parameters
- **Interfaces**: Prefixed with `I` (e.g., `IMediaPlayer`)
- **Private fields**: Prefixed with `_` (e.g., `_isPlaying`)
- **XAML resources**: Descriptive names (e.g., `PlayerControlStyle`)

### File Organization
- **One class per file**: Clear file-to-class mapping
- **Nested namespaces**: Match folder structure
- **Partial classes**: Used for XAML code-behind files
- **Resource files**: Grouped by language and type

This structure enables maintainable, scalable development while supporting the rich feature set of a modern media player application.