using Dalamud.ContextMenu;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Pilz.Dalamud.ActivityContexts;
using PlayerTags.Configuration;
using PlayerTags.PluginStrings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Data
{
    public class PluginData
    {
        public Dictionary<ZoneType, PluginTagsData> TagsData = new();
        private readonly PluginConfiguration pluginConfiguration;

        public PluginData(PluginConfiguration pluginConfiguration)
        {
            this.pluginConfiguration = pluginConfiguration;
            ReloadDefault();
        }

        public PluginTagsData GetTagsData(ZoneType zoneType)
        {
            return TagsData[zoneType];
        }

        public void ReloadDefault(ZoneType zoneType)
        {
            if (TagsData[zoneType].ReloadDefault())
                pluginConfiguration.Save(this);
        }

        public void ReloadDefault()
        {
            var needToSave = false;
            
            foreach (var tagsData in TagsData.Values)
                needToSave |= tagsData.ReloadDefault();

            if (needToSave)
                pluginConfiguration.Save(this);
        }
    }
}
