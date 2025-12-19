# 📱 Screenbox Project Structure

This document provides a comprehensive overview of the Screenbox project's architecture, organization, and practical workflows for development.

## 📋 Table of Contents

- [📖 Overview](#-overview)
- [🏗️ Solution Structure](#️-solution-structure)
- [🎨 View Layer](#-view-layer)
  - [Visual States](#visual-states)
  - [Data Binding](#data-binding)
  - [Navigation System](#navigation-system)
  - [Localization System](#localization-system)
- [🔄 ViewModel Layer](#-viewmodel-layer)
  - [PropertyChanged Events](#propertychanged-events)
  - [Messaging System](#messaging-system)
- [🔧 Model Layer](#-model-layer)
  - [Services Architecture](#services-architecture)
  - [Media Playback Engine](#media-playback-engine)
  - [Data Models and Persistence](#data-models-and-persistence)
- [🛠️ Technology Stack](#️-technology-stack)
- [📋 Development Guidelines](#-development-guidelines)

## 📖 Overview

Screenbox is a modern media player for Windows built using the Universal Windows Platform ([UWP](https://learn.microsoft.com/en-us/windows/uwp/get-started/universal-application-platform-guide)) and LibVLCSharp. The application follows the Model-View-ViewModel ([MVVM](https://learn.microsoft.com/en-us/windows/uwp/data-binding/data-binding-and-mvvm)) design pattern with dependency injection for maintainable and testable code.

The architecture is built around clean separation of concerns:
- **View Layer**: XAML-based UI components and user controls
- **ViewModel Layer**: Presentation logic and data binding with MVVM
- **Model Layer**: Business logic, services, and media playback engine

## 🏗️ Solution Structure

The solution contains two main projects organized for clear separation of concerns:

```
Screenbox.sln
├── Screenbox/           # Main UWP application (UI layer)
└── Screenbox.Core/      # Core business logic library
```

### Main Project: Screenbox
The primary UWP application containing all user interface components, platform-specific code, and XAML resources.

### Core Library: Screenbox.Core
Contains the business logic, services, view models, and media playback components that can be shared across different platforms.

## 🎨 View Layer

The View layer is contained in the Screenbox project. This project consists primarily of XAML files and custom controls that define the user interface. `App.xaml` and the `Styles` folder contain resources referenced throughout the application, while the `App.xaml.cs` file serves as the main entry point and handles dependency injection configuration.

### Core UI Structure

Screenbox uses a single-page application model with `MainPage.xaml` as the root container. The main page utilizes a `NavigationView` control for the navigation menu and houses content frames for different application modes:

```
MainPage.xaml (Root Container)
├── NavigationView (Menu System)
├── ContentFrame (Dynamic Content)
└── PlayerPage.xaml (Media Player Overlay)
```

### Key Page Components

The application organizes its content into several main page categories:

- **`HomePage.xaml`**: Recently accessed media
- **`VideosPage.xaml`**: Video library browsing with nested pages for categories
- **`MusicPage.xaml`**: Music library with artist, album, and song views
- **`NetworkPage.xaml`**: Network streaming and casting features  
- **`PlayQueuePage.xaml`**: Current playlist and queue management
- **`SettingsPage.xaml`**: Application configuration and preferences

### Custom Controls Architecture

Screenbox implements numerous custom controls for specialized functionality:

#### Core Playback Controls
- **`PlayerControls.xaml`**: Primary media control interface
- **`SeekBar.xaml`**: Timeline scrubbing and progress indication
- **`VolumeControl.xaml`**: Audio level management
- **`PlayerElement.xaml`**: Main video rendering surface

#### Media Display Controls  
- **`MediaListViewItem.xaml`**: Templated list items for media content
- **`CommonGridViewItem.xaml`**: Grid layout for media thumbnails
- **`PlaylistView.xaml`**: Specialized playlist display component

#### Dialogs and Overlay Controls
- **`PropertiesDialog.xaml`**: Media file information display
- **`OpenUrlDialog.xaml`**: Network URL input interface

### Visual States

[Visual States](https://learn.microsoft.com/en-us/windows/uwp/design/layout/layouts-with-xaml#adaptive-layouts-with-visual-states-and-state-triggers) enable adaptive layouts that respond to window size changes, device orientation, and user interaction modes. Screenbox uses visual states extensively to create responsive experiences across different form factors.

### Data Binding

Screenbox uses [data binding](https://learn.microsoft.com/en-us/windows/uwp/data-binding/data-binding-quickstart) extensively to create dynamic, responsive UI components. The application primarily uses the [x:Bind](https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/x-bind-markup-extension) markup extension for performance benefits over the legacy [Binding](https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/binding-markup-extension) syntax.

Example of x:Bind usage in media display:
```xml
<TextBlock Text="{x:Bind ViewModel.CurrentMedia.Title, Mode=OneWay}" />
<ProgressBar Value="{x:Bind ViewModel.PlaybackPosition, Mode=OneWay}" />
```

The binding system enables automatic UI updates when media state changes, providing smooth user experiences without manual UI manipulation code.

### Navigation System

Navigation in Screenbox is handled through a custom `NavigationService` that maps ViewModels to their corresponding Pages. This system is configured in `App.xaml.cs` and enables loose coupling between the ViewModel and View layers:

```csharp
new KeyValuePair<Type, Type>(typeof(HomePageViewModel), typeof(HomePage)),
new KeyValuePair<Type, Type>(typeof(VideosPageViewModel), typeof(VideosPage)),
new KeyValuePair<Type, Type>(typeof(MusicPageViewModel), typeof(MusicPage))
```

### Localization System

Screenbox implements comprehensive multi-language support through the Windows and ReswPlus resource system, following [UWP localization guidelines](https://learn.microsoft.com/en-us/windows/uwp/design/globalizing/globalizing-portal) and [ReswPlus guides](https://github.com/DotNetPlus/ReswPlus/wiki) for efficient localized resource management.

## 🔄 ViewModel Layer

The ViewModel layer is contained in the Screenbox.Core project and serves as the intermediary between the UI components and business logic. ViewModels provide data sources for UI binding and encapsulate presentation logic while remaining independent of specific UI implementations.

### Key ViewModel Architecture

#### Primary Application ViewModels
- **`MainPageViewModel.cs`**: Root application state and navigation coordination
- **`PlayerPageViewModel.cs`**: Media playback state and control logic
- **`PlayerControlsViewModel.cs`**: Playback control interactions and state
- **`SettingsPageViewModel.cs`**: Application configuration management

#### Content Management ViewModels
- **`MediaListViewModel.cs`**: Global playlist and media queue management
- **`CommonViewModel.cs`**: Shared state across multiple pages
- **`MediaViewModel.cs`**: Individual media item representation
- **`PlaylistViewModel.cs`**: Playlist-specific functionality

#### Library and Search ViewModels
- **`HomePageViewModel.cs`**: Recent media
- **`VideosPageViewModel.cs`**: Video library browsing
- **`MusicPageViewModel.cs`**: Music library organization
- **`SearchResultPageViewModel.cs`**: Search functionality and results

### PropertyChanged Events

ViewModels implement property change notification through the [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) framework. Most ViewModels inherit from `ObservableObject` or use source generators to implement [INotifyPropertyChanged](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.data.inotifypropertychanged) automatically:

```csharp
[ObservableProperty]
private string _currentMediaTitle;

[ObservableProperty]  
private TimeSpan _playbackPosition;
```

This enables automatic UI updates when ViewModel properties change, maintaining synchronization between the data layer and user interface.

### Messaging System

Screenbox uses the [CommunityToolkit.Mvvm messaging system](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger) for decoupled communication between components. This allows ViewModels, Services, and other components to communicate without direct references.

#### Core Message Types

**Playback Control Messages**
- **`PlayMediaMessage.cs`**: Initiate media playback
- **`TogglePlayPauseMessage.cs`**: Toggle playback state
- **`ChangeTimeRequestMessage.cs`**: Seek to specific time position

**UI State Messages**  
- **`PlayerControlsVisibilityChangedMessage.cs`**: Show/hide player controls
- **`SettingsChangedMessage.cs`**: Application setting modifications
- **`NavigationViewDisplayModeRequestMessage.cs`**: Navigation menu state changes

**System Integration Messages**
- **`SuspendingMessage.cs`**: Application lifecycle events
- **`ErrorMessage.cs`**: Error reporting and handling
- **`NotificationRaisedEventArgs.cs`**: User notification display

## 🔧 Model Layer

The Model layer contains the core business logic and is primarily located in the Screenbox.Core project. This layer consists of services, media playback components, and data models that provide the foundation for the application functionality.

### Services Architecture

Screenbox implements a comprehensive service-oriented architecture using [Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) for service registration and resolution. Services are registered in `ServiceHelpers.cs` and injected throughout the application.

#### Core Business Services

**Media Management Services**
- **`IFilesService`**: File system operations and media discovery
- **`ILibraryService`**: Media library indexing and organization  
- **`ISearchService`**: Content search and filtering functionality

**Playback and Streaming Services**
- **`IPlayerService`**: Media player initialization and playback item management
- **`ICastService`**: Stateless casting operations for renderer creation and configuration
- **`ISystemMediaTransportControlsService`**: Windows media key integration

**System Integration Services**
- **`ISettingsService`**: Application configuration persistence
- **`INotificationService`**: User notification management
- **`IWindowService`**: Window management and display operations

#### Application Contexts

Contexts provide observable state management that can be shared across services and components:

- **`PlayerContext`**: Holds the current media player instance, allowing components to observe player state changes
- **`CastContext`**: Holds casting state including the active renderer watcher and selected renderer

#### Helper Classes

Helper classes provide focused utilities and lightweight wrappers for specific functionality:

- **`RendererWatcher`**: Lightweight wrapper around VLC's RendererDiscoverer for network renderer discovery, exposing events for renderer found/lost notifications and maintaining a list of available renderers
- **`DisplayRequestTracker`**: Manages display sleep prevention during media playback
- **`LastPositionTracker`**: Tracks and persists media playback positions for resume functionality

#### UI-Specific Services (Screenbox Project)
- **`ResourceService.cs`**: Localization and resource string management
- **`NavigationService.cs`**: Page navigation and routing

### Media Playback Engine

The media playback system is built on [LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp) with custom abstractions for integration with the MVVM architecture:

#### Core Playback Components
- **`IMediaPlayer`**: Media player abstraction interface
- **`VlcMediaPlayer.cs`**: VLC-based implementation of media player
- **`PlaybackItem.cs`**: Media item wrapper with metadata and state

#### Track Management System
- **`PlaybackAudioTrackList.cs`**: Audio track selection and switching
- **`PlaybackVideoTrackList.cs`**: Video track and subtitle management  
- **`PlaybackSubtitleTrackList.cs`**: Subtitle track handling
- **`PlaybackChapterList.cs`**: Chapter navigation support

The playback engine provides a clean interface for the ViewModel layer while abstracting the complexities of the underlying VLC media framework.

### Data Models and Persistence

#### Media Information Models
- **`MediaInfo.cs`**: Base media file information
- **`VideoInfo.cs`**: Video-specific metadata and properties
- **`MusicInfo.cs`**: Audio metadata and music library information

#### Application State Models  
- **`PersistentMediaRecord.cs`**: Saved playback state and resume positions
- **`PersistentStorageLibrary.cs`**: Library folder persistence

## 🛠️ Technology Stack

- **UWP (Universal Windows Platform)**: Windows application framework providing native performance and system integration
- **C# 10.0**: Primary programming language
- **XAML**: Declarative markup for user interface definition
- **LibVLCSharp 3.7.0**: Cross-platform media playback engine with extensive codec support
- **CommunityToolkit.Mvvm**: Modern MVVM framework with source generators and messaging

### Development and Build Tools  
- **Visual Studio 2022**: Primary integrated development environment
- **MSBuild**: Build system and project management
- **XAML Styler**: XAML formatting and style enforcement

### Key Dependencies

#### MVVM and UI Framework
- [Microsoft.UI.Xaml](https://github.com/microsoft/microsoft-ui-xaml/) - WinUI controls and modern UI components
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet/) - MVVM helpers, messaging, and source generators
- [CommunityToolkit.Uwp](https://www.nuget.org/packages/CommunityToolkit.Uwp/) - UWP community extensions

#### Dependency Injection
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/) - Service container and dependency injection

#### Media and Data Processing
- [LibVLCSharp](https://github.com/videolan/libvlcsharp) - VLC media player integration
- [TagLibSharp](https://github.com/mono/taglib-sharp) - Audio metadata reading
- [protobuf-net](https://github.com/protobuf-net/protobuf-net) - Protocol Buffers serialization

#### Monitoring and Analytics
- [Microsoft.AppCenter](https://www.nuget.org/packages/Microsoft.AppCenter/) - Crash reporting and usage analytics
- [Sentry](https://www.nuget.org/packages/Sentry/) - Error tracking and performance monitoring

## 📋 Development Guidelines

### Code Organization
Follow [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) and [UWP](https://learn.microsoft.com/en-us/windows/uwp/get-started/) best practices throughout the codebase.

### Naming Conventions
Adhere to [.NET naming guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines):

- **PascalCase**: Classes, methods, properties, public fields, enums
- **camelCase**: Local variables, parameters, private fields
- **Interfaces**: Prefixed with `I` (e.g., `IMediaPlayer`)
- **Private fields**: Prefixed with `_` (e.g., `_settingsService`)
- **XAML resources**: Descriptive, consistent naming patterns

### File Organization
- One class per file
- Nested namespaces match folder structure
- Partial classes for XAML code-behind

### Performance Considerations

#### Asynchronous Programming
- Use `async/await` for I/O operations following [async best practices](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)
- Proper `ConfigureAwait(false)` usage in library code
- Task-based operations for file system access

#### Memory Management
- Follow [.NET memory management guidelines](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/)
- Use weak references for event handlers where appropriate

#### UI Performance
- Follow [UWP performance best practices](https://learn.microsoft.com/en-us/windows/uwp/debug-test-perf/performance-and-xaml-ui)
- Enable virtualization for large collections
- Implement proper image caching
- Process heavy operations on background threads

### Build Configuration and Deployment

#### Supported Platforms
- **x86**: 32-bit Intel/AMD processors
- **x64**: 64-bit Intel/AMD processors
- **ARM64**: 64-bit ARM processors

#### Build Modes
- **Debug**: Development builds with debugging symbols
- **Release**: Optimized production builds
- **StoreUpload**: Microsoft Store submission packages

For detailed build configuration, see [UWP packaging documentation](https://learn.microsoft.com/en-us/windows/uwp/packaging/).

### External Resources and Documentation

#### Documentation
- [UWP Developer Guide](https://learn.microsoft.com/en-us/windows/uwp/)
- [C# Programming Reference](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [MVVM Toolkit Documentation](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

#### Best Practices and Guidelines
- [XAML Performance Guidelines](https://learn.microsoft.com/en-us/windows/uwp/debug-test-perf/performance-and-xaml-ui)
- [Async Programming Patterns](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)
- [Accessibility Guidelines for UWP Apps](https://learn.microsoft.com/en-us/windows/uwp/accessibility/accessibility)

This structure enables maintainable, scalable development while supporting the rich feature set of a modern media player application.
