#nullable enable

using ProtoBuf;

namespace Screenbox.Casting.Chromecast.Protocol;

[ProtoContract]
internal sealed class CastChannelMessage
{
    [ProtoContract]
    internal enum ProtocolVersion
    {
        [ProtoEnum(Name = "CASTV2_1_0")]
        CastV210 = 0,
    }

    [ProtoContract]
    internal enum PayloadType
    {
        [ProtoEnum(Name = "STRING")]
        String = 0,

        [ProtoEnum(Name = "BINARY")]
        Binary = 1,
    }

    [ProtoMember(1, IsRequired = true)]
    public ProtocolVersion protocol_version { get; set; } = ProtocolVersion.CastV210;

    [ProtoMember(2, IsRequired = true)]
    public string source_id { get; set; } = string.Empty;

    [ProtoMember(3, IsRequired = true)]
    public string destination_id { get; set; } = string.Empty;

    [ProtoMember(4, IsRequired = true)]
    public string @namespace { get; set; } = string.Empty;

    [ProtoMember(5, IsRequired = true)]
    public PayloadType payload_type { get; set; } = PayloadType.String;

    [ProtoMember(6)]
    public string? payload_utf8 { get; set; }

    [ProtoMember(7)]
    public byte[]? payload_binary { get; set; }
}
