using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pilz.Dalamud.Nameplates.EventArgs;
using Pilz.Dalamud.Tools.Strings;

namespace Pilz.Dalamud.Nameplates.Tools
{
    public class NameplateChanges
    {
        private readonly Dictionary<NameplateElements, StringChangesProps> changes = new();

        public NameplateChanges()
        {
            changes.Add(NameplateElements.Title, new());
            changes.Add(NameplateElements.Name, new());
            changes.Add(NameplateElements.FreeCompany, new());
        }

        public NameplateChanges(AddonNamePlate_SetPlayerNameManagedEventArgs eventArgs) : this()
        {
            GetProps(NameplateElements.Title).Destination = eventArgs.Title;
            GetProps(NameplateElements.Name).Destination = eventArgs.Name;
            GetProps(NameplateElements.FreeCompany).Destination = eventArgs.FreeCompany;
        }

        /// <summary>
        /// Gets the properties with the changes of an element of your choice where you can add your payloads to a change and setup some options.
        /// </summary>
        /// <param name="element">The position of your choice.</param>
        /// <returns></returns>
        public StringChangesProps GetProps(NameplateElements element)
        {
            return changes[element];
        }

        /// <summary>
        /// Gets the changes of an element of your choice where you can add your payloads to a change.
        /// </summary>
        /// <param name="element">The position of your choice.</param>
        /// <returns></returns>
        public StringChanges GetChanges(NameplateElements element)
        {
            return GetProps(element).StringChanges;
        }

        /// <summary>
        /// Gets a change of the position of the element of your choice where you can add your payloads.
        /// </summary>
        /// <param name="element">The position of your choice.</param>
        /// <returns></returns>
        public StringChange GetChange(NameplateElements element, StringPosition position)
        {
            return GetChanges(element).GetChange(position);
        }
    }
}
