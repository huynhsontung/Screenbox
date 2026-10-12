using Screenbox.Core.Enums;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for general application settings such as theme, file handling, and history.
/// </summary>
public sealed partial class GeneralSettingsContext : SettingsContextBase
{
    private const string GeneralThemeKey = "General/Theme";
    private const string GeneralEnqueueAllInFolderKey = "General/EnqueueAllInFolder";
    private const string GeneralFrameCaptureFolderTokenKey = "General/FrameCaptureFolderToken";
    private const string GeneralShowRecentKey = "General/ShowRecent";

    public ThemeOption Theme
    {
        get => Get(GeneralThemeKey, ThemeOption.Auto);
        set => Set(GeneralThemeKey, value);
    }

    public bool EnqueueAllFilesInFolder
    {
        get => Get(GeneralEnqueueAllInFolderKey, false);
        set => Set(GeneralEnqueueAllInFolderKey, value);
    }

    public string FrameCaptureFolderToken
    {
        get => Get(GeneralFrameCaptureFolderTokenKey, string.Empty);
        set => Set(GeneralFrameCaptureFolderTokenKey, value);
    }

    public bool ShowRecent
    {
        get => Get(GeneralShowRecentKey, true);
        set => Set(GeneralShowRecentKey, value);
    }

    public GeneralSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
