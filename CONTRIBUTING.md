# 🎬 Contributing to Screenbox

Thank you for your interest in contributing to Screenbox! This guide will help you get started with developing and contributing to the project.

## 📋 Table of Contents

- [🚀 Getting Started](#-getting-started)
- [⚙️ Development Environment Setup](#️-development-environment-setup)
- [🏗️ Project Structure](#️-project-structure)
- [🔄 Development Workflow](#-development-workflow)
  - [📝 Creating an Issue](#-creating-an-issue)
  - [🔀 Creating a Pull Request](#-creating-a-pull-request)
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
  - Required optional components:
    - **Universal Windows Platform tools**
    - **Windows 11 SDK (10.0.26100.0)**
  - Recommended extensions:
    - **XAML Styler**
- **Windows 10** version 1903 (build 18362) or later, or **Windows 11**
- **Developer Mode** enabled in Windows settings

> [!NOTE]
> JetBrains Rider should also work but specific setup instructions aren't covered in this guide.

## ⚙️ Development Environment Setup

### 1. Fork and Clone the Repository

1. **Fork the repository** On GitHub, click the **Fork** button
2. **Clone your fork**
   - Open Visual Studio
   - Select "Clone Repository..." from the File menu
   - Enter your fork's URL, replace `YOUR-USERNAME` with your GitHub username:
     ```
     https://github.com/YOUR-USERNAME/Screenbox.git
     ```
   
     Or, if you are familiar with Git:
     ```bash
     git clone https://github.com/YOUR-USERNAME/Screenbox.git
     ```

### 2. Open and Build the Solution

1. **Open the solution** in Visual Studio: `Screenbox.sln`
2. **Set the platform** to match your machine's architecture (typically x64)
3. **Build the solution** to restore NuGet packages: `F6`
4. **Start debugging**: `F5`

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

- `Screenbox/Pages/`: XAML pages
- `Screenbox/Controls/`: Custom controls and user interface components
- `Screenbox/Converters/`: Data binding converters
- `Screenbox/Helpers/`: Utility and helper classes
- `Screenbox/Strings/`: Localization resources
- `Screenbox.Core/ViewModels/`: Presentation logic and data binding
- `Screenbox.Core/Services/`: Business logic and data access services
- `Screenbox.Core/Models/`: Data structures and entities
- `Screenbox.Core/Messages/`: MVVM Toolkit messenger message types
- `Screenbox.Core/Playback/`: Media playback logic and components

For a detailed breakdown of the entire codebase architecture, see the [Project Structure documentation](docs/PROJECT_STRUCTURE.md).

## 🔄 Development Workflow

### 📝 Creating an Issue

Creating an issue is a great way to discuss bugs, feature requests, or enhancements with the maintainers and community.

#### When to Create an Issue

- **Bug Reports**: When you encounter unexpected behavior or errors
- **Enhancements**: When you want to improve existing features or add new functionality

#### Issue Guidelines

1. **Search existing issues** first to avoid duplicates
2. **Use the appropriate issue template** (Bug Report or Enhancement)
3. **Use descriptive titles** that clearly summarize the problem or request
4. **Provide detailed information** as requested in the template
5. **Be respectful** and constructive in your communication

### 🔀 Creating a Pull Request

1. **Create a new branch** from the main branch
   - Select "New Branch..." from the Git menu
   - Enter a short, descriptive name for your branch
   
   Or, if you are familiar with Git:
   ```bash
   git branch BRANCH-NAME
   git switch BRANCH-NAME
   ```
2. **Make your changes** and commit them with clear, descriptive messages
3. **Push to your fork** and open a pull request on GitHub

For a full walkthrough of the process, see the [GitHub documentation](https://docs.github.com/en/get-started/exploring-projects-on-github/contributing-to-a-project).

#### Pull Request Guidelines

- **Use descriptive titles** following the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) standard (e.g., `feat:`, `fix:`, `docs:`, `refactor:`, etc.)
- **Include a clear description** of what changes were made and why
- **Link to relevant issues** if applicable
- **Keep pull requests focused** on one feature or fix per PR when possible
- **Update documentation** if your changes affect user-facing functionality

## 🧹 Code Guidelines

### Code Formatting

Before committing changes, please:

1. **Run XAML Styler** (`Ctrl+K, Ctrl+2`) on all `.xaml` files you've modified
2. **Use Code Cleanup** (`Ctrl+K, Ctrl+E`) on all `.cs` files you've modified

Your IDE should automatically use the project's `.editorconfig` rules for formatting.

## 🧪 Testing

### Manual Testing Checklist

When testing your changes:

- ✅ **Functionality**: Verify your feature works as expected
- ✅ **Performance**: Check that playback remains smooth
- ✅ **Error handling**: Test edge cases and error scenarios
- ✅ **Accessibility**: Test with different themes (light/dark/high contrast), screen scale, text scale factor, and gamepad navigation if possible
- ✅ **UI**: Verify the interface looks correct and responsive

### Platform Testing

While not required, testing on different architectures (x64, x86, ARM64) and platforms (such as Xbox One and Xbox Series consoles) is helpful if you have access to these systems.

## 🌍 Translation

### Adding New Strings

When adding features that require new user-facing text:

1. **Only add new strings to the `.resw` files** in the `Screenbox/Strings/en-US/` directory
2. **Use ReswPlus features** for pluralization and advanced formatting when needed
3. **Follow existing naming conventions** for resource keys

The main resource files are:
- `Resources.resw`: General UI strings
- `KeyboardResources.resw`: Keyboard shortcuts and commands
- `ManifestResources.resw`: App manifest strings

### Contributing Translations

For translating the app to other languages:
- **Crowdin (Recommended)**: [crowdin.com/project/screenbox](https://crowdin.com/project/screenbox)
- **Local translation**: Only recommended for languages not available on Crowdin

#### Local Translation Workflow

If your language isn't available on Crowdin, you can either request its addition or proceed with local translation. Just follow these steps: 

- Under `Screenbox/Strings`, create a new sub-folder, for example "fr-FR" for French (France), using the [BCP-47 language tag](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements#supported-languages) for the folder name.
- Copy the contents of the `Screenbox/Strings/en-US/` folder into your language folder and translate them.

For detailed guidance on the translation workflow and language support, see:
- [Translation section in the main README](README.md#translation).
- [Microsoft Language Resources](https://learn.microsoft.com/en-us/globalization/reference/microsoft-language-resources) for comprehensive language and localization reference

## 📤 Submitting Contributions

1. **Ensure your code builds** without errors or warnings
2. **Test your changes** thoroughly using the testing checklist above
3. **Create a pull request** with a clear title and description
4. **Be responsive** to feedback during the review process

That's it! The maintainers will review your contribution and provide feedback if needed.

## 🎉 Thank You!

Every contribution helps make Screenbox better for everyone. Whether you're fixing bugs, adding features, improving documentation, or translating the app, your efforts are appreciated! 🙏