using Newtonsoft.Json;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTags.Configuration
{
    public class TagsConfiguration : ZoneConfigurationBase
    {
        public DefaultPluginDataTemplate DefaultPluginDataTemplate = DefaultPluginDataTemplate.Simple;

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

        public void ApplyTagsData(PluginTagsData tagsData)
        {
            AllTagsChanges = tagsData.AllTags.GetChanges(tagsData.Default.AllTags.GetChanges());
            AllRoleTagsChanges = tagsData.AllRoleTags.GetChanges(tagsData.Default.AllRoleTags.GetChanges());

            RoleTagsChanges = new Dictionary<Role, Dictionary<string, InheritableData>>();
            foreach ((var role, var roleTag) in tagsData.RoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (tagsData.Default.RoleTags.TryGetValue(role, out var defaultTag))
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
            foreach ((var dpsRole, var dpsRoleTag) in tagsData.DpsRoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (tagsData.Default.DpsRoleTags.TryGetValue(dpsRole, out var defaultTag))
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
            foreach ((var rangedDpsRole, var rangedDpsRoleTag) in tagsData.RangedDpsRoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (tagsData.Default.RangedDpsRoleTags.TryGetValue(rangedDpsRole, out var defaultTag))
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
            foreach ((var landHandRole, var landHandRoleTag) in tagsData.LandHandRoleTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (tagsData.Default.LandHandRoleTags.TryGetValue(landHandRole, out var defaultTag))
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
            foreach ((var jobAbbreviation, var jobTag) in tagsData.JobTags)
            {
                Dictionary<string, InheritableData>? defaultChanges = new Dictionary<string, InheritableData>();
                if (tagsData.Default.JobTags.TryGetValue(jobAbbreviation, out var defaultTag))
                {
                    defaultChanges = defaultTag.GetChanges();
                }

                var changes = jobTag.GetChanges(defaultChanges);
                if (changes.Any())
                {
                    JobTagsChanges[jobAbbreviation] = changes;
                }
            }

            AllCustomTagsChanges = tagsData.AllCustomTags.GetChanges(tagsData.Default.AllCustomTags.GetChanges());

            CustomTagsChanges = new List<Dictionary<string, InheritableData>>();
            foreach (var customTag in tagsData.CustomTags)
            {
                CustomTagsChanges.Add(customTag.GetChanges());
            }

            Identities = tagsData.Identities;
        }
    }
}
