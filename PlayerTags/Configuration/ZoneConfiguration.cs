using Newtonsoft.Json;
using Pilz.Dalamud.ActivityContexts;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTags.Configuration
{
    public class ZoneConfiguration<TConfig> : Dictionary<ZoneType, TConfig> where TConfig : ZoneConfigurationBase, new()
    {
        public bool IsEverywhere { get; set; } = true;

        public TConfig GetConfig(ZoneType zoneType)
        {
            if (IsEverywhere)
                zoneType = ZoneType.Everywhere;

            if (!ContainsKey(zoneType))
                Add(zoneType, new TConfig());

            return this[zoneType];
        }
    }
}
