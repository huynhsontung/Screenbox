# Agent Instructions & Guidelines

This repository maintains its canonical AI agent instructions and coding guidelines under `.github/`. All AI assistants (Antigravity, GitHub Copilot, and other agentic tools) must adhere to these instructions.

## 📋 General Guidelines & Workflows

Refer to [`.github/copilot-instructions.md`](.github/copilot-instructions.md) for:
- **Git & PR Workflows**: Conventional Commits standard, PR title conventions, issue linking, descriptive change summaries (context, motivation, impact), and mandatory `--body-file` usage for GitHub CLI (`gh`) commands on Windows.
- **Build & Test Workflows**: MSBuild commands (Visual Studio 2026 / version 18) for `Screenbox` and `Screenbox.Core` (never `dotnet build`), and `dotnet run` for running `Screenbox.Core.Tests`.
- **Persistence Boundaries**: Ephemeral SQLite cache data (`library_folders`, `media_records`, `playback_progress`) vs. durable user data (`playlists`, `playlist_items`), and per-table database recovery rules.

## 💻 C# and .NET 9+ Development

When viewing, writing, or modifying C# code (`**/*.cs`), strictly follow [`.github/instructions/dotnet-uwp.instructions.md`](.github/instructions/dotnet-uwp.instructions.md) for:
- **Language & Formatting**: C# 14 features, nullable reference types (`is null` / `is not null`), `.editorconfig` formatting, and XML doc comments.
- **Architecture Layers**: Separation of Views, ViewModels, Contexts (observable state holders), Coordinators (stateful resource managers), and Services (stateless business logic).
- **MVVM Patterns**: CommunityToolkit.Mvvm `ObservableObject`, `ObservableRecipient`, `[ObservableProperty]`, `[RelayCommand]`, and `Messenger`.
- **Testing Constraints**: Mandatory `UWP Unit Test App` runner for XAML-dependent tests with `[UITestMethod]` vs `[TestMethod]`.

## 🎨 XAML & UI Development

When viewing, writing, or modifying XAML markup (`**/*.xaml`), strictly follow [`.github/instructions/uwp-xaml.instructions.md`](.github/instructions/uwp-xaml.instructions.md) for:
- **Data Binding**: Compiled bindings (`{x:Bind}`), explicit modes (`OneWay`, `TwoWay`, `OneTime`), `x:DataType` on templates, and fallback handling.
- **Localization**: ReswPlus strongly-typed strings in `Screenbox/Strings/en-US/`, with strict View-layer-only string access (never from ViewModels).
- **Accessibility & Theming**: WCAG contrast, `AutomationProperties`, `{ThemeResource}` vs `{StaticResource}`, and UI collection virtualization (`ListView`/`GridView`, `ISupportIncrementalLoading`).
