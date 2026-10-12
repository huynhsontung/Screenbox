using System;
using System.Linq;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for advanced player settings and arguments.
/// </summary>
public sealed partial class AdvancedSettingsContext : SettingsContextBase
{
    private const string AdvancedModeKey = "Advanced/IsEnabled";
    private const string AdvancedVideoUpscaleKey = "Advanced/VideoUpscale";
    private const string AdvancedMultipleInstancesKey = "Advanced/MultipleInstances";
    private const string GlobalArgumentsKey = "Values/GlobalArguments";

    public bool AdvancedMode
    {
        get => Get(AdvancedModeKey, false);
        set => Set(AdvancedModeKey, value);
    }

    public VideoUpscaleOption VideoUpscale
    {
        get => Get(AdvancedVideoUpscaleKey, VideoUpscaleOption.Linear);
        set => Set(AdvancedVideoUpscaleKey, value);
    }

    public bool UseMultipleInstances
    {
        get => Get(AdvancedMultipleInstancesKey, false);
        set => Set(AdvancedMultipleInstancesKey, value);
    }

    public string GlobalArguments
    {
        get => Get(GlobalArgumentsKey, string.Empty);
        set
        {
            string sanitized = SanitizeArguments(value);
            if (!Set(GlobalArgumentsKey, sanitized) && sanitized != value)
            {
                OnPropertyChanged();
            }
        }
    }

    public AdvancedSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }

    private static string SanitizeArguments(string raw)
    {
        string[] args = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.StartsWith('-') && s != "--").ToArray();
        return string.Join(' ', args);
    }
}
