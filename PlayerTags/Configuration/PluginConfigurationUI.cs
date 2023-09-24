using Dalamud.Configuration;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.Havok;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Pilz.Dalamud.ActivityContexts;
using Pilz.Dalamud.Icons;
using Pilz.Dalamud.Nameplates.Model;
using Pilz.Dalamud.Nameplates.Tools;
using Pilz.Dalamud.Tools;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using PlayerTags.PluginStrings;
using PlayerTags.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Transactions;

namespace PlayerTags.Configuration
{
    public class PluginConfigurationUI
    {
        private struct PlayerInfo
        {
            public PlayerContext PlayerContext;
            public float Proximity;
        }

        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;
        private PropertyProxy propertyProxy;

        private InheritableValue<ushort>? m_ColorPickerPopupDataContext;
        private Dictionary<object, object> inheritableTEnumProxies = new();

        public PluginConfigurationUI(PluginConfiguration config, PluginData pluginData)
        {
            m_PluginConfiguration = config;
            m_PluginData = pluginData;
            propertyProxy = new PropertyProxy(config);
        }

        private static float ScalePoints(float input) => input * ImGuiHelpers.GlobalScale;

        public void Draw()
        {
            if (m_PluginConfiguration == null || !m_PluginConfiguration.IsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(600, 500), ImGuiCond.FirstUseEver);

            if (ImGui.Begin(Strings.Loc_Static_PluginName, ref m_PluginConfiguration.IsVisible))
            {
                propertyProxy.LoadData();

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.8f, 0.5f, 1));
                ImGui.TextWrapped(Strings.Loc_Static_WarningMessage);
                ImGui.PopStyleColor();

