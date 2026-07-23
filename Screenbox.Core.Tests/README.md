# Screenbox.Core.Tests

This test project contains unit tests for `Screenbox.Core` focusing on SQL database services, JSON serialization, legacy JSON-to-SQL data migration, and Native AOT compatibility.

## 🚀 Key Objectives & Architecture

1. **SQL Database Services (`DatabaseService`)**:
   - Verification of SQLite table schema initialization (`library_folders`, `media_records`, `playback_progress`, `playlists`, `playlist_items`).
   - Schema drift detection and automatic table rebuilds.
   - Library cache saving and loading operations.
   - Playlist management (`SavePlaylistAsync`, `LoadPlaylistAsync`, `ListPlaylistsAsync`, `DeletePlaylistAsync`).
   - Playback progress tracking state management.

2. **Legacy JSON Migration to SQL**:
   - Verification of importing legacy JSON playlist files (e.g. `Playlists/*.json`) containing `PlaylistRecordDto` and `RawMediaRecordDto` into SQLite database tables (`playlists` and `playlist_items`).
   - Automated post-migration cleanup of legacy `.json` files and temporary migration artifacts.

3. **JSON Serialization & System.Text.Json Source Generation**:
   - Direct testing of `CoreJsonContext` (`JsonSerializerContext` source generator).
   - Guaranteeing reflection-free serialization/deserialization for DTOs (`PlaylistRecordDto`, `RawMediaRecordDto`).

4. **⚡ Native AOT Compatibility**:
   - The test project is configured with Roslyn AOT & trimming analyzers:
     - `<IsAotCompatible>true</IsAotCompatible>`
     - `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>`
     - `<EnableSingleFileAnalyzer>true</EnableSingleFileAnalyzer>`

5. **✨ Modern C# Features**:
   - Target Framework: `net10.0-windows10.0.26100.0`.
   - C# 12+ features used throughout test files:
     - File-scoped namespaces
     - Primary constructors
     - Collection expressions (`[ item1, item2 ]`)
     - Raw string literals (`"""..."""`)
     - Pattern matching and slice operators

---

## 🛠️ How to Build and Run Tests

The test project relies strictly on the `x64`, `ARM64`, or `x86` build architectures to align with the Screenbox UWP environments. Standard `AnyCPU` configuration is intentionally overridden.

### Using MSBuild / Visual Studio:
```cmd
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" Screenbox.Core.Tests\Screenbox.Core.Tests.csproj /p:Configuration=Release /p:Platform=x64 /restore
```

### Running Tests:
```cmd
vstest.console.exe Screenbox.Core.Tests\bin\x64\Release\*\Screenbox.Core.Tests.dll
```
Or via `.NET CLI` targeting the pre-built DLL (which is how GitHub Actions CI executes it):
```cmd
dotnet test Screenbox.Core.Tests\bin\x64\Release\*\Screenbox.Core.Tests.dll
```

---

## 📝 Information for Future Developers / AI Agents

- **Source Generated JSON**: When adding new DTOs or JSON contracts in `Screenbox.Core`, ensure you add `[JsonSerializable(typeof(YourDto))]` to `CoreJsonContext` in `Screenbox.Core/Models/Serialization/CoreJsonContext.cs`. Never use reflection-based `JsonSerializer.Deserialize<T>(json)` without passing `JsonSerializerContext` or `JsonTypeInfo<T>` generated metadata.
- **Isolating WinRT for JIT**: Unit tests execute on generic `.NET`, lacking UWP runtime elements like `ApplicationData.Current`. The core library explicitly isolates WinRT calls into unique methods using `[MethodImpl(MethodImplOptions.NoInlining)]`. This securely bypasses JIT `PlatformNotSupportedException` crashes since test execution won't trigger the compiler to load UWP references.
- **Database Initialization for Testing**: Instead of relying on WinRT to resolve database folders, unit tests instantiate `DatabaseService` and set the public property `DbFolderPath = TestDirectoryPath` to force SQLite interactions into isolated, volatile temporary directories.
