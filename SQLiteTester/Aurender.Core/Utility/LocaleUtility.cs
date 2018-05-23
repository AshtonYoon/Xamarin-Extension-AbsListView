using System;

namespace Aurender.Core.Utility
{
    public static class LocaleUtility
    {
        public static Func<string, string> RequestTranslation;
        public static string TranslateResult = string.Empty;

        public static string GetCurrentRegion()
        {
            var r = System.Globalization.RegionInfo.CurrentRegion;
            var c = System.Globalization.CultureInfo.CurrentCulture;

            return $"{c.TwoLetterISOLanguageName}-{r.TwoLetterISORegionName}";
        }

        public static string Translate(string message)
        {
            return RequestTranslation?.Invoke(message) ?? message;
        }
    }
}
