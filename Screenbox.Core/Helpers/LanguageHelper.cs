using System.Linq;
using Windows.ApplicationModel.Resources;

namespace Screenbox.Core.Helpers;
internal static class LanguageHelper
{
    private static readonly ResourceLoader Loader = ResourceLoader.GetForViewIndependentUse("Screenbox.Core/LanguageCodes");

    public static bool TryConvertISO6392ToISO6391(string threeLetterTag, out string twoLetterTag)
    {
        twoLetterTag = string.Empty;
        if (threeLetterTag.Length != 3) return false;
        twoLetterTag = Loader.GetString(threeLetterTag);
        return !string.IsNullOrEmpty(twoLetterTag);
    }

    public static string GetPreferredLanguage()
    {
        return Windows.System.UserProfile.GlobalizationPreferences.Languages.FirstOrDefault() ?? string.Empty;
    }
}