                ImGui.Spacing();
                ImGui.Spacing();
                if (ImGui.BeginTabBar("MainTabs"))
                {
                    if (ImGui.BeginTabItem(Strings.Loc_Static_General))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawCheckbox(nameof(m_PluginConfiguration.IsCustomTagsContextMenuEnabled), true, ref m_PluginConfiguration.IsCustomTagsContextMenuEnabled, () => SaveSettings());


                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawHeading(Strings.Loc_Static_CurrentActivityProfile);
                        DrawComboBox(true, true, false, ref propertyProxy.CurrentActivityContext, () => SaveSettings(false));


                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawHeading(Strings.Loc_Static_Nameplates);
                        DrawComboBox(true, true, false, ref propertyProxy.NameplateFreeCompanyVisibility, () => SaveSettings(true));
                        DrawComboBox(true, true, false, ref propertyProxy.NameplateTitleVisibility, () => SaveSettings(true));
                        DrawComboBox(true, true, false, ref propertyProxy.NameplateTitlePosition, () => SaveSettings(true));
                        DrawComboBox(true, true, false, ref propertyProxy.NameplateDeadPlayerHandling, () => SaveSettings(true));

                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawHeading(Strings.Loc_Static_Chat);
                        //DrawCheckbox(nameof(propertyProxy.IsLinkSelfInChatEnabled), true, ref propertyProxy.IsLinkSelfInChatEnabled, () => SaveSettings(true));
                        DrawCheckbox(nameof(propertyProxy.IsApplyTagsToAllChatMessagesEnabled), true, ref propertyProxy.IsApplyTagsToAllChatMessagesEnabled, () => SaveSettings(true));


                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawHeading(Strings.Loc_Static_OtherExperimental);
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayerNameRandomlyGenerated), true, ref m_PluginConfiguration.IsPlayerNameRandomlyGenerated, () => SaveSettings());

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_Tags))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawComboBox(true, true, false, ref m_PluginConfiguration.DefaultPluginDataTemplate, () =>
                        {
                            m_PluginData.ReloadDefault();
                            SaveSettings();
                        }, true, true);
                        DrawCheckbox(nameof(m_PluginConfiguration.IsShowInheritedPropertiesEnabled), true, ref m_PluginConfiguration.IsShowInheritedPropertiesEnabled, () => SaveSettings());
                        ImGui.BeginGroup();
                        ImGui.Columns(2);

                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.BeginGroup();
                        DrawTree(m_PluginData.AllTags);
                        ImGui.EndGroup();

                        ImGui.NextColumn();
                        var selectedTag = m_PluginData.AllTags.Descendents.SingleOrDefault(descendent => descendent.IsSelected.Value);
                        if (selectedTag != null)
                        {
                            DrawControls(selectedTag);
                        }

                        ImGui.EndGroup();
                        ImGui.Columns(1);

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_QuickTag))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabOrderedByProximity), true, ref m_PluginConfiguration.IsPlayersTabOrderedByProximity, () => SaveSettings());
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabSelfVisible), true, ref m_PluginConfiguration.IsPlayersTabSelfVisible, () => SaveSettings());
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabFriendsVisible), true, ref m_PluginConfiguration.IsPlayersTabFriendsVisible, () => SaveSettings());
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabPartyVisible), true, ref m_PluginConfiguration.IsPlayersTabPartyVisible, () => SaveSettings());
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabAllianceVisible), true, ref m_PluginConfiguration.IsPlayersTabAllianceVisible, () => SaveSettings());
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabEnemiesVisible), true, ref m_PluginConfiguration.IsPlayersTabEnemiesVisible, () => SaveSettings());
                        DrawCheckbox(nameof(m_PluginConfiguration.IsPlayersTabOthersVisible), true, ref m_PluginConfiguration.IsPlayersTabOthersVisible, () => SaveSettings());

                        ImGui.Spacing();
                        ImGui.Spacing();
                        if (ImGui.BeginTable("##PlayersTable", 1 + m_PluginData.CustomTags.Count))
                        {
                            ImGui.TableHeader(Strings.Loc_Static_PlayerName);
                            ImGui.TableSetupColumn(Strings.Loc_Static_PlayerName);
                            ImGui.NextColumn();
                            foreach (var customTag in m_PluginData.CustomTags)
                            {
                                ImGui.TableHeader(customTag.Text.InheritedValue);
                                ImGui.TableSetupColumn(customTag.Text.InheritedValue);
                                ImGui.NextColumn();
                            }
                            ImGui.TableHeadersRow();

                            if (PluginServices.ClientState.LocalPlayer != null)
                            {
                                Dictionary<Identity, PlayerInfo> playerNameContexts = PluginServices.ObjectTable
                                    .Where(gameObject => gameObject is PlayerCharacter)
                                    .Select(gameObject => gameObject as PlayerCharacter)
                                    .ToDictionary(
                                        playerCharacter => m_PluginData.GetIdentity(playerCharacter!),
                                        playerCharacter => new PlayerInfo()
                                        {
                                            PlayerContext = PlayerContextHelper.GetPlayerContext(playerCharacter!),
                                            Proximity = (playerCharacter!.Position - PluginServices.ClientState.LocalPlayer.Position).Length()
                                        });

                                // Include party members that aren't in the game object list
                                foreach (var partyMember in PluginServices.PartyList)
                                {
                                    var partyMemberIdentity = m_PluginData.GetIdentity(partyMember);

                                    if (!playerNameContexts.ContainsKey(partyMemberIdentity))
                                    {
                                        playerNameContexts[partyMemberIdentity] = new PlayerInfo()
                                        {
                                            PlayerContext = PlayerContext.Party,
                                            Proximity = int.MaxValue
                                        };
                                    }
                                }

                                var filteredPlayerNameContexts = playerNameContexts.Where(player => PlayerContextHelper.GetIsVisible(player.Value.PlayerContext,
                                    m_PluginConfiguration.IsPlayersTabSelfVisible,
                                    m_PluginConfiguration.IsPlayersTabFriendsVisible,
                                    m_PluginConfiguration.IsPlayersTabPartyVisible,
                                    m_PluginConfiguration.IsPlayersTabAllianceVisible,
                                    m_PluginConfiguration.IsPlayersTabEnemiesVisible,
                                    m_PluginConfiguration.IsPlayersTabOthersVisible));

                                var orderedPlayerNameContexts = filteredPlayerNameContexts.OrderBy(player => player.Key);
                                if (m_PluginConfiguration.IsPlayersTabOrderedByProximity)
                                {
                                    orderedPlayerNameContexts = filteredPlayerNameContexts.OrderBy(player => player.Value.Proximity);
                                }

                                int rowIndex = 0;
                                foreach (var player in orderedPlayerNameContexts)
                                {
                                    DrawQuickAddRow(player.Key, rowIndex);
                                    ++rowIndex;
                                }
                            }

                            ImGui.EndTable();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_TaggedPlayers))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();
                        if (ImGui.BeginTable("##TaggedPlayersTable", 1 + m_PluginData.CustomTags.Count))
                        {
                            ImGui.TableHeader(Strings.Loc_Static_PlayerName);
                            ImGui.TableSetupColumn(Strings.Loc_Static_PlayerName);
                            ImGui.NextColumn();
                            foreach (var customTag in m_PluginData.CustomTags)
                            {
                                ImGui.TableHeader(customTag.Text.InheritedValue);
                                ImGui.TableSetupColumn(customTag.Text.InheritedValue);
                                ImGui.NextColumn();
                            }
                            ImGui.TableHeadersRow();

                            int rowIndex = 0;
                            foreach (var identity in m_PluginData.Identities.ToArray())
                            {
                                DrawQuickAddRow(identity, rowIndex);
                                ++rowIndex;
                            }

                            if (PluginServices.ObjectTable.Length == 0 && PluginServices.ClientState.LocalPlayer != null)
                            {
                                DrawQuickAddRow(m_PluginData.GetIdentity(PluginServices.ClientState.LocalPlayer), 0);
                            }

                            ImGui.EndTable();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Strings.Loc_Static_StatusIconPrioList))
                    {
                        ImGui.Spacing();
                        ImGui.Spacing();

                        var isPriorizerEnabled = m_PluginConfiguration.StatusIconPriorizerSettings.UsePriorizedIcons;
                        DrawCheckbox(nameof(StatusIconPriorizerSettings.UsePriorizedIcons), true, ref isPriorizerEnabled, () =>
                        {
                            m_PluginConfiguration.StatusIconPriorizerSettings.UsePriorizedIcons = isPriorizerEnabled;
                            SaveSettings();
                        });

                        DrawCheckbox(nameof(PluginConfiguration.MoveStatusIconToNameplateTextIfPossible), true, ref m_PluginConfiguration.MoveStatusIconToNameplateTextIfPossible, () => SaveSettings());

                        if (isPriorizerEnabled)
                        {
                            var statusIcons = Enum.GetValues<StatusIcons>();

                            ImGui.Spacing();
                            ImGui.Spacing();

                            if (ImGui.Button(Strings.Loc_StatusIconPriorizer_ResetToDefault))
                            {
                                m_PluginConfiguration.StatusIconPriorizerSettings.ResetToDefault();
                                SaveSettings();
                            }
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip(Strings.Loc_StatusIconPriorizer_ResetToDefault_Description);
                            
                            ImGui.SameLine();

                            if (ImGui.Button(Strings.Loc_StatusIconPriorizer_ResetToEmpty))
                            {
                                m_PluginConfiguration.StatusIconPriorizerSettings.ResetToEmpty();
                                SaveSettings();
                            }
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip(Strings.Loc_StatusIconPriorizer_ResetToEmpty_Description);

                            ImGui.Spacing();
                            ImGui.Spacing();

                            foreach (var conditionSetName in Enum.GetValues<StatusIconPriorizerConditionSets>())
                            {
                                if (ImGui.CollapsingHeader(Localizer.GetString(conditionSetName, false)))
                                {
                                    var conditionSet = m_PluginConfiguration.StatusIconPriorizerSettings.GetConditionSet(conditionSetName);

                                    foreach (var statusIcon in statusIcons)
                                    {
                                        var isChecked = conditionSet.Contains(statusIcon);
                                        DrawCheckbox(Localizer.GetName(statusIcon), true, ref isChecked, () =>
                                        {
                                            if (isChecked)
                                            {
                                                if (!conditionSet.Contains(statusIcon))
                                                    conditionSet.Add(statusIcon);
                                            }
                                            else if (conditionSet.Contains(statusIcon))
                                                conditionSet.Remove(statusIcon);
                                            SaveSettings();
                                        });
                                    }
                                }

                                if (ImGui.IsItemHovered())
                                    ImGui.SetTooltip(Localizer.GetString(conditionSetName, true));

                                ImGui.Spacing();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }

            if (!m_PluginConfiguration.IsVisible)
            {
                SaveSettings();
            }
        }

        void DrawQuickAddRow(Identity identity, int rowIndex)
        {
            ImGui.PushID(identity.ToString());

            ImGui.TableNextRow();

            if (rowIndex % 2 != 0)
            {
                var backgroundColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.05f));
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, backgroundColor);
            }

            ImGui.TableNextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 0));
            ImGui.Text(identity.Name);
            if (identity.WorldId != null)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1, 1, 1, 0.25f), $"@{identity.WorldName}");
            }
            ImGui.PopStyleVar();

            foreach (Tag customTag in m_PluginData.CustomTags)
            {
                ImGui.PushID(customTag.GetHashCode().ToString());

                ImGui.TableNextColumn();

                bool isTagAssigned = identity.CustomTagIds.Contains(customTag.CustomId.Value);

                DrawSimpleCheckbox(string.Format(Strings.Loc_Static_Format_AddTagToPlayer, customTag.Text.InheritedValue, identity.Name), ref isTagAssigned, () =>
                {
                    if (isTagAssigned)
                    {
                        m_PluginData.AddCustomTagToIdentity(customTag, identity);
                        SaveSettings();
                    }
                    else
                    {
                        m_PluginData.RemoveCustomTagFromIdentity(customTag, identity);
                        SaveSettings();
                    }
                });

                ImGui.PopID();
            }

            ImGui.PopID();
        }

        public string GetTreeItemName(Tag tag)
        {
            string itemName = tag.Name.Value;
            if (m_PluginData.CustomTags.Contains(tag))
            {
                if (!string.IsNullOrWhiteSpace(tag.Text.InheritedValue))
                {
                    itemName = tag.Text.InheritedValue;
                }
                else
                {
                    itemName = Strings.Loc_Static_NoText;
                }
            }

            return itemName;
        }

        public void Select(Tag tag)
        {
            foreach (var descendent in m_PluginData.AllTags.Descendents)
            {
                descendent.IsSelected.Value = false;
            }

            tag.IsSelected.Value = true;
            SaveSettings();
        }

        public void DrawTree(Tag tag)
        {
            ImGui.PushID(tag.GetHashCode().ToString());

            // Build the tree node flags
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.AllowItemOverlap | ImGuiTreeNodeFlags.FramePadding;

            if (!tag.Children.Any())
            {
                flags |= ImGuiTreeNodeFlags.Leaf;
            }

            if (tag.IsSelected.Value)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            if (tag.IsExpanded.Value)
            {
                flags |= ImGuiTreeNodeFlags.DefaultOpen;
            }

            // Render the tree node
            var beforeItemPos = ImGui.GetCursorScreenPos();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 3));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            if (tag.TextColor.InheritedValue != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UIColorHelper.ToColor(tag.TextColor.InheritedValue.Value));
            }
            bool isOpened = ImGui.TreeNodeEx($"{GetTreeItemName(tag)}###{tag.GetHashCode()}", flags);
            if (tag.TextColor.InheritedValue != null)
            {
                ImGui.PopStyleColor();
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            var afterItemPos = ImGui.GetCursorScreenPos();

            // Don't allow expanding the item to select the item
            // Don't allow clicking on add/remove buttons to select the item
            var hard23 = ScalePoints(23);
            var hard2 = ScalePoints(2);
            var deadzoneTopLeft = new Vector2(beforeItemPos.X + ImGui.GetContentRegionAvail().X - hard23, beforeItemPos.Y - hard2);
            var deadzoneBottomRight = new Vector2(beforeItemPos.X + ImGui.GetContentRegionAvail().X + hard2, afterItemPos.Y + hard2);
            if (!ImGui.IsItemToggledOpen() && !ImGui.IsMouseHoveringRect(deadzoneTopLeft, deadzoneBottomRight) && ImGui.IsItemClicked())
            {
                Select(tag);
            }

            // Render the custom tag button
            if (tag == m_PluginData.AllCustomTags)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.3f, 0.1f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.2f, 0.6f, 0.2f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.6f, 0.2f, 1));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SetCursorPosX(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X - hard23);
                if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()))
                {
                    var newTag = new Tag(new LocalizedPluginString(nameof(PluginData.CustomTags)))
                    {
                        IsExpanded = true,
                        Text = Strings.Loc_Static_NewTag,
                        CustomId = Guid.NewGuid()
                    };

                    m_PluginData.CustomTags.Add(newTag);
                    newTag.Parent = m_PluginData.AllCustomTags;

                    Select(newTag);
                }
                ImGui.PopFont();
                ImGui.PopStyleVar();
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.Loc_Static_AddCustomTag_Description);
                }
            }


            // Render the remove custom tag button
            if (m_PluginData.CustomTags.Contains(tag))
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.1f, 0.1f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.2f, 0.2f, 1));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.2f, 1));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SetCursorPosX(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X - hard23);
                if (ImGui.Button(FontAwesomeIcon.TrashAlt.ToIconString()))
                {
                    m_PluginData.RemoveCustomTagFromIdentities(tag);
                    m_PluginData.AllCustomTags.Children.Remove(tag);
                    m_PluginData.CustomTags.Remove(tag);
                    SaveSettings();

                    Select(m_PluginData.AllCustomTags);
                }
                ImGui.PopFont();
                ImGui.PopStyleVar();
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.Loc_Static_RemoveCustomTag_Description);
                }
            }
                        

            // Save the node expansion state
            if (isOpened && tag.Children.Any() && !tag.IsExpanded.Value)
            {
                tag.IsExpanded.Value = true;
                SaveSettings();
            }
            else if (!isOpened && tag.IsExpanded.Value)
            {
                tag.IsExpanded.Value = false;
                SaveSettings();
            }



            // Render the child nodes
            if (isOpened)
            {
                ImGui.TreePush();
                foreach (var childTag in tag.Children.OrderBy(child => GetTreeItemName(child)).ToArray())
                {
                    DrawTree(childTag);
                }
                ImGui.TreePop();
            }


            ImGui.PopID();
        }

        public void DrawControls(Tag tag)
        {
            var hard23 = ScalePoints(23);
            ImGui.PushID(tag.GetHashCode().ToString());

            // Render the add property override button and popup
            if (ImGui.IsPopupOpen("AddPopup"))
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1));
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.3f, 0.1f, 1));
            }
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.2f, 0.6f, 0.2f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.6f, 0.2f, 1));

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(hard23, hard23)))
            {
                ImGui.OpenPopup("AddPopup");
            }
            ImGui.PopFont();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            bool wasPaddingConsumed = false;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ImGuiHelpers.ScaledVector2(0, 5));
            ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos() - ImGuiHelpers.ScaledVector2(0, 4));
            if (ImGui.BeginPopup("AddPopup"))
            {
                wasPaddingConsumed = true;
                ImGui.PopStyleVar();

                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0));
                if (ImGui.BeginListBox("###SelectableInheritables"))
                {
                    IEnumerable<KeyValuePair<string, IInheritable>> inheritables1 = tag.Inheritables.Where(inheritable => inheritable.Value.Behavior == InheritableBehavior.Inherit);
                    var selectedInheritables1 = inheritables1
                        .Select(inheritable =>
                        {
                            string? categoryId = null;

                            var field = tag.GetType().GetField(inheritable.Key);
                            if (field != null)
                            {
                                var inheritableCategory = field.GetCustomAttributes(typeof(InheritableCategoryAttribute), false).Cast<InheritableCategoryAttribute>().FirstOrDefault();
                                if (inheritableCategory != null)
                                {
                                    categoryId = inheritableCategory.CategoryId;
                                }
                            }

                            return new
                            {
                                Inheritable = inheritable,
                                CategoryId = categoryId,
                                LocalizedName = Localizer.GetString(inheritable.Key, false)
                            };
                        }).Where(inheritable => inheritable.CategoryId != null);

                    var inheritableGroups1 = selectedInheritables1.GroupBy(inheritable => inheritable.CategoryId);

                    foreach (var inheritableGroup in inheritableGroups1)
                    {
                        if (inheritableGroup.Key != null)
                        {
                            ImGui.PushID(inheritableGroup.Key);
                            DrawHeading(Localizer.GetString(inheritableGroup.Key, false));
                        }

                        foreach (var selectedInheritable in inheritableGroup)
                        {
                            bool isSelected = false;
                            if (ImGui.Selectable(selectedInheritable.LocalizedName, isSelected))
                            {
                                selectedInheritable.Inheritable.Value.Behavior = InheritableBehavior.Enabled;

                                if (selectedInheritable.Inheritable.Value is InheritableValue<bool> inheritableBool)
                                {
                                    inheritableBool.Value = true;
                                }

                                SaveSettings();
                                ImGui.CloseCurrentPopup();
                            }

                            if (isSelected)
                            {
                                ImGui.SetItemDefaultFocus();
                            }

                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip(Localizer.GetString(selectedInheritable.Inheritable.Key, true));
                            }
                        }

                        if (inheritableGroup.Key != null)
                        {
                            ImGui.PopID();
                        }
                    }

                    ImGui.EndListBox();
                }
                ImGui.PopStyleColor();
                ImGui.EndPopup();
            }
            if (!wasPaddingConsumed)
            {
                ImGui.PopStyleVar();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Strings.Loc_Static_AddPropertyOverride_Description);
            }

            if (tag.HasDefaults)
            {
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SetCursorPosX(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X - hard23);
                if (ImGui.Button(FontAwesomeIcon.Recycle.ToIconString()))
                {
                    tag.SetDefaults();
                }
                ImGui.PopFont();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.Loc_Static_ResetDefault_Description);
                }
            }

            // Render all the property overrides, and optionally allow the inherited properties to be rendered
            IEnumerable<KeyValuePair<string, IInheritable>> inheritables = tag.Inheritables;
            if (!m_PluginConfiguration.IsShowInheritedPropertiesEnabled)
            {
                inheritables = inheritables.Where(inheritable => inheritable.Value.Behavior != InheritableBehavior.Inherit);
            }

            var selectedInheritables = inheritables
                .Select(inheritable =>
                {
                    string? categoryId = null;

                    var field = tag.GetType().GetField(inheritable.Key);
                    if (field != null)
                    {
                        var inheritableCategory = field.GetCustomAttributes(typeof(InheritableCategoryAttribute), false).Cast<InheritableCategoryAttribute>().FirstOrDefault();
                        if (inheritableCategory != null)
                        {
                            categoryId = inheritableCategory.CategoryId;
                        }
                    }

                    return new
                    {
                        Inheritable = inheritable,
                        CategoryId = categoryId,
                        LocalizedName = Localizer.GetString(inheritable.Key, false)
                    };
                }).Where(inheritable => inheritable.CategoryId != null);

            var inheritableGroups = selectedInheritables.GroupBy(inheritable => inheritable.CategoryId);

            foreach (var inheritableGroup in inheritableGroups)
            {
                ImGui.Spacing();
                ImGui.Spacing();
                if (inheritableGroup.Key != null)
                {
                    DrawHeading(Localizer.GetString(inheritableGroup.Key, false));
                }

                foreach (var selectedInheritable in inheritableGroup)
                {
                    if (selectedInheritable.Inheritable.Value is InheritableValue<bool> inheritableBool)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, inheritableBool);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<ushort> inheritableUshort)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, inheritableUshort);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<BitmapFontIcon> inheritableBitmapFontIcon)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, false, true, inheritableBitmapFontIcon);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<TagPosition> inheritableTagPosition)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, true, false, inheritableTagPosition);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<NameplateElement> inheritableNameplateElement)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, true, false, inheritableNameplateElement);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<NameplateFreeCompanyVisibility> inheritableFreeCompanyVisibility)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, true, false, inheritableFreeCompanyVisibility);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<NameplateTitleVisibility> inheritableNameplateTitleVisibility)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, true, false, inheritableNameplateTitleVisibility);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<NameplateTitlePosition> inheritableNameplateTitlePosition)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, true, false, inheritableNameplateTitlePosition);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableValue<JobIconSetName> inheritableJobIconSetName)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, false, false, inheritableJobIconSetName);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableReference<List<XivChatType>> inheritableXivChatType)
                    {
                        DrawMultiselect(selectedInheritable.Inheritable.Key, inheritableXivChatType);
                    }
                    else if (selectedInheritable.Inheritable.Value is InheritableReference<string> inheritableString)
                    {
                        DrawInheritable(selectedInheritable.Inheritable.Key, inheritableString);
                    }
                    else
                    {
                        PluginServices.PluginLog.Warning($"Rendering for inheritable option not implemented: {selectedInheritable.Inheritable.Key}");
                    }
                }
            }


            ImGui.PopID();
        }

        private void DrawRemovePropertyOverrideButton(IInheritable inheritable)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.1f, 0.1f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.2f, 0.2f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.2f, 1));
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.TrashAlt.ToIconString(), ImGuiHelpers.ScaledVector2(23)))
            {
                inheritable.Behavior = InheritableBehavior.Inherit;
                SaveSettings();
            }
            ImGui.PopFont();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Strings.Loc_Static_RemovePropertyOverride_Description);
            }
        }

        private void DrawInheritable(string localizedStringName, InheritableValue<bool> inheritable)
        {
            bool isDisabled = inheritable.Behavior == InheritableBehavior.Inherit;
            if (isDisabled)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
            }

            ImGui.BeginGroup();
            ImGui.BeginChild(inheritable.GetHashCode().ToString(), ImGuiHelpers.ScaledVector2(0, 50));

            ImGui.Text(Localizer.GetString(localizedStringName, false));
            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
                ImGui.PopStyleVar();
            }

            if (isDisabled)
            {
                ImGui.SameLine();
                ImGui.Text(Strings.Loc_Static_Inherited);
            }

            if (isDisabled)
            {
                bool value = inheritable.InheritedValue != null ? inheritable.InheritedValue.Value : false;
                DrawCheckbox("IsEnabled", false, ref value, () =>
                {
                });
            }
            else
            {
                DrawCheckbox("IsEnabled", false, ref inheritable.Value, () =>
                {
                    SaveSettings();
                });

                ImGui.SameLine();
                DrawRemovePropertyOverrideButton(inheritable);
            }

            ImGui.EndChild();
            ImGui.EndGroup();

            if (isDisabled)
            {
                ImGui.PopStyleVar();
            }
        }

        private void DrawInheritable<TEnum>(string localizedStringName, bool shouldLocalizeNames, bool shouldOrderNames, InheritableValue<TEnum> inheritable)
            where TEnum : struct, Enum
        {
            bool isDisabled = inheritable.Behavior == InheritableBehavior.Inherit;
            if (isDisabled)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
            }

            ImGui.BeginGroup();
            ImGui.BeginChild(inheritable.GetHashCode().ToString(), ImGuiHelpers.ScaledVector2(0, 50));

            ImGui.Text(Localizer.GetString(localizedStringName, false));
            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
                ImGui.PopStyleVar();
            }

            if (isDisabled)
            {
                ImGui.SameLine();
                ImGui.Text(Strings.Loc_Static_Inherited);
            }

            if (isDisabled)
            {
                bool isEnabled = inheritable.InheritedValue != null;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () => { });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ScalePoints(200));
                    ImGui.BeginChild(inheritable.GetHashCode().ToString(), ImGuiHelpers.ScaledVector2(200, 0));
                    TEnum value = inheritable.InheritedValue != null ? inheritable.InheritedValue.Value : default(TEnum);
                    DrawComboBox(false, shouldLocalizeNames, shouldOrderNames, ref value, () => { });
                    ImGui.EndChild();
                }
            }
            else
            {
                bool isEnabled = inheritable.Behavior == InheritableBehavior.Enabled;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () =>
                {
                    if (isEnabled)
                    {
                        inheritable.Behavior = InheritableBehavior.Enabled;
                    }
                    else
                    {
                        inheritable.Behavior = InheritableBehavior.Disabled;
                    }
                    SaveSettings();
                });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ScalePoints(200));
                    ImGui.BeginChild(inheritable.GetHashCode().ToString(), ImGuiHelpers.ScaledVector2(200, 0));
                    DrawComboBox(false, shouldLocalizeNames, shouldOrderNames, ref inheritable.Value, () => { SaveSettings(); });
                    ImGui.EndChild();
                }

                ImGui.SameLine();
                DrawRemovePropertyOverrideButton(inheritable);
            }

            ImGui.EndChild();
            ImGui.EndGroup();

            if (isDisabled)
            {
                ImGui.PopStyleVar();
            }
        }

        private void DrawInheritable(string localizedStringName, InheritableValue<ushort> inheritable)
        {
            bool isDisabled = inheritable.Behavior == InheritableBehavior.Inherit;
            if (isDisabled)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
            }

            ImGui.BeginGroup();
            ImGui.BeginChild(inheritable.GetHashCode().ToString(), ImGuiHelpers.ScaledVector2(180, 50));

            ImGui.Text(Localizer.GetString(localizedStringName, false));
            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
                ImGui.PopStyleVar();
            }

            if (isDisabled)
            {
                ImGui.SameLine();
                ImGui.Text(Strings.Loc_Static_Inherited);
            }

            if (isDisabled)
            {
                bool isEnabled = inheritable.InheritedValue != null;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () => { });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ushort value = inheritable.InheritedValue != null ? inheritable.InheritedValue.Value : default(ushort);
                    DrawColorButton(value.ToString(), UIColorHelper.ToColor(value), () => { });
                }
            }
            else
            {
                bool isEnabled = inheritable.Behavior == InheritableBehavior.Enabled;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () =>
                {
                    if (isEnabled)
                    {
                        inheritable.Behavior = InheritableBehavior.Enabled;
                    }
                    else
                    {
                        inheritable.Behavior = InheritableBehavior.Disabled;
                    }
                    SaveSettings();
                });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    DrawColorButton(
                        inheritable.Value.ToString(),
                        UIColorHelper.ToColor(inheritable.Value),
                        () =>
                        {
                            m_ColorPickerPopupDataContext = inheritable;
                            ImGui.OpenPopup("ColorPickerPopup");
                        });
                }

                bool wasStyleConsumed = false;
                ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos() + ImGuiHelpers.ScaledVector2(31, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                if (ImGui.BeginPopup("ColorPickerPopup"))
                {
                    wasStyleConsumed = true;
                    ImGui.PopStyleVar();

                    DrawUIColorPicker(
                        (UIColor value) =>
                        {
                            if (m_ColorPickerPopupDataContext != null)
                            {
                                m_ColorPickerPopupDataContext.Value = (ushort)value.RowId;
                                m_ColorPickerPopupDataContext = null;
                                SaveSettings();
                            }

                            ImGui.CloseCurrentPopup();
                        });

                    ImGui.EndPopup();
                }
                if (!wasStyleConsumed)
                {
                    ImGui.PopStyleVar();
                }

                ImGui.SameLine();
                DrawRemovePropertyOverrideButton(inheritable);
            }

            ImGui.EndChild();
            ImGui.EndGroup();

            if (isDisabled)
            {
                ImGui.PopStyleVar();
            }
        }

        private void DrawInheritable(string localizedStringName, InheritableReference<string> inheritable)
        {
            bool isDisabled = inheritable.Behavior == InheritableBehavior.Inherit;
            if (isDisabled)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);
            }

            ImGui.BeginGroup();
            ImGui.BeginChild(inheritable.GetHashCode().ToString(), ImGuiHelpers.ScaledVector2(0, 50));

            ImGui.Text(Localizer.GetString(localizedStringName, false));
            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
                ImGui.PopStyleVar();
            }

            if (isDisabled)
            {
                ImGui.SameLine();
                ImGui.Text(Strings.Loc_Static_Inherited);
            }

            if (isDisabled)
            {
                bool isEnabled = inheritable.InheritedValue != null;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () =>
                {
                });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ScalePoints(200));
                    ImGui.BeginChild(inheritable.GetHashCode().ToString(), ImGuiHelpers.ScaledVector2(200, 0));
                    string value = inheritable.Value;
                    DrawTextBox(localizedStringName, ref value, () => { });
                    ImGui.EndChild();
                }
            }
            else
            {
                bool isEnabled = inheritable.Behavior == InheritableBehavior.Enabled;
                DrawCheckbox("IsEnabled", false, ref isEnabled, () =>
                {
                    if (isEnabled)
                    {
                        inheritable.Behavior = InheritableBehavior.Enabled;
                    }
                    else
                    {
                        inheritable.Behavior = InheritableBehavior.Disabled;
                    }
                    SaveSettings();
                });

                if (isEnabled)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ScalePoints(200));
                    ImGui.BeginChild(inheritable.GetHashCode().ToString(), ImGuiHelpers.ScaledVector2(200, 0));
                    DrawTextBox(localizedStringName, ref inheritable.Value, () => { SaveSettings(); });                
                    ImGui.EndChild();
                }

                ImGui.SameLine();
                DrawRemovePropertyOverrideButton(inheritable);
            }

            ImGui.EndChild();
            ImGui.EndGroup();

            if (isDisabled)
            {
                ImGui.PopStyleVar();
            }
        }

        private void DrawHeading(string label)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.6f, 1f, 1f), label);
        }

        private void DrawMultiselect<TEnum>(string localizedStringName, InheritableReference<List<TEnum>> inheritable) where TEnum : Enum
        {
            bool isDisabled = inheritable.Behavior == InheritableBehavior.Inherit;
            List<TEnum> proxyKey = isDisabled ? inheritable.InheritedValue : inheritable.Value;

            if (isDisabled)
                proxyKey = inheritable.InheritedValue;
            if (proxyKey == null)
                proxyKey = inheritable.Value;
            
            if (isDisabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.25f);

            var isExpanded = ImGui.CollapsingHeader(Localizer.GetString(localizedStringName, false));
            
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
            
            if (isDisabled)
            {
                ImGui.SameLine();
                ImGui.Text(Strings.Loc_Static_Inherited);
            }
            else
                DrawRemovePropertyOverrideButton(inheritable);

            if (isExpanded)
            {
                bool isClicked = false;
                var typeofEnum = typeof(TEnum);
                EnumMultiselectProxy<TEnum> proxy;

                if (inheritableTEnumProxies.ContainsKey(proxyKey))
                    proxy = inheritableTEnumProxies[proxyKey] as EnumMultiselectProxy<TEnum>;
                else
                {
                    proxy = new EnumMultiselectProxy<TEnum>(proxyKey);
                    inheritableTEnumProxies.Add(proxyKey, proxy);
                }

                foreach (var entry in proxy.Entries)
                {
                    var entryName = Enum.GetName(typeofEnum, entry.Value);
                    var tempval = entry.Enabled;

                    isClicked = ImGui.Checkbox(Localizer.GetString(entryName, false), ref isDisabled ? ref tempval : ref entry.Enabled);

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(Localizer.GetString(entryName, true));

                    if (isClicked && !isDisabled)
                    {
                        var newList = proxyKey.ToList();
                        proxy.ApplyTo(newList);
                        inheritable.Value = newList;
                        SaveSettings();
                    }
                }
            }

            if (isDisabled)
                ImGui.PopStyleVar();
        }

        private void DrawComboBox<TEnum>(bool isLabelVisible, bool shouldLocalizeNames, bool shouldOrderNames, ref TEnum currentValue, System.Action changed, bool showToolTipToLabel = false, bool showLabelInSameLine = false)
            where TEnum : Enum
        {
            if (isLabelVisible)
            {
                ImGui.Text(Localizer.GetString<TEnum>(false));
                if (showLabelInSameLine)
                    ImGui.SameLine();
            }

            var currentDisplayName = shouldLocalizeNames ? Localizer.GetString(currentValue, false) : currentValue.ToString();

            ImGui.SetNextItemWidth(ImGui.CalcItemWidth());
            if (ImGui.BeginCombo($"###{currentValue.GetType().Name}", currentDisplayName))
            {
                var displayNames = Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                    .Select(value => new { Value = value, DisplayName = shouldLocalizeNames ? Localizer.GetString(value, false) : value.ToString() });

                if (shouldOrderNames)
                {
                    displayNames = displayNames.OrderBy(displayEnum => displayEnum.DisplayName);
                }

                foreach (var orderedDisplayName in displayNames)
                {
                    bool isSelected = orderedDisplayName.Value.Equals(currentValue);
                    if (ImGui.Selectable(orderedDisplayName.DisplayName, isSelected))
                    {
                        currentValue = orderedDisplayName.Value;
                        changed();
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }

                    if (ImGui.IsItemHovered() && shouldLocalizeNames)
                    {
                        ImGui.SetTooltip(Localizer.GetString(orderedDisplayName.Value, true));
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.IsItemHovered() && shouldLocalizeNames)
            {
                if (showToolTipToLabel)
                    ImGui.SetTooltip(Localizer.GetString(typeof(TEnum).Name, true));
                else
                    ImGui.SetTooltip(Localizer.GetString(currentValue, true));
            }
        }

        private void DrawCheckbox(string localizedStringName, bool hasLabel, ref bool isChecked, System.Action changed)
        {
            if (ImGui.Checkbox(hasLabel ? Localizer.GetString(localizedStringName, false) : $"###{Localizer.GetString(localizedStringName, false)}", ref isChecked))
            {
                changed();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
            }
        }

        private void DrawSimpleCheckbox(string localizedString, ref bool isChecked, System.Action changed)
        {
            if (ImGui.Checkbox($"###{localizedString}", ref isChecked))
            {
                changed();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(localizedString);
            }
        }

        private void DrawColorButton(string colorId, Vector4 color, System.Action clicked)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
            if (ImGui.Button($"###{colorId}", ImGuiHelpers.ScaledVector2(23)))
            {
                clicked();
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(colorId);
            }
        }

        private void DrawTextBox(string localizedStringName, ref string text, System.Action changed)
        {
            ImGui.SetNextItemWidth(ImGui.CalcItemWidth());

            var oldText = text;
            ImGui.InputText($"###{localizedStringName}", ref text, 1024);
            if (text != oldText)
            {
                changed();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Localizer.GetString(localizedStringName, true));
            }
        }

        private void DrawUIColorPicker(System.Action<UIColor> colorSelected)
        {
            const int columnCount = 12;

            int currentColumn = 0;
            foreach (var uiColor in UIColorHelper.UIColors)
            {
                if (currentColumn % columnCount != 0)
                {
                    ImGui.SameLine(0, 0);
                }

                DrawColorButton(uiColor.RowId.ToString(), UIColorHelper.ToColor(uiColor), () => { colorSelected(uiColor); });
                currentColumn++;
            }
        }

        private void SaveSettings(bool saveProxy = false)
        {
            if (saveProxy)
                propertyProxy.SaveData();
            m_PluginConfiguration.Save(m_PluginData);
        }

        private class PropertyProxy
        {
            private PluginConfiguration pluginConfig;

            public ActivityContextSelection CurrentActivityContext;
            public NameplateFreeCompanyVisibility NameplateFreeCompanyVisibility;
            public NameplateTitleVisibility NameplateTitleVisibility;
            public NameplateTitlePosition NameplateTitlePosition;
            public DeadPlayerHandling NameplateDeadPlayerHandling;
            public bool IsApplyTagsToAllChatMessagesEnabled;

            public PropertyProxy(PluginConfiguration config)
            {
                pluginConfig = config;
                CurrentActivityContext = config.IsGeneralOptionsAllTheSameEnabled ? ActivityContextSelection.All : ActivityContextSelection.None;
            }

            public void LoadData()
            {
                var currentActivityContext = GetActivityContext(CurrentActivityContext);
                NameplateFreeCompanyVisibility = pluginConfig.GeneralOptions[currentActivityContext].NameplateFreeCompanyVisibility;
                NameplateTitleVisibility = pluginConfig.GeneralOptions[currentActivityContext].NameplateTitleVisibility;
                NameplateTitlePosition = pluginConfig.GeneralOptions[currentActivityContext].NameplateTitlePosition;
                NameplateDeadPlayerHandling = pluginConfig.GeneralOptions[currentActivityContext].NameplateDeadPlayerHandling;
                IsApplyTagsToAllChatMessagesEnabled = pluginConfig.GeneralOptions[currentActivityContext].IsApplyTagsToAllChatMessagesEnabled;
            }

            public void SaveData()
            {
                if (CurrentActivityContext == ActivityContextSelection.All)
                {
                    pluginConfig.IsGeneralOptionsAllTheSameEnabled = true;
                    foreach (var key in pluginConfig.GeneralOptions.Keys)
                        applyChanges(key);
                }
                else
                {
                    pluginConfig.IsGeneralOptionsAllTheSameEnabled = false;
                    applyChanges(GetActivityContext(CurrentActivityContext));
                }

                void applyChanges(ActivityType key)
                {
                    pluginConfig.GeneralOptions[key].NameplateFreeCompanyVisibility = NameplateFreeCompanyVisibility;
                    pluginConfig.GeneralOptions[key].NameplateTitleVisibility = NameplateTitleVisibility;
                    pluginConfig.GeneralOptions[key].NameplateTitlePosition = NameplateTitlePosition;
                    pluginConfig.GeneralOptions[key].NameplateDeadPlayerHandling = NameplateDeadPlayerHandling;
                    pluginConfig.GeneralOptions[key].IsApplyTagsToAllChatMessagesEnabled = IsApplyTagsToAllChatMessagesEnabled;
                }
            }

            private ActivityType GetActivityContext(ActivityContextSelection selection)
            {
                ActivityType result;

                switch (selection)
                {
                    case ActivityContextSelection.PveDuty:
                        result = ActivityType.PveDuty;
                        break;
                    case ActivityContextSelection.PvpDuty:
                        result = ActivityType.PvpDuty;
                        break;
                    case ActivityContextSelection.All:
                    case ActivityContextSelection.None:
                    default:
                        result = ActivityType.None;
                        break;
                }

                return result;
            }
        }

        private enum ActivityContextSelection
        {
            All,
            None,
            PveDuty,
            PvpDuty
        }

        private class EnumMultiselectProxy<TEnum> where TEnum : Enum
        {
            public List<Entry> Entries { get; } = new();

            public EnumMultiselectProxy(List<TEnum> target)
            {
                foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
                    Entries.Add(new(value, target.Contains(value)));
            }

            public void ApplyTo(List<TEnum> target)
            {
                foreach (var entry in Entries)
                {
                    if (entry.Enabled)
                    {
                        if (!target.Contains(entry.Value))
                            target.Add(entry.Value);
                    }
                    else if (target.Contains(entry.Value))
                        target.Remove(entry.Value);
                }
            }

            public class Entry
            {
                public TEnum Value { get; set; }
                public bool Enabled;

                public Entry(TEnum value, bool enabled)
                {
                    Value = value;
                    Enabled = enabled;
                }
            }
        }
    }
}
