using PlayerTags.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTags.Configuration
{
    public class GeneralConfiguration : ZoneConfigurationBase
    {
        public NameplateFreeCompanyVisibility NameplateFreeCompanyVisibility = NameplateFreeCompanyVisibility.Default;
        public NameplateTitleVisibility NameplateTitleVisibility = NameplateTitleVisibility.WhenHasTags;
        public NameplateTitlePosition NameplateTitlePosition = NameplateTitlePosition.AlwaysAboveName;
        public DeadPlayerHandling NameplateDeadPlayerHandling = DeadPlayerHandling.Include;
        public bool IsApplyTagsToAllChatMessagesEnabled = true;
    }
}
