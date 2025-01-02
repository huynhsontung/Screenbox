# Screenbox Symbol Font

This folder holds the custom icons, in the [Unified Font Object](https://github.com/unified-font-object/ufo-spec) format, that are utilized across the application.

## Building the fonts

This guide provides steps to set up your development environment and build the icon font.

### Building in FontForge GUI

1. Install FontForge
   
   ```powershell
   winget install --id FontForge.FontForge --exact --source winget
   ```

2. Start FontForge and, from the **Open** Dialog, select the font folder (.ufo)

3. Select `File` > `Generate Fonts... (Ctrl+Shift+G)`

4. Pick **TrueType** from the dropdown list

5. Uncheck all options in the `Options` dialog, except for **TrueType Hints**

6. Uncheck the **Validate Before Saving** option

7. Press `Generate`

