namespace Screenbox.Core.Messages
{
    internal sealed record SettingsChangedMessage(string SettingsName)
    {
        public string SettingsName { get; } = SettingsName;
    }
}
