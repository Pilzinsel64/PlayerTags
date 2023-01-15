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
    public class PluginConfiguration : IPluginConfiguration
    {
        private const int DEFAULT_CONFIG_VERSION = 1;

        [JsonProperty]
        public int RootVersion { get; private set; } = DEFAULT_CONFIG_VERSION;
        public int Version { get; set; } = DEFAULT_CONFIG_VERSION;
        public bool IsVisible = false;

        [JsonProperty("GeneralOptionsV2"), Obsolete]
        private Dictionary<ActivityType, GeneralOptionsClass> GeneralOptionsV2
        {
            set
            {
                void copyOverSettings(ActivityType srcType, ZoneType destType)
                {
                    var src = value[srcType];
                    var dest = GeneralOptions[destType];
                    dest.IsApplyTagsToAllChatMessagesEnabled = src.IsApplyTagsToAllChatMessagesEnabled;
                    dest.NameplateDeadPlayerHandling = src.NameplateDeadPlayerHandling;
                    dest.NameplateFreeCompanyVisibility = src.NameplateFreeCompanyVisibility;
                    dest.NameplateTitlePosition = src.NameplateTitlePosition;
                    dest.NameplateTitleVisibility = src.NameplateTitleVisibility;
                }

                copyOverSettings(ActivityType.None, ZoneType.Overworld);
                copyOverSettings(ActivityType.PvpDuty, ZoneType.Pvp);
                copyOverSettings(ActivityType.PveDuty, ZoneType.Dungeon);
                copyOverSettings(ActivityType.PveDuty, ZoneType.Raid);
                copyOverSettings(ActivityType.PveDuty, ZoneType.AllianceRaid);
                copyOverSettings(ActivityType.PveDuty, ZoneType.Foray);
            }
        }

        [JsonProperty("GeneralOptionsV3")]
        public Dictionary<ZoneType, GeneralOptionsClass> GeneralOptions = new()
        {
            { ZoneType.Overworld, new GeneralOptionsClass() }
        };

        public DefaultPluginDataTemplate DefaultPluginDataTemplate = DefaultPluginDataTemplate.Simple;
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
        public bool IsGeneralOptionsAllTheSameEnabled = true;

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, InheritableData> AllTagsChanges = new Dictionary<string, InheritableData>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, InheritableData> AllRoleTagsChanges = new Dictionary<string, InheritableData>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<Role, Dictionary<string, InheritableData>> RoleTagsChanges = new Dictionary<Role, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<DpsRole, Dictionary<string, InheritableData>> DpsRoleTagsChanges = new Dictionary<DpsRole, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<RangedDpsRole, Dictionary<string, InheritableData>> RangedDpsRoleTagsChanges = new Dictionary<RangedDpsRole, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<LandHandRole, Dictionary<string, InheritableData>> LandHandRoleTagsChanges = new Dictionary<LandHandRole, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, Dictionary<string, InheritableData>> JobTagsChanges = new Dictionary<string, Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public Dictionary<string, InheritableData> AllCustomTagsChanges = new Dictionary<string, InheritableData>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public List<Dictionary<string, InheritableData>> CustomTagsChanges = new List<Dictionary<string, InheritableData>>();

        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
        public List<Identity> Identities = new List<Identity>();

        #region Obsulate Properties

        [JsonProperty("NameplateFreeCompanyVisibility"), Obsolete]
        private NameplateFreeCompanyVisibility NameplateFreeCompanyVisibilityV1
        {
            set
            {
                foreach (var key in GeneralOptions.Keys)
                    GeneralOptions[key].NameplateFreeCompanyVisibility = value;
            }
        }
        [JsonProperty("NameplateTitleVisibility"), Obsolete]
        public NameplateTitleVisibility NameplateTitleVisibilityV1
        {
            set
            {
                foreach (var key in GeneralOptions.Keys)
                    GeneralOptions[key].NameplateTitleVisibility = value;
            }
        }
        [JsonProperty("NameplateTitlePosition"), Obsolete]
        public NameplateTitlePosition NameplateTitlePositionV1
        {
            set
            {
                foreach (var key in GeneralOptions.Keys)
                    GeneralOptions[key].NameplateTitlePosition = value;
            }
        }

        [JsonProperty("IsApplyTagsToAllChatMessagesEnabled"), Obsolete]
        private bool IsApplyTagsToAllChatMessagesEnabledV1
        {
            set
            {
                foreach (var key in GeneralOptions.Keys)
                    GeneralOptions[key].IsApplyTagsToAllChatMessagesEnabled = value;
            }
        }

        #endregion

        public event System.Action? Saved;

        public void Save(PluginData pluginData)
        {
            AllTagsChanges = pluginData.AllTags.GetChanges(pluginData.Default.AllTags.GetChanges());
            AllRoleTagsChanges = pluginData.AllRoleTags.GetChanges(pluginData.Default.AllRoleTags.GetChanges());

            RoleTagsChanges = new Dictionary<Role, Dictionary<string, InheritableData>>();
            foreach ((var role, var roleTag) in pluginData.RoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (pluginData.Default.RoleTags.TryGetValue(role, out var defaultTag))
                {
                    defaultChanges = defaultTag.GetChanges();
                }

                var changes = roleTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    RoleTagsChanges[role] = changes;
                }
            }

            DpsRoleTagsChanges = new Dictionary<DpsRole, Dictionary<string, InheritableData>>();
            foreach ((var dpsRole, var dpsRoleTag) in pluginData.DpsRoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (pluginData.Default.DpsRoleTags.TryGetValue(dpsRole, out var defaultTag))
                {
                    defaultChanges = defaultTag.GetChanges();
                }

                var changes = dpsRoleTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    DpsRoleTagsChanges[dpsRole] = changes;
                }
            }

            RangedDpsRoleTagsChanges = new Dictionary<RangedDpsRole, Dictionary<string, InheritableData>>();
            foreach ((var rangedDpsRole, var rangedDpsRoleTag) in pluginData.RangedDpsRoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (pluginData.Default.RangedDpsRoleTags.TryGetValue(rangedDpsRole, out var defaultTag))
                {
                    defaultChanges = defaultTag.GetChanges();
                }

                var changes = rangedDpsRoleTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    RangedDpsRoleTagsChanges[rangedDpsRole] = changes;
                }
            }

            LandHandRoleTagsChanges = new Dictionary<LandHandRole, Dictionary<string, InheritableData>>();
            foreach ((var landHandRole, var landHandRoleTag) in pluginData.LandHandRoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (pluginData.Default.LandHandRoleTags.TryGetValue(landHandRole, out var defaultTag))
                {
                    defaultChanges = defaultTag.GetChanges();
                }

                var changes = landHandRoleTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    LandHandRoleTagsChanges[landHandRole] = changes;
                }
            }

            JobTagsChanges = new Dictionary<string, Dictionary<string, InheritableData>>();
            foreach ((var jobAbbreviation, var jobTag) in pluginData.JobTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (pluginData.Default.JobTags.TryGetValue(jobAbbreviation, out var defaultTag))
                {
                    defaultChanges = defaultTag.GetChanges();
                }

                var changes = jobTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    JobTagsChanges[jobAbbreviation] = changes;
                }
            }

            AllCustomTagsChanges = pluginData.AllCustomTags.GetChanges(pluginData.Default.AllCustomTags.GetChanges());

            CustomTagsChanges = new List<Dictionary<string, InheritableData>>();
            foreach (var customTag in pluginData.CustomTags)
            {
                CustomTagsChanges.Add(customTag.GetChanges());
            }

            Identities = pluginData.Identities;

            SavePluginConfig();

            Saved?.Invoke();
        }

        private void SavePluginConfig()
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

    public class GeneralOptionsClass
    {
        public NameplateFreeCompanyVisibility NameplateFreeCompanyVisibility = NameplateFreeCompanyVisibility.Default;
        public NameplateTitleVisibility NameplateTitleVisibility = NameplateTitleVisibility.WhenHasTags;
        public NameplateTitlePosition NameplateTitlePosition = NameplateTitlePosition.AlwaysAboveName;
        public DeadPlayerHandling NameplateDeadPlayerHandling = DeadPlayerHandling.Include;
        public bool IsApplyTagsToAllChatMessagesEnabled = true;
    }
}
