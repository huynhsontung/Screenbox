using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for general application settings such as theme, file handling, and history.
/// </summary>
public sealed partial class GeneralSettingsContext : ObservableRecipient
{
    private const string GeneralThemeKey = "General/Theme";
    private const string GeneralEnqueueAllInFolderKey = "General/EnqueueAllInFolder";
    private const string GeneralFrameCaptureFolderTokenKey = "General/FrameCaptureFolderToken";
    private const string GeneralShowRecentKey = "General/ShowRecent";

    private readonly ISettingsStorage _storage;
    private readonly bool _isInitialized;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial ThemeOption Theme { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool EnqueueAllFilesInFolder { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string FrameCaptureFolderToken { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ShowRecent { get; set; }

    public GeneralSettingsContext(ISettingsStorage storage)
    {
        _storage = storage;
        Theme = (ThemeOption)_storage.GetValue(GeneralThemeKey, (int)ThemeOption.Auto);
        EnqueueAllFilesInFolder = _storage.GetValue(GeneralEnqueueAllInFolderKey, false);
        FrameCaptureFolderToken = _storage.GetValue(GeneralFrameCaptureFolderTokenKey, string.Empty);
        ShowRecent = _storage.GetValue(GeneralShowRecentKey, true);
        _isInitialized = true;
        IsActive = true;
    }

    partial void OnThemeChanged(ThemeOption value)
    {
        if (_isInitialized) _storage.SetValue(GeneralThemeKey, (int)value);
    }

    partial void OnEnqueueAllFilesInFolderChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(GeneralEnqueueAllInFolderKey, value);
    }

    partial void OnFrameCaptureFolderTokenChanged(string value)
    {
        if (_isInitialized) _storage.SetValue(GeneralFrameCaptureFolderTokenKey, value);
    }

    partial void OnShowRecentChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(GeneralShowRecentKey, value);
    }
}
