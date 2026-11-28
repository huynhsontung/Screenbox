using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.Tests.Services;

public class FilesServiceTests
{
    [Test]
    public async Task ResolveFileAsync_NonExistentFileSource_ReturnsNull()
    {
        var service = new FilesService(NullLogger<FilesService>.Instance);
        var source = new FileMediaSource(@"C:\nonexistent_folder_xyz\missing.mp4");

        StorageFile? file = await service.ResolveFileAsync(source);

        await Assert.That(file).IsNull();
        await Assert.That(source.StorageFile).IsNull();
    }

    [Test]
    public async Task ResolveFileAsync_ExistingPath_ResolvesAndCachesStorageFile()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"screenbox_files_test_{Guid.NewGuid():N}.mp4");
        await File.WriteAllBytesAsync(tempPath, [0x01, 0x02]);

        try
        {
            var service = new FilesService(NullLogger<FilesService>.Instance);
            var source = new FileMediaSource(tempPath);

            StorageFile? resolved = await service.ResolveFileAsync(source);

            await Assert.That(resolved).IsNotNull();
            await Assert.That(source.StorageFile).IsSameReferenceAs(resolved);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
