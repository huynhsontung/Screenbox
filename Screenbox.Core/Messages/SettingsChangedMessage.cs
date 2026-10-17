using System;

namespace Screenbox.Core.Messages;

[Obsolete("Use domain-specific settings contexts and PropertyChangedMessage instead.")]
public sealed record SettingsChangedMessage(string SettingsName, Type Origin)
{
    public Type Origin { get; } = Origin;
    public string SettingsName { get; } = SettingsName;
}
