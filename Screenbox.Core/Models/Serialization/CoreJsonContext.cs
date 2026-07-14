using System;
using System.Text.Json.Serialization;

namespace Screenbox.Core.Models.Serialization;

[Obsolete("Remove the class in version 1.0, after giving older versions enough time to migrate away from Protobuf.")]
[JsonSourceGenerationOptions( GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(PlaylistRecordDto))]
[JsonSerializable(typeof(RawMediaRecordDto))]
internal sealed partial class CoreJsonContext : JsonSerializerContext
{
}
