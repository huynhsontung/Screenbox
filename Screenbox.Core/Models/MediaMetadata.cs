namespace Screenbox.Core.Models;

public sealed record MediaMetadata
{
    public string Name { get; private set; }

    public string Value { get; private set; }

    public MediaMetadata(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
