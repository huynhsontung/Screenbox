using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for advanced player settings and arguments.
/// </summary>
public sealed partial class AdvancedSettingsContext : ObservableRecipient
{
    private const string AdvancedModeKey = "Advanced/IsEnabled";
    private const string AdvancedVideoUpscaleKey = "Advanced/VideoUpscale";
    private const string AdvancedMultipleInstancesKey = "Advanced/MultipleInstances";
    private const string GlobalArgumentsKey = "Values/GlobalArguments";

    private readonly ISettingsStorage _storage;
    private readonly bool _isInitialized;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool AdvancedMode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial VideoUpscaleOption VideoUpscale { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool UseMultipleInstances { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string GlobalArguments { get; set; }

    public AdvancedSettingsContext(ISettingsStorage storage)
    {
        _storage = storage;
        AdvancedMode = _storage.GetValue(AdvancedModeKey, false);
        VideoUpscale = (VideoUpscaleOption)_storage.GetValue(AdvancedVideoUpscaleKey, (int)VideoUpscaleOption.Linear);
        UseMultipleInstances = _storage.GetValue(AdvancedMultipleInstancesKey, false);
        GlobalArguments = _storage.GetValue(GlobalArgumentsKey, string.Empty);
        _isInitialized = true;
    }

    partial void OnAdvancedModeChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(AdvancedModeKey, value);
    }

    partial void OnVideoUpscaleChanged(VideoUpscaleOption value)
    {
        if (_isInitialized) _storage.SetValue(AdvancedVideoUpscaleKey, (int)value);
    }

    partial void OnUseMultipleInstancesChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(AdvancedMultipleInstancesKey, value);
    }

    partial void OnGlobalArgumentsChanged(string value)
    {
        if (!_isInitialized) return;

        string sanitized = SanitizeArguments(value);
        if (sanitized != value)
        {
            GlobalArguments = sanitized;
            return;
        }

        _storage.SetValue(GlobalArgumentsKey, sanitized);
    }

    private static string SanitizeArguments(string raw)
    {
        string[] args = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.StartsWith('-') && s != "--").ToArray();
        return string.Join(' ', args);
    }
}
