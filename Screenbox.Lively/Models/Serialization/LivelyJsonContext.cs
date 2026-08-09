using System.Text.Json.Serialization;

namespace Screenbox.Lively.Models.Serialization;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(LivelyInfoModel))]
[JsonSerializable(typeof(LivelyMusicModel))]
[JsonSerializable(typeof(LivelyPlaybackStateModel))]
internal sealed partial class LivelyJsonContext : JsonSerializerContext
{
}
