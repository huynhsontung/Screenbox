using System.Text.Json.Serialization;

namespace Screenbox.Lively.Models;

[JsonSerializable(typeof(LivelyInfoModel), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(LivelyMusicModel), GenerationMode = JsonSourceGenerationMode.Metadata)]
internal sealed partial class LivelyJsonContext : JsonSerializerContext
{
}
