using System;
using System.Text.Json.Serialization;

namespace Screenbox.Core.Models.Serialization;

[Obsolete("Remove the class once the transition from Protobuf to Microsoft.Data.Sqlite has been in place for some time.")]
[JsonSourceGenerationOptions( GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(PlaylistRecordDto))]
[JsonSerializable(typeof(RawMediaRecordDto))]
internal sealed partial class CoreJsonContext : JsonSerializerContext
{
}
