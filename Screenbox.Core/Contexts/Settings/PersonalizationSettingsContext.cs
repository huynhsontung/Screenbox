using Screenbox.Core.Enums;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for personalization and appearance settings.
/// </summary>
public sealed partial class PersonalizationSettingsContext : SettingsContextBase
{
    private const string ThemeKey = "General/Theme";

    public ThemeOption Theme
    {
        get => Get(ThemeKey, ThemeOption.Auto);
        set => Set(ThemeKey, value);
    }

    public PersonalizationSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
