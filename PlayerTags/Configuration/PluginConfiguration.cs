using Dalamud.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Pilz.Dalamud.ActivityContexts;
using Pilz.Dalamud.Nameplates.Tools;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace PlayerTags.Configuration
{
    [Serializable]
    public partial class PluginConfiguration : IPluginConfiguration
    {
        private const int DEFAULT_CONFIG_VERSION = 1;

        [JsonProperty]
        public int RootVersion { get; private set; } = DEFAULT_CONFIG_VERSION;
        public int Version { get; set; } = DEFAULT_CONFIG_VERSION;
        public bool IsVisible = false;

        public ZoneConfiguration<GeneralConfiguration> GeneralConfigs = new();
        public ZoneConfiguration<TagsConfiguration> TagsConfigs = new();
        public StatusIconPriorizerSettings StatusIconPriorizerSettings = new(true);
        public bool MoveStatusIconToNameplateTextIfPossible = true;
        public bool IsPlayerNameRandomlyGenerated = false;
        public bool IsCustomTagsContextMenuEnabled = true;
        public bool IsShowInheritedPropertiesEnabled = true;
        public bool IsPlayersTabOrderedByProximity = false;
        public bool IsPlayersTabSelfVisible = true;
        public bool IsPlayersTabFriendsVisible  = true;
        public bool IsPlayersTabPartyVisible = true;
        public bool IsPlayersTabAllianceVisible = true;
        public bool IsPlayersTabEnemiesVisible = true;
        public bool IsPlayersTabOthersVisible = false;
        public bool IsSimpleUIEnabled = true;

        #region Obsulate Properties

        [JsonProperty("GeneralOptionsV2"), Obsolete]
        private Dictionary<ActivityType, GeneralConfiguration> GeneralOptionsV2
        {
            set
            {
                void copyOverSettings(ActivityType srcType, ZoneType destType)
                {
                    var src = value[srcType];
                    var dest = GeneralConfigs.GetConfig(destType);
                    dest.IsApplyTagsToAllChatMessagesEnabled = src.IsApplyTagsToAllChatMessagesEnabled;
                    dest.NameplateDeadPlayerHandling = src.NameplateDeadPlayerHandling;
                    dest.NameplateFreeCompanyVisibility = src.NameplateFreeCompanyVisibility;
                    dest.NameplateTitlePosition = src.NameplateTitlePosition;
                    dest.NameplateTitleVisibility = src.NameplateTitleVisibility;
                }

                copyOverSettings(ActivityType.None, ZoneType.Everywhere);
                copyOverSettings(ActivityType.None, ZoneType.Overworld);
                copyOverSettings(ActivityType.PvpDuty, ZoneType.Pvp);
                copyOverSettings(ActivityType.PveDuty, ZoneType.Doungen);
                copyOverSettings(ActivityType.PveDuty, ZoneType.Raid);
                copyOverSettings(ActivityType.PveDuty, ZoneType.AllianceRaid);
                copyOverSettings(ActivityType.PveDuty, ZoneType.Foray);
            }
        }

        [JsonProperty]
        [Obsolete]
        public bool IsApplyToEverywhereEnabled
        {
            set
            {
                GeneralConfigs.IsEverywhere = value;
                TagsConfigs.IsEverywhere = value;
            }
        }

        [JsonProperty]
        [Obsolete]
        private bool IsGeneralOptionsAllTheSameEnabled
        {
            set => IsApplyToEverywhereEnabled = value;
        }

        [JsonProperty("NameplateFreeCompanyVisibility"), Obsolete]
        private NameplateFreeCompanyVisibility NameplateFreeCompanyVisibilityV1
        {
            set => GeneralConfigs.GetConfig(ZoneType.Everywhere).NameplateFreeCompanyVisibility = value;
        }
        [JsonProperty("NameplateTitleVisibility"), Obsolete]
        public NameplateTitleVisibility NameplateTitleVisibilityV1
        {
            set => GeneralConfigs.GetConfig(ZoneType.Everywhere).NameplateTitleVisibility = value;
        }
        [JsonProperty("NameplateTitlePosition"), Obsolete]
        public NameplateTitlePosition NameplateTitlePositionV1
        {
            set => GeneralConfigs.GetConfig(ZoneType.Everywhere).NameplateTitlePosition = value;
        }

        [JsonProperty("IsApplyTagsToAllChatMessagesEnabled"), Obsolete]
        private bool IsApplyTagsToAllChatMessagesEnabledV1
        {
            set => GeneralConfigs.GetConfig(ZoneType.Everywhere).IsApplyTagsToAllChatMessagesEnabled = value;
        }

        #endregion

        public event System.Action? Saved;

        public void Save(PluginData pluginData)
        {
            foreach (var tagConfig in TagsConfigs)
                tagConfig.Value.ApplyTagsData(pluginData.GetTagsData(tagConfig.Key));

            SavePluginConfig();

            Saved?.Invoke();
        }

        public void SavePluginConfig()
        {
            Version = DEFAULT_CONFIG_VERSION;
            var configFilePath = GetConfigFilePath();
            var configFileContent = JsonConvert.SerializeObject(this, Formatting.Indented, GetJsonSettings());
            File.WriteAllText(configFilePath, configFileContent);
        }

        public static PluginConfiguration LoadPluginConfig()
        {
            var configFilePath = GetConfigFilePath();
            object config = null;

            if (File.Exists(configFilePath))
            {
                var configFileContent = File.ReadAllText(configFilePath);
                config = JsonConvert.DeserializeObject<PluginConfiguration>(configFileContent, GetJsonSettings());
            }
            else
            {
                // Try loading the old settings, if possible
                configFilePath = PluginServices.DalamudPluginInterface.ConfigFile.FullName;
                config = PluginServices.DalamudPluginInterface.GetPluginConfig();
            }

            return config as PluginConfiguration;
        }

        private static string GetConfigFilePath()
        {
            return Path.Combine(PluginServices.DalamudPluginInterface.ConfigDirectory.FullName, "Config.json");
        }

        private static JsonSerializerSettings GetJsonSettings()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Auto,
            };

            jsonSettings.Converters.Add(new StringEnumConverter());

            return jsonSettings;
        }
    }
}
