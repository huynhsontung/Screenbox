namespace Screenbox.Core.Messages
{
    public sealed record SettingsChangedMessage(string SettingsName)
    {
        public string SettingsName { get; } = SettingsName;
    }
}
