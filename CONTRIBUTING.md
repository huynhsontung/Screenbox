# 🎬 Contributing to Screenbox

Thank you for your interest in contributing to Screenbox! This guide will help you get started with developing and contributing to the project.

## 📋 Table of Contents

- [🚀 Getting Started](#-getting-started)
- [⚙️ Development Environment Setup](#️-development-environment-setup)
- [🏗️ Project Structure](#️-project-structure)
- [🔄 Development Workflow](#-development-workflow)
- [🧹 Code Guidelines](#-code-guidelines)
- [🧪 Testing](#-testing)
- [🌍 Translation](#-translation)
- [📤 Submitting Contributions](#-submitting-contributions)

## 🚀 Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:

- **Visual Studio 2022** (Community, Professional, or Enterprise) or later
  - Required workloads:
    - **WinUI application development**
  - Optional components (recommended):
    - **Universal Windows Platform tools**
    - **Windows 11 SDK (10.0.22621)**
- **Windows 10** version 1903 (build 18362) or later, or **Windows 11**

> **Note**: JetBrains Rider should also work but specific setup instructions aren't covered in this guide.

## ⚙️ Development Environment Setup

### 1. Fork and Clone the Repository

1. **Fork the repository** on GitHub by clicking the "Fork" button
2. **Clone your fork** to your local machine:
   ```bash
   git clone https://github.com/YOUR_USERNAME/Screenbox.git
   cd Screenbox
   ```

### 2. Open and Build the Solution

1. **Open the solution** in Visual Studio: `Screenbox.sln`
2. **Build the solution** to restore NuGet packages: `Ctrl+Shift+B`
3. **Set the platform** to x64 and **start debugging**: `F5`

Visual Studio's built-in Git integration should be sufficient for most development workflows.

## 🏗️ Project Structure

The solution contains two main projects:

```
Screenbox.sln
├── Screenbox/           # Main UWP application (UI layer)
└── Screenbox.Core/      # Core business logic library
```

### Key Architecture Patterns

- **MVVM (Model-View-ViewModel)**: Clean separation of UI and business logic
- **Dependency Injection**: Services are injected for loose coupling
- **Messaging**: Components communicate via the MVVM Toolkit messenger
- **Services Architecture**: Business logic organized into focused services

### Main Directories

- `Screenbox/Views/`: XAML pages and user controls
- `Screenbox/ViewModels/`: Presentation logic and data binding
- `Screenbox.Core/Services/`: Business logic and data access
- `Screenbox.Core/Models/`: Data structures and entities
- `Screenbox/Strings/`: Localization resources

## 🔄 Development Workflow

### Creating a Pull Request

1. **Create a feature branch**: `git checkout -b feature/your-feature-name`
2. **Make your changes** and commit them with descriptive messages
3. **Push to your fork**: `git push origin feature/your-feature-name`
4. **Open a pull request** on GitHub with a clear description

### Pull Request Guidelines

- Use descriptive titles that start with a prefix like `feat:`, `fix:`, or `docs:`
- Include a clear description of what changes were made and why
- Link to relevant issues if applicable
## 🧹 Code Guidelines

### Code Formatting

Before committing changes, please:

1. **Run XAML Styler** on all `.xaml` files you've modified
2. **Use Code Cleanup** (`Ctrl+K, Ctrl+E`) on all `.cs` files you've modified

### EditorConfig

The project includes a comprehensive `.editorconfig` file that defines formatting rules. Key highlights:

- **C# files**: 4-space indentation, UTF-8 with BOM encoding
- **XAML files**: 4-space indentation, UTF-8 with BOM encoding
- **Private fields**: Use underscore prefix (`_fieldName`)
- **Async methods**: End with "Async" suffix
- **Interfaces**: Start with "I" prefix

Your IDE should automatically apply these rules when the `.editorconfig` file is present.
## 🧪 Testing

### Manual Testing Checklist

When testing your changes:

- ✅ **Functionality**: Verify your feature works as expected
- ✅ **Performance**: Check that playback remains smooth
- ✅ **Error handling**: Test edge cases and error scenarios
- ✅ **Accessibility**: Test with different themes (light/dark/high contrast), screen scale, text scale factor, and gamepad navigation if possible
- ✅ **UI**: Verify the interface looks correct and responsive

### Platform Testing

While not required, testing on different architectures (x64, x86, ARM64) is helpful if you have access to those systems.
            if (IsPlaying)
            {
                await _mediaService.PauseAsync();
            }
            else
## 🌍 Translation

### Adding New Strings

When adding features that require new user-facing text:

1. **Only add new strings to the `.en-US.resw` files** in the `Screenbox/Strings/en-US/` directory
2. **Use ReswPlus features** for pluralization and advanced formatting when needed
3. **Follow existing naming conventions** for resource keys

The main resource files are:
- `Resources.resw`: General UI strings
- `KeyboardResources.resw`: Keyboard shortcuts and commands
- `ManifestResources.resw`: App manifest strings

### Contributing Translations

For translating the app to other languages:
- **Crowdin (Recommended)**: [crowdin.com/project/screenbox](https://crowdin.com/project/screenbox)
- **Local translation**: Create copies of the `en-US` resource files in the appropriate language folder

## 📤 Submitting Contributions

1. **Ensure your code builds** without errors or warnings
2. **Test your changes** thoroughly using the testing checklist above
3. **Create a pull request** with a clear title and description
4. **Be responsive** to feedback during the review process

That's it! The maintainers will review your contribution and provide feedback if needed.

## 🎉 Thank You!

Every contribution helps make Screenbox better for everyone. Whether you're fixing bugs, adding features, improving documentation, or translating the app, your efforts are appreciated! 🙏