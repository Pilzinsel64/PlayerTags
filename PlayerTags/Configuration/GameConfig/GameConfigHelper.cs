using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTags.Configuration.GameConfig
{
    public class GameConfigHelper
    {
        private static GameConfigHelper instance = null;
        private unsafe static ConfigModule* configModule = null;

        public static GameConfigHelper Instance
        {
            get
            {
                instance ??= new GameConfigHelper();
                return instance;
            }
        }

        private GameConfigHelper()
        {
            unsafe
            {
                configModule = ConfigModule.Instance();
            }
        }

        private uint? GetIntValue(ConfigOption option)
        {
            if (PluginServices.GameConfig.UiConfig.TryGetUInt(nameof(ConfigOption.LogNameType), out var value))
                return value;
            return null;
        }

        public LogNameType? GetLogNameType()
        {
            uint? value = GetIntValue(ConfigOption.LogNameType);
            if (value != null)
                return (LogNameType)value;
            return null;
        }
    }
}
