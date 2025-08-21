# Contributing to Screenbox

Thank you for your interest in contributing to Screenbox! This guide will help you get started with developing and contributing to the project.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Environment Setup](#development-environment-setup)
- [Project Structure](#project-structure)
- [Development Workflow](#development-workflow)
- [Code Guidelines](#code-guidelines)
- [Testing](#testing)
- [Translation](#translation)
- [Submitting Contributions](#submitting-contributions)
- [Resources](#resources)

## Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:

- **Visual Studio 2022** (Community, Professional, or Enterprise)
  - Required workloads:
    - .NET desktop development
    - Universal Windows Platform development
  - Required individual components:
    - Windows 10/11 SDK (version 10.0.22621.0 or later)
    - .NET 6.0 Runtime
- **Git** for version control
- **Windows 10** version 1903 (build 18362) or later, or **Windows 11**

### Optional Tools

- **ResX Resource Manager**: For easier `.resw` file translation
- **GitHub Desktop**: If you prefer a GUI for Git operations

## Development Environment Setup

### 1. Fork and Clone the Repository

1. **Fork the repository** on GitHub by clicking the "Fork" button
2. **Clone your fork** to your local machine:
   ```bash
   git clone https://github.com/YOUR_USERNAME/Screenbox.git
   cd Screenbox
   ```

### 2. Install Development Tools

The project uses .NET CLI tools for development. Install them by running:

```bash
dotnet tool restore
```

This will install:
- **XAML Styler**: For consistent XAML formatting

### 3. Open the Solution

1. Open **Visual Studio 2022**
2. Open the solution file: `Screenbox.sln`
3. Allow Visual Studio to restore NuGet packages automatically

### 4. Build the Solution

1. Set the build configuration to **Debug**
2. Set the platform to **x64** (recommended for development)
3. Build the solution: **Build > Build Solution** (Ctrl+Shift+B)

### 5. Run the Application

1. Set **Screenbox** as the startup project (right-click → Set as Startup Project)
2. Run the application: **Debug > Start Debugging** (F5)

## Project Structure

Please refer to the [Project Structure Guide](PROJECT_STRUCTURE.md) for a detailed overview of the codebase organization.

### Key Directories for Contributors

- **`Screenbox/Pages/`**: UI pages and screens
- **`Screenbox/Controls/`**: Custom user controls
- **`Screenbox.Core/ViewModels/`**: MVVM view models
- **`Screenbox.Core/Services/`**: Business logic services
- **`Screenbox.Core/Models/`**: Data models
- **`Screenbox/Strings/`**: Localization resources

## Development Workflow

### 1. Creating a New Feature

1. **Create a new branch** for your feature:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Follow the MVVM pattern**:
   - Create models in `Screenbox.Core/Models/`
   - Create view models in `Screenbox.Core/ViewModels/`
   - Create views (pages/controls) in `Screenbox/Pages/` or `Screenbox/Controls/`

3. **Register services** in `Screenbox.Core/Common/ServiceHelpers.cs` if needed

4. **Add localization strings** in `Screenbox/Strings/en-US/Resources.resw`

### 2. Making Changes

1. **Write clean, documented code** following the project's coding standards
2. **Test your changes** thoroughly on different scenarios
3. **Run XAML linting** to ensure consistent formatting:
   ```bash
   .\scripts\lint-xaml.ps1
   ```

### 3. Committing Changes

1. **Stage your changes**:
   ```bash
   git add .
   ```

2. **Commit with a descriptive message**:
   ```bash
   git commit -m "feat: add support for custom subtitle fonts"
   ```

   Use conventional commit prefixes:
   - `feat:` for new features
   - `fix:` for bug fixes
   - `docs:` for documentation changes
   - `style:` for formatting changes
   - `refactor:` for code refactoring
   - `test:` for adding tests
   - `chore:` for maintenance tasks

## Code Guidelines

### General Principles

1. **Follow SOLID principles**
2. **Use dependency injection** for service dependencies
3. **Separate concerns** between UI and business logic
4. **Write self-documenting code** with clear naming
5. **Handle errors gracefully** with proper exception handling

### C# Coding Standards

#### Naming Conventions

```csharp
// Classes and methods: PascalCase
public class MediaPlayer
{
    public void PlayMedia() { }
}

// Private fields: _camelCase
private readonly IMediaService _mediaService;

// Parameters and local variables: camelCase
public void ProcessFile(string fileName)
{
    var processedName = fileName.ToLower();
}

// Constants: PascalCase
private const int DefaultVolume = 50;

// Interfaces: I + PascalCase
public interface IMediaService { }
```

#### Code Organization

```csharp
// Using statements
using System;
using System.Threading.Tasks;

// Namespace matching folder structure
namespace Screenbox.Core.Services
{
    // Class with XML documentation for public APIs
    /// <summary>
    /// Manages media playback operations.
    /// </summary>
    public class MediaService : IMediaService
    {
        // Private fields first
        private readonly ILogger _logger;
        
        // Constructor
        public MediaService(ILogger logger)
        {
            _logger = logger;
        }
        
        // Public properties
        public bool IsPlaying { get; private set; }
        
        // Public methods
        public async Task PlayAsync(string filePath)
        {
            // Method implementation
        }
        
        // Private methods
        private void LogError(string message)
        {
            _logger.LogError(message);
        }
    }
}
```

### XAML Guidelines

#### Formatting and Structure

```xml
<!-- Use XAML Styler formatting -->
<Page x:Class="Screenbox.Pages.HomePage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:controls="using:Screenbox.Controls">

    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Content here -->
        <controls:MediaListView Grid.Row="1"
                               ItemsSource="{x:Bind ViewModel.MediaItems}"
                               SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}" />

    </Grid>
</Page>
```

#### Data Binding

```xml
<!-- Use x:Bind for better performance -->
<TextBlock Text="{x:Bind ViewModel.Title}" />

<!-- Use Binding for dynamic scenarios -->
<TextBlock Text="{Binding CurrentTime, Converter={StaticResource TimeConverter}}" />
```

### MVVM Implementation

#### ViewModel Example

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Screenbox.Core.ViewModels
{
    public partial class MediaPlayerViewModel : ObservableRecipient
    {
        private readonly IMediaService _mediaService;
        
        [ObservableProperty]
        private bool _isPlaying;
        
        [ObservableProperty]
        private string _currentTitle = string.Empty;
        
        public MediaPlayerViewModel(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }
        
        [RelayCommand]
        private async Task PlayPauseAsync()
        {
            if (IsPlaying)
            {
                await _mediaService.PauseAsync();
            }
            else
            {
                await _mediaService.PlayAsync();
            }
            
            IsPlaying = !IsPlaying;
        }
    }
}
```

## Testing

### Manual Testing

1. **Test on different platforms**: x64, x86, ARM64 if possible
2. **Test with various media formats**: Audio, video, playlists
3. **Test UI responsiveness**: Different window sizes and orientations
4. **Test accessibility**: Keyboard navigation, screen readers
5. **Test with different languages**: Switch UI language if possible

### Automated Testing

Currently, the project relies on manual testing. Consider adding unit tests for:
- Service layer functionality
- ViewModel logic
- Data transformation methods

## Translation

Screenbox supports multiple languages through localization. Here are two ways to contribute translations:

### Option 1: Crowdin (Recommended)

1. Visit the [Screenbox Crowdin project](https://crowdin.com/project/screenbox)
2. Request to join the translation team for your language
3. Use the intuitive Crowdin interface to translate strings
4. Translations are automatically synced to the GitHub repository

### Option 2: Local Translation

1. **Create a language folder**:
   ```
   Screenbox/Strings/[language-code]/
   ```
   Use IETF language tags (e.g., `fr-FR`, `es-ES`, `zh-Hans`)

2. **Copy base files** from `en-US` folder:
   - `Resources.resw` - Main application strings
   - `KeyboardResources.resw` - Keyboard shortcuts
   - `ManifestResources.resw` - App manifest strings
   - `STORE.md` - Store description

3. **Translate the content** in each file:
   - For `.resw` files: Translate the `<value>` content, keep `<name>` unchanged
   - For `.md` files: Translate the text content

4. **Test your translations** by switching the system language

### Translation Guidelines

- **Keep placeholders**: Maintain `{0}`, `{1}` format strings
- **Preserve line breaks**: Keep `\n` and `\r\n` sequences
- **Maintain context**: Consider UI space limitations
- **Use consistent terminology**: Maintain consistency across the app
- **Test in context**: Verify translations work in the actual UI

## Submitting Contributions

### Pull Request Process

1. **Push your branch** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request** on GitHub:
   - Navigate to the original repository
   - Click "New Pull Request"
   - Select your branch from your fork
   - Fill out the PR template with:
     - Clear description of changes
     - Screenshots for UI changes
     - Testing steps performed

3. **Address feedback** from reviewers:
   - Make requested changes
   - Push additional commits to your branch
   - Respond to comments professionally

### PR Requirements

- **Code compiles** without errors or warnings
- **XAML is properly formatted** (run lint script)
- **No breaking changes** unless discussed
- **Include screenshots** for UI changes
- **Update documentation** if needed
- **Test thoroughly** on multiple scenarios

### Review Process

1. **Automated checks** will run (build, XAML lint)
2. **Maintainers will review** your code and design
3. **Feedback may be provided** for improvements
4. **Once approved**, your PR will be merged

## Resources

### Documentation
- [Project Structure](PROJECT_STRUCTURE.md)
- [UWP Documentation](https://docs.microsoft.com/en-us/windows/uwp/)
- [LibVLCSharp Documentation](https://code.videolan.org/videolan/LibVLCSharp)
- [MVVM Toolkit Documentation](https://docs.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)

### Community
- [GitHub Issues](https://github.com/huynhsontung/Screenbox/issues) - Bug reports and feature requests
- [GitHub Discussions](https://github.com/huynhsontung/Screenbox/discussions) - Questions and general discussion

### Tools and References
- [ResX Resource Manager](https://github.com/dotnet/ResXResourceManager) - Translation tool
- [Crowdin](https://crowdin.com/project/screenbox) - Online translation platform
- [XAML Styler](https://github.com/Xavalon/XamlStyler) - XAML formatting tool
- [Visual Studio](https://visualstudio.microsoft.com/) - Development environment

## Questions?

If you have questions about contributing, feel free to:
1. Check existing [GitHub Issues](https://github.com/huynhsontung/Screenbox/issues)
2. Start a [GitHub Discussion](https://github.com/huynhsontung/Screenbox/discussions)
3. Open a new issue with the `question` label

Thank you for contributing to Screenbox! Your efforts help make this media player better for everyone.