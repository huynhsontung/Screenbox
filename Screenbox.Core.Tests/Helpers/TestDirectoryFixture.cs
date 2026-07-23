#nullable enable

namespace Screenbox.Core.Tests.Helpers;

/// <summary>
/// Provides isolated temporary directories for unit testing database and filesystem operations.
/// Uses primary constructor and modern C# features.
/// </summary>
public sealed class TestDirectoryFixture : IDisposable
{
    static TestDirectoryFixture()
    {
        // Ensure SQLite native libraries are initialized for Native AOT / unit testing
        SQLitePCL.Batteries.Init();
    }

    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "ScreenboxTests", Guid.NewGuid().ToString("N"));

    public TestDirectoryFixture()
    {
        Directory.CreateDirectory(DirectoryPath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup for temporary test files
        }
    }
}
