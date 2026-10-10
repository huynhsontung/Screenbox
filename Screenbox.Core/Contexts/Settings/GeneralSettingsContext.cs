using Screenbox.Core.Enums;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for general application settings, shell appearance, history,
/// instance lifecycle, and catch-all preferences that do not belong to a more specific domain.
/// </summary>
public sealed partial class GeneralSettingsContext : SettingsContextBase<GeneralSettingsContext>
{
    private const string ThemeKey = "General/Theme";
    private const string RequireDeleteConfirmationKey = "Personalization/RequireDeleteConfirmation";
    private const string ShowRecentKey = "General/ShowRecent";
    private const string EnqueueAllInFolderKey = "General/EnqueueAllInFolder";
    private const string MultipleInstancesKey = "Advanced/MultipleInstances";

    public ThemeOption Theme
    {
        get => Get(ThemeKey, ThemeOption.Auto);
        set => Set(ThemeKey, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the delete command asks for confirmation
    /// before deleting a file.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the delete requires confirmation; otherwise, <see langword="false"/>.
    /// </returns>
    public bool RequireDeleteConfirmation
    {
        get => Get(RequireDeleteConfirmationKey, true);
        set => Set(RequireDeleteConfirmationKey, value);
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
