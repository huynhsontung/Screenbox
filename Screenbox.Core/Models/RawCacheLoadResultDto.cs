#nullable enable

using System.Collections.Generic;

namespace Screenbox.Core.Models;

internal sealed class RawCacheLoadResultDto
{
    public List<string> FolderPaths { get; set; } = new();

    public List<RawMediaRecordDto> Records { get; set; } = new();
}
