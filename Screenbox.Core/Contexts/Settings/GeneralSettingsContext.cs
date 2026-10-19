using Screenbox.Core.Services;
using Windows.UI.Xaml;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for general application settings, shell appearance, history,
/// instance lifecycle, and catch-all preferences that do not belong to a more specific domain.
/// </summary>
public sealed partial class GeneralSettingsContext : SettingsContextBase<GeneralSettingsContext>
{
    private const string ThemeKey = "General/Theme";
    private const string ShowRecentKey = "General/ShowRecent";
    private const string EnqueueAllInFolderKey = "General/EnqueueAllInFolder";
    private const string MultipleInstancesKey = "Advanced/MultipleInstances";

    public ElementTheme Theme
    {
        get => Get(ThemeKey, ElementTheme.Default);
        set => Set(ThemeKey, value);
    }

    public bool ShowRecent
    {
        get => Get(ShowRecentKey, true);
        set => Set(ShowRecentKey, value);
    }

    public bool EnqueueAllFilesInFolder
    {
        get => Get(EnqueueAllInFolderKey, false);
        set => Set(EnqueueAllInFolderKey, value);
    }

    public bool UseMultipleInstances
    {
        get => Get(MultipleInstancesKey, false);
        set => Set(MultipleInstancesKey, value);
    }

    public GeneralSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
