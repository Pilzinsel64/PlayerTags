using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Pilz.Dalamud.Icons;
using Pilz.Dalamud.Nameplates.Tools;
using Pilz.Dalamud.Tools.Strings;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.GameInterface.Nameplates;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Features
{
    /// <summary>
    /// A feature that adds tags to nameplates.
    /// </summary>
    public class NameplateTagTargetFeature : TagTargetFeature
    {
        private readonly StatusIconPriorizer statusiconPriorizer;
        private readonly JobIconSets jobIconSets = new();
        private Nameplate? m_Nameplate;

        public NameplateTagTargetFeature(PluginConfiguration pluginConfiguration, PluginData pluginData) : base(pluginConfiguration, pluginData)
        {
            statusiconPriorizer = new(pluginConfiguration.StatusIconPriorizerSettings);

            PluginServices.ClientState.Login += ClientState_Login;
            PluginServices.ClientState.Logout += ClientState_Logout;

            Hook();
        }

        public override void Dispose()
        {
            Unhook();
            
            PluginServices.ClientState.Logout -= ClientState_Logout;
            PluginServices.ClientState.Login -= ClientState_Login;
            
            base.Dispose();
        }

        private void Hook()
        {
            if (m_Nameplate == null)
            {
                m_Nameplate = new Nameplate();
                if (!m_Nameplate.IsValid)
                {
                    m_Nameplate = null;
                }

                if (m_Nameplate != null)
                {
                    m_Nameplate.PlayerNameplateUpdated += Nameplate_PlayerNameplateUpdated;
                }
            }
        }

        private void Unhook()
        {
            if (m_Nameplate != null)
            {
                m_Nameplate.PlayerNameplateUpdated -= Nameplate_PlayerNameplateUpdated;
                m_Nameplate.Dispose();
                m_Nameplate = null;
            }
        }

        private void ClientState_Login()
        {
            Hook();
        }

        private void ClientState_Logout()
        {
            Unhook();
        }

        protected override bool IsIconVisible(Tag tag)
        {
            if (tag.IsRoleIconVisibleInNameplates.InheritedValue != null)
            {
                return tag.IsRoleIconVisibleInNameplates.InheritedValue.Value;
            }

            return false;
        }

        protected override bool IsTextVisible(Tag tag)
        {
            if (tag.IsTextVisibleInNameplates.InheritedValue != null)
            {
                return tag.IsTextVisibleInNameplates.InheritedValue.Value;
            }

            return false;
        }

        private unsafe void Nameplate_PlayerNameplateUpdated(PlayerNameplateUpdatedArgs args)
        {
            if (!EnableGlobal) return;

            var beforeTitleBytes = args.Title.Encode();
            var iconID = args.IconId;
            var generalOptions = pluginConfiguration.GeneralOptions[ActivityContextManager.CurrentActivityContext.ActivityType];

            AddTagsToNameplate(args.PlayerCharacter, args.Name, args.Title, args.FreeCompany, ref iconID, generalOptions);

            args.IconId = iconID;

            if (generalOptions.NameplateTitlePosition == NameplateTitlePosition.AlwaysAboveName)
                args.IsTitleAboveName = true;
            else if (generalOptions.NameplateTitlePosition == NameplateTitlePosition.AlwaysBelowName)
                args.IsTitleAboveName = false;

            if (generalOptions.NameplateTitleVisibility == NameplateTitleVisibility.Always)
                args.IsTitleVisible = true;
            else if (generalOptions.NameplateTitleVisibility == NameplateTitleVisibility.Never)
                args.IsTitleVisible = false;
            else if (generalOptions.NameplateTitleVisibility == NameplateTitleVisibility.WhenHasTags)
            {
                bool hasTitleChanged = !beforeTitleBytes.SequenceEqual(args.Title.Encode());
                args.IsTitleVisible = hasTitleChanged;
            }

            if (generalOptions.NameplateFreeCompanyVisibility == NameplateFreeCompanyVisibility.Never)
                args.FreeCompany.Payloads.Clear();
        }

        /// <summary>
        /// Adds the given payload changes to the specified locations.
        /// </summary>
        /// <param name="nameplateElement">The nameplate element of the changes.</param>
        /// <param name="tagPosition">The position of the changes.</param>
        /// <param name="payloadChanges">The payload changes to add.</param>
        /// <param name="nameplateChanges">The dictionary to add changes to.</param>
        private void AddPayloadChanges(NameplateElement nameplateElement, TagPosition tagPosition, IEnumerable<Payload> payloadChanges, NameplateChanges nameplateChanges, bool forceUsingSingleAnchorPayload)
        {
            if (payloadChanges.Any())
            {
                var changes = nameplateChanges.GetChanges((NameplateElements)nameplateElement);
                AddPayloadChanges((StringPosition)tagPosition, payloadChanges, changes, forceUsingSingleAnchorPayload);
            }
        }

        private NameplateChanges GenerateEmptyNameplateChanges(SeString name, SeString title, SeString freeCompany)
        {
            NameplateChanges nameplateChanges = new();
            
            nameplateChanges.GetProps(NameplateElements.Name).Destination = name;
            nameplateChanges.GetProps(NameplateElements.Title).Destination = title;
            nameplateChanges.GetProps(NameplateElements.FreeCompany).Destination = freeCompany;

            return nameplateChanges;
        }

        /// <summary>
        /// Adds tags to the nameplate of a game object.
        /// </summary>
        /// <param name="gameObject">The game object context.</param>
        /// <param name="name">The name text to change.</param>
        /// <param name="title">The title text to change.</param>
        /// <param name="freeCompany">The free company text to change.</param>
        private void AddTagsToNameplate(GameObject gameObject, SeString name, SeString title, SeString freeCompany, ref int statusIcon, GeneralOptionsClass generalOptions)
        {
            var playerCharacter = gameObject as PlayerCharacter;
            int? newStatusIcon = null;
            NameplateChanges nameplateChanges = GenerateEmptyNameplateChanges(name, title, freeCompany);

            if (playerCharacter != null && (!playerCharacter.IsDead || generalOptions.NameplateDeadPlayerHandling != DeadPlayerHandling.Ignore))
            {
                var classJob = playerCharacter.ClassJob;
                var classJobGameData = classJob?.GameData;

                // Add the job tags
                if (classJobGameData != null && pluginData.JobTags.TryGetValue(classJobGameData.Abbreviation, out var jobTag))
                {
                    if (jobTag.TagTargetInNameplates.InheritedValue != null && jobTag.TagPositionInNameplates.InheritedValue != null)
                        checkTag(jobTag);
                }

                // Add the randomly generated name tag payload
                if (pluginConfiguration.IsPlayerNameRandomlyGenerated)
                {
                    var characterName = playerCharacter.Name.TextValue;
                    if (characterName != null)
                    {
                        var generatedName = RandomNameGenerator.Generate(characterName);
                        if (generatedName != null)
                            AddPayloadChanges(NameplateElement.Name, TagPosition.Replace, Enumerable.Empty<Payload>().Append(new TextPayload(generatedName)), nameplateChanges, false);
                    }
                }

                // Add custom tags
                Identity identity = pluginData.GetIdentity(playerCharacter);
                foreach (var customTagId in identity.CustomTagIds)
                {
                    var customTag = pluginData.CustomTags.FirstOrDefault(tag => tag.CustomId.Value == customTagId);
                    if (customTag != null)
                        checkTag(customTag);
                }

                void checkTag(Tag tag)
                {
                    if (tag.TagTargetInNameplates.InheritedValue != null && tag.TagPositionInNameplates.InheritedValue != null)
                    {
                        var payloads = GetPayloads(tag, gameObject);
                        if (payloads.Any())
                            AddPayloadChanges(tag.TagTargetInNameplates.InheritedValue.Value, tag.TagPositionInNameplates.InheritedValue.Value, payloads, nameplateChanges, false);
                    }
                    if (IsTagVisible(tag, gameObject) && newStatusIcon == null && classJob != null && (tag.IsJobIconVisibleInNameplates?.InheritedValue ?? false))
                        newStatusIcon = jobIconSets.GetJobIcon(tag.JobIconSet?.InheritedValue ?? JobIconSetName.Framed, classJob.Id);
                }
            }

            // Apply new status icon
            if (newStatusIcon != null)
            {
                var change = nameplateChanges.GetChange(NameplateElements.Name, StringPosition.Before);
                NameplateUpdateFactory.ApplyStatusIconWithPrio(ref statusIcon, (int)newStatusIcon, change, ActivityContextManager.CurrentActivityContext, statusiconPriorizer, pluginConfiguration.MoveStatusIconToNameplateTextIfPossible);
            }

            // Gray out the nameplate
            if (playerCharacter != null && playerCharacter.IsDead && generalOptions.NameplateDeadPlayerHandling == DeadPlayerHandling.GrayOut)
                GrayOutNameplate(gameObject, nameplateChanges);

            // Build the final strings out of the payloads
            ApplyNameplateChanges(nameplateChanges);

            if (playerCharacter != null && (!playerCharacter.IsDead || generalOptions.NameplateDeadPlayerHandling == DeadPlayerHandling.Include))
            {
                // An additional step to apply text color to additional locations
                Identity identity = pluginData.GetIdentity(playerCharacter);
                foreach (var customTagId in identity.CustomTagIds)
                {
                    var customTag = pluginData.CustomTags.FirstOrDefault(tag => tag.CustomId.Value == customTagId);
                    if (customTag != null)
                        applyTextFormatting(customTag);
                }

                if (playerCharacter.ClassJob.GameData != null && pluginData.JobTags.TryGetValue(playerCharacter.ClassJob.GameData.Abbreviation, out var jobTag))
                    applyTextFormatting(jobTag);

                void applyTextFormatting(Tag tag)
                {
                    var destStrings = new[] { name, title, freeCompany };
                    var isTextColorApplied = new[] { tag.IsTextColorAppliedToNameplateName, tag.IsTextColorAppliedToNameplateTitle, tag.IsTextColorAppliedToNameplateFreeCompany };
                    ApplyTextFormatting(gameObject, tag, new[] { name, title, freeCompany }, isTextColorApplied, null);
                }
            }
        }

        private void GrayOutNameplate(GameObject gameObject, NameplateChanges nameplateChanges)
        {
            if (gameObject is PlayerCharacter playerCharacter)
            {
                foreach (NameplateElements element in Enum.GetValues<NameplateElements>())
                {
                    nameplateChanges.GetChange(element, StringPosition.Before).Payloads.Add(new UIForegroundPayload(3));
                    nameplateChanges.GetChange(element, StringPosition.After).Payloads.Add(new UIForegroundPayload(0));
                }
            }
        }

        protected void ApplyNameplateChanges(NameplateChanges nameplateChanges)
        {
            var props = new NameplateChangesProps
            {
                Changes = nameplateChanges
            };
            NameplateUpdateFactory.ApplyNameplateChanges(props);
        }
    }
}
