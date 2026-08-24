---
name: generate-assets
description: >-
  Procedures for generating vector fonts and Lottie animation visual sources from assets.
  Use when vector font definitions, font glyphs, or animation JSON files are modified.
---

# Asset Generation Runbook

Use this runbook when updating icon glyphs, font files, or Lottie animation assets for Screenbox.

---

## 1. Prerequisites

Ensure local dotnet tools are restored:
```pwsh
dotnet tool restore
```

---

## 2. Generate Font Assets

When font definitions in `assets/fonts/` are modified or new glyph icons are added:

1. **Run the Font Generation Script**:
   ```pwsh
   pwsh ./scripts/Generate-Fonts.ps1
   ```
2. **Generated Output Location**:
   - `Screenbox/Assets/Fonts/ScreenboxFluentIcons.ttf`
   - `Screenbox/Assets/Fonts/ScreenboxMDL2Assets.ttf`
3. **Verification**:
   - Verify generated `.ttf` files under `Screenbox/Assets/Fonts/`.
   - Ensure the new glyphs match the character codes referenced in XAML or code (e.g., `GlyphConverter.cs`).

---

## 3. Generate Lottie Animation Classes

When animation JSON files in `assets/animations/` are added or modified:

1. **Run the Lottie Generation Script**:
   ```pwsh
   pwsh ./scripts/generate-lottie.ps1
   ```
   This script runs `LottieGen` with parameters:
   - `-GenerateColorBindings`
   - `-GenerateDependencyObject`
   - `-Language CSharp`
   - `-MinimumUapVersion 8`
   - `-Namespace Screenbox.Animations`
   - `-Public`
   - `-WinUIVersion 2.8`
   - Target output: `Screenbox/Assets/Animations/`
2. **Generated Output Location**:
   - `Screenbox/Assets/Animations/*.cs` (e.g., `AnimatedPlayingVisualSource.cs`)
3. **Verification**:
   - Build `Screenbox` using MSBuild from Visual Studio 2026 to ensure generated C# classes compile cleanly:
     ```pwsh
     msbuild Screenbox/Screenbox.csproj /p:Configuration=Debug /p:Platform=x64 /t:Build /m
     ```
