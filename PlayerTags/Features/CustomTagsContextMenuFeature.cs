using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Dalamud.Plugin;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Features
{
    /// <summary>
    /// A feature that adds options for the management of custom tags to context menus.
    /// </summary>
    public class CustomTagsContextMenuFeature : FeatureBase, IDisposable
    {
        private string?[] SupportedAddonNames = new string?[]
        {
            null,
            "_PartyList",
            "ChatLog",
            "ContactList",
            "ContentMemberList",
            "CrossWorldLinkshell",
            "FreeCompany",
            "FriendList",
            "LookingForGroup",
            "LinkShell",
            "PartyMemberList",
            "SocialList",
        };

        private DalamudContextMenu? m_ContextMenu;

        public CustomTagsContextMenuFeature(PluginConfiguration pluginConfiguration, PluginData pluginData, DalamudPluginInterface pluginInterface) : base(pluginConfiguration, pluginData)
        {
            m_ContextMenu = new DalamudContextMenu(pluginInterface);
            m_ContextMenu.OnOpenGameObjectContextMenu += ContextMenuHooks_ContextMenuOpened;
        }

        public void Dispose()
        {
            if (m_ContextMenu != null)
            {
                m_ContextMenu.OnOpenGameObjectContextMenu -= ContextMenuHooks_ContextMenuOpened;
                ((IDisposable)m_ContextMenu).Dispose();
                m_ContextMenu = null;
            }
        }

        private void ContextMenuHooks_ContextMenuOpened(GameObjectContextMenuOpenArgs contextMenuOpenedArgs)
        {
            if (!EnableGlobal || !pluginConfiguration.IsCustomTagsContextMenuEnabled
                || !SupportedAddonNames.Contains(contextMenuOpenedArgs.ParentAddonName))
            {
                return;
            }

            Identity? identity = pluginData.GetIdentity(contextMenuOpenedArgs);
            if (identity != null)
            {
                var allTags = new Dictionary<Tag, bool>();
                foreach (var customTag in pluginData.CustomTags)
                {
                    var isAdded = identity.CustomTagIds.Contains(customTag.CustomId.Value);
                    allTags.Add(customTag, isAdded);
                }
                
                var sortedTags = allTags.OrderBy(n => n.Value);
                foreach (var tag in sortedTags)
                {
                    string menuItemText;
                    if (tag.Value)
                        menuItemText = Strings.Loc_Static_ContextMenu_RemoveTag;
                    else
                        menuItemText = Strings.Loc_Static_ContextMenu_AddTag;
                    menuItemText = string.Format(menuItemText, tag.Key.Text.Value);

                    contextMenuOpenedArgs.AddCustomItem(
                        new GameObjectContextMenuItem(menuItemText, openedEventArgs =>
                        {
                            if (tag.Value)
                                pluginData.RemoveCustomTagFromIdentity(tag.Key, identity);
                            else
                                pluginData.AddCustomTagToIdentity(tag.Key, identity);
                            pluginConfiguration.Save(pluginData);
                        })
                        {
                            IsSubMenu = false
                        });
                }
            }
        }
    }
}
