# Screenbox.Core.Tests

This test project contains unit tests for `Screenbox.Core` focusing on SQL database services, JSON serialization, legacy JSON-to-SQL data migration, and Native AOT compatibility.

## 🚀 Key Objectives & Architecture

1. **SQL Database Services (`DatabaseService`)**:
   - Verification of SQLite table schema initialization (`library_folders`, `media_records`, `playback_progress`, `playlists`, `playlist_items`).
   - Schema drift detection and automatic table rebuilds.
   - Library cache saving (`SaveMusicCacheAsync`, `SaveVideoCacheAsync`) and loading (`LoadLibraryCacheAsync`).
   - Playlist management (`SavePlaylistAsync`, `LoadPlaylistAsync`, `ListPlaylistsAsync`, `DeletePlaylistAsync`).
   - Playback progress tracking (`SavePlaybackProgressAsync`, `LoadPlaybackProgressAsync`).

2. **Legacy JSON Migration to SQL**:
   - Verification of importing legacy JSON playlist files (e.g. `Playlists/*.json`) containing `PlaylistRecordDto` and `RawMediaRecordDto` into SQLite database tables (`playlists` and `playlist_items`).
   - Automated post-migration cleanup of legacy `.json` files and temporary migration artifacts (`songs.bin`, `videos.bin`, `last_positions.bin`).

3. **JSON Serialization & System.Text.Json Source Generation**:
   - Direct testing of `CoreJsonContext` (`JsonSerializerContext` source generator).
   - Guaranteeing reflection-free serialization/deserialization for DTOs (`PlaylistRecordDto`, `RawMediaRecordDto`).

4. **⚡ Native AOT Compatibility**:
   - The test project is configured with Roslyn AOT & trimming analyzers:
     - `<IsAotCompatible>true</IsAotCompatible>`
     - `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>`
     - `<EnableSingleFileAnalyzer>true</EnableSingleFileAnalyzer>`
   - All test code adheres to Native AOT constraints:
     - Avoid runtime `System.Reflection.Emit` and non-source-generated reflection JSON serialization.
     - Ensure `SQLitePCLRaw` battery initialization (`SQLitePCL.Batteries.Init()`) executes cleanly before database connections open.
     - Validate DTO data structures and parameter binding models (`SqlParameterDto`) under trimmed environments.

5. **✨ Modern C# Features**:
   - Target Framework: `net10.0-windows10.0.26100.0`.
   - C# 12 / 13 / 14 features used throughout test files:
     - File-scoped namespaces (`namespace Screenbox.Core.Tests...`)
     - Primary constructors (e.g., `public class DatabaseServiceTests(TestDirectoryFixture fixture)`)
     - Collection expressions (`[ item1, item2 ]`)
     - Raw string literals (`"""..."""`)
     - Pattern matching (`is`, `switch` expressions)
     - `[]` slice and index operators

---

## 🛠️ How to Build and Run Tests

### Using MSBuild / Visual Studio:
```cmd
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" Screenbox.Core.Tests\Screenbox.Core.Tests.csproj /p:Configuration=Debug /p:Platform=x64 /restore
```

### Running Tests:
```cmd
vstest.console.exe Screenbox.Core.Tests\bin\x64\Debug\net10.0-windows10.0.26100.0\Screenbox.Core.Tests.dll
```
or via `dotnet test Screenbox.Core.Tests\Screenbox.Core.Tests.csproj`.

---

## 📝 Information for Future Developers / AI Agents

- When adding new DTOs or JSON contracts in `Screenbox.Core`, ensure you add `[JsonSerializable(typeof(YourDto))]` to `CoreJsonContext` in `Screenbox.Core/Models/Serialization/CoreJsonContext.cs`.
- Never use reflection-based `JsonSerializer.Deserialize<T>(json)` without passing `JsonSerializerContext` or `JsonTypeInfo<T>` generated metadata, as reflection serialization is incompatible with Native AOT.
- Unit tests instantiate `DatabaseService` using the internal constructor `internal DatabaseService(string? customFolderPath)` to run in an isolated temporary directory, avoiding UWP `ApplicationData.Current` package identity restrictions during test execution.
