using Dalamud.Logging;
using Dalamud.Plugin;
using PlayerTags.Resources;
using System;
using System.ComponentModel;
using System.Globalization;

namespace PlayerTags
{
    public static class Localizer
    {
        public static void SetLanguage(string langCode)
        {
            SetLanguage(new CultureInfo(langCode));
        }

        public static void SetLanguage(CultureInfo cultureInfo)
        {
            Strings.Culture = cultureInfo;
        }

        public static string GetName<TEnum>(TEnum value)
        {
            return $"{typeof(TEnum).Name}_{value}";
        }

        public static string GetString<TEnum>(bool isDescription)
            where TEnum : Enum
        {
            return GetString(typeof(TEnum).Name, isDescription);
        }

        public static string GetString<TEnum>(TEnum value, bool isDescription)
            where TEnum : Enum
        {
            return GetString(GetName(value), isDescription);
        }

        public static string GetString(string localizedStringName, bool isDescription)
        {
            string localizedStringId = $"Loc_{localizedStringName}";

            if (isDescription)
            {
                localizedStringId += "_Description";
            }

            return GetString(localizedStringId);
        }

        public static string GetString(string localizedStringId)
        {
            string? value = Strings.ResourceManager.GetString(localizedStringId, Strings.Culture);

            if (value != null)
                return value;

            PluginServices.PluginLog.Error($"Failed to get localized string for id {localizedStringId}");
            return localizedStringId;
        }
    }
}
