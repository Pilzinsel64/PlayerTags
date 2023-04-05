using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Nameplates.Tools
{
    public class NameplateChangesProps
    {
        /// <summary>
        /// All the changes to the nameplate that should be made.
        /// </summary>
        public NameplateChanges Changes { get; set; }

        public NameplateChangesProps()
        {
        }

        public NameplateChangesProps(NameplateChanges changes) : this()
        {
            Changes = changes;
        }
    }
}
