using Dalamud.Game.Text.SeStringHandling;
using Pilz.Dalamud.Nameplates.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.EventArgs
{
    public class AddonNamePlate_SetPlayerNameManagedEventArgs : HookWithResultManagedBaseEventArgs<IntPtr>
    {
        public new AddonNamePlate_SetPlayerNameEventArgs OriginalEventArgs
        {
            get => base.OriginalEventArgs as AddonNamePlate_SetPlayerNameEventArgs;
            set => base.OriginalEventArgs = value;
        }

        public SafeNameplateObject SafeNameplateObject { get; set; }
        public SeString Title { get; internal set; }
        public SeString Name { get; internal set; }
        public SeString FreeCompany { get; internal set; }

        public bool IsTitleAboveName
        {
            get => OriginalEventArgs.IsTitleAboveName;
            set => OriginalEventArgs.IsTitleAboveName = value;
        }

        public bool IsTitleVisible
        {
            get => OriginalEventArgs.IsTitleVisible;
            set => OriginalEventArgs.IsTitleVisible = value;
        }

        public int IconID
        {
            get => OriginalEventArgs.IconID;
            set => OriginalEventArgs.IconID = value;
        }
    }
}
