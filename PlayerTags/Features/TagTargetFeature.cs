using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.GeneratedSheets;
using Pilz.Dalamud.ActivityContexts;
using Pilz.Dalamud.Tools.Strings;
using PlayerTags.Configuration.GameConfig;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

namespace PlayerTags.Features
{
    /// <summary>
    /// The base of a feature that adds tags to UI elements.
    /// </summary>
    public abstract class TagTargetFeature : FeatureBase
    {

        public TagTargetFeature()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        protected abstract bool IsIconVisible(Tag tag);

        protected abstract bool IsTextVisible(Tag tag);

        protected bool IsTagVisible(Tag tag, GameObject? gameObject)
        {
            bool isVisibleForActivity = ActivityContextHelper.GetIsVisible(ActivityContextManager.CurrentActivityContext.ActivityType,
                tag.IsVisibleInPveDuties.InheritedValue ?? false,
                tag.IsVisibleInPvpDuties.InheritedValue ?? false,
                tag.IsVisibleInOverworld.InheritedValue ?? false);

            if (!isVisibleForActivity)
            {
                return false;
            }

            if (gameObject is PlayerCharacter playerCharacter)
            {
                bool isVisibleForPlayer = PlayerContextHelper.GetIsVisible(playerCharacter,
                    tag.IsVisibleForSelf.InheritedValue ?? false,
                    tag.IsVisibleForFriendPlayers.InheritedValue ?? false,
                    tag.IsVisibleForPartyPlayers.InheritedValue ?? false,
                    tag.IsVisibleForAlliancePlayers.InheritedValue ?? false,
                    tag.IsVisibleForEnemyPlayers.InheritedValue ?? false,
                    tag.IsVisibleForOtherPlayers.InheritedValue ?? false);

                if (!isVisibleForPlayer)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the payloads for the given tag and game object depending on visibility conditions.
        /// </summary>
        /// <param name="gameObject">The game object to get payloads for.</param>
        /// <param name="tag">The tag config to get payloads for.</param>
        /// <returns>A list of payloads for the given tag.</returns>
        protected Payload[] GetPayloads(Tag tag, GameObject? gameObject)
        {
            if (!IsTagVisible(tag, gameObject))
            {
                return Array.Empty<Payload>();
            }

            return CreatePayloads(tag);
        }

        /// <summary>
        /// Creates payloads for the given tag.
        /// </summary>
        /// <param name="tag">The tag to create payloads for.</param>
        /// <returns>The payloads for the given tag.</returns>
        private Payload[] CreatePayloads(Tag tag)
        {
            List<Payload> newPayloads = new List<Payload>();

            BitmapFontIcon? icon = null;
            if (IsIconVisible(tag))
            {
                icon = tag.Icon.InheritedValue;
            }

            if (icon != null && icon.Value != BitmapFontIcon.None)
            {
                newPayloads.Add(new IconPayload(icon.Value));
            }

            string? text = null;
            if (IsTextVisible(tag))
            {
                text = tag.Text.InheritedValue;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                if (tag.IsTextItalic.InheritedValue != null && tag.IsTextItalic.InheritedValue.Value)
                {
                    newPayloads.Add(new EmphasisItalicPayload(true));
                }

                if (tag.TextGlowColor.InheritedValue != null)
                {
                    newPayloads.Add(new UIGlowPayload(tag.TextGlowColor.InheritedValue.Value));
                }

                if (tag.TextColor.InheritedValue != null)
                {
                    newPayloads.Add(new UIForegroundPayload(tag.TextColor.InheritedValue.Value));
                }

                newPayloads.Add(new TextPayload(text));

                if (tag.TextColor.InheritedValue != null)
                {
                    newPayloads.Add(new UIForegroundPayload(0));
                }

                if (tag.TextGlowColor.InheritedValue != null)
                {
                    newPayloads.Add(new UIGlowPayload(0));
                }

                if (tag.IsTextItalic.InheritedValue != null && tag.IsTextItalic.InheritedValue.Value)
                {
                    newPayloads.Add(new EmphasisItalicPayload(false));
                }
            }

            return newPayloads.ToArray();
        }

        protected static string BuildPlayername(string name)
        {
            var logNameType = GameConfigHelper.Instance.GetLogNameType();
            var result = string.Empty;

            if (logNameType != null && !string.IsNullOrEmpty(name))
            {
                var nameSplitted = name.Split(' ');

                if (nameSplitted.Length > 1)
                {
                    var firstName = nameSplitted[0];
                    var lastName = nameSplitted[1];

                    switch (logNameType)
                    {
                        case LogNameType.FullName:
                            result = $"{firstName} {lastName}";
                            break;
                        case LogNameType.LastNameShorted:
                            result = $"{firstName} {lastName[..1]}.";
                            break;
                        case LogNameType.FirstNameShorted:
                            result = $"{firstName[..1]}. {lastName}";
                            break;
                        case LogNameType.Initials:
                            result = $"{firstName[..1]}. {lastName[..1]}.";
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(result))
                result = name;

            return result;
        }

        /// <summary>
        /// Adds the given payload changes to the dictionary.
        /// </summary>
        /// <param name="tagPosition">The position to add changes to.</param>
        /// <param name="payloads">The payloads to add.</param>
        /// <param name="stringChanges">The dictionary to add the changes to.</param>
        protected void AddPayloadChanges(StringPosition tagPosition, IEnumerable<Payload> payloads, StringChanges stringChanges, bool forceUsingSingleAnchorPayload)
        {
            if (payloads != null && payloads.Any() && stringChanges != null)
            {
                var changes = stringChanges.GetChange(tagPosition);
                changes.Payloads.AddRange(payloads);
                changes.ForceUsingSingleAnchorPayload = forceUsingSingleAnchorPayload;
            }
        }

        /// <summary>
        /// Applies changes to the given string.
        /// </summary>
        /// <param name="seString">The string to apply changes to.</param>
        /// <param name="stringChanges">The changes to apply.</param>
        /// <param name="anchorPayload">The payload in the string that changes should be anchored to. If there is no anchor, the changes will be applied to the entire string.</param>
        protected void ApplyStringChanges(SeString seString, StringChanges stringChanges, List<Payload> anchorPayloads = null, Payload anchorReplacePayload = null)
        {
            var props = new StringChangesProps
            {
                Destination = seString,
                AnchorPayload = anchorReplacePayload
            };

            props.AnchorPayloads = anchorPayloads;
            props.StringChanges = stringChanges;

            StringUpdateFactory.ApplyStringChanges(props);
        }

        protected void ApplyTextFormatting(GameObject gameObject, Tag tag, SeString[] destStrings, InheritableValue<bool>[] textColorApplied, List<Payload> preferedPayloads, ushort? overwriteTextColor = null)
        {
            if (IsTagVisible(tag, gameObject))
            {
                for (int i = 0; i < destStrings.Length; i++)
                {
                    var destString = destStrings[i];
                    var isTextColorApplied = textColorApplied[i];
                    applyTextColor(destString, isTextColorApplied, tag.TextColor);
                    //applyTextGlowColor(destString, isTextColorApplied, tag.TextGlowColor);
                    //applyTextItalicColor(destString, tag.IsTextItalic); // Disabled, because that is needed only for a few parts somewhere else.
                }
            }

            void applyTextColor(SeString destPayload, InheritableValue<bool> enableFlag, InheritableValue<ushort> colorValue)
            {
                var colorToUse = overwriteTextColor ?? colorValue?.InheritedValue;
                if (shouldApplyFormattingPayloads(destPayload)
                            && enableFlag.InheritedValue != null
                            && enableFlag.InheritedValue.Value
                            && colorToUse != null)
                    applyTextFormattingPayloads(destPayload, new UIForegroundPayload(colorToUse.Value), new UIForegroundPayload(0));
            }

            //void applyTextGlowColor(SeString destPayload, InheritableValue<bool> enableFlag, InheritableValue<ushort> colorValue)
            //{
            //    if (shouldApplyFormattingPayloads(destPayload)
            //                && enableFlag.InheritedValue != null
            //                && enableFlag.InheritedValue.Value
            //                && colorValue.InheritedValue != null)
            //        applyTextFormattingPayloads(destPayload, new UIGlowPayload(colorValue.InheritedValue.Value), new UIGlowPayload(0));
            //}

            //void applyTextItalicColor(SeString destPayload, InheritableValue<bool> italicValue)
            //{
            //    if (shouldApplyFormattingPayloads(destPayload)
            //                && italicValue.InheritedValue != null
            //                && italicValue.InheritedValue.Value)
            //        applyTextFormattingPayloads(destPayload, new EmphasisItalicPayload(true), new EmphasisItalicPayload(false));
            //}

            bool shouldApplyFormattingPayloads(SeString destPayload)
                => destPayload.Payloads.Any(payload => payload is TextPayload || payload is PlayerPayload);

            void applyTextFormattingPayloads(SeString destPayload, Payload startPayload, Payload endPayload)
            {
                if (preferedPayloads == null || !preferedPayloads.Any())
                    applyTextFormattingPayloadToStartAndEnd(destPayload, startPayload, endPayload);
                else
                    applyTextFormattingPayloadsToSpecificPosition(destPayload, startPayload, endPayload, preferedPayloads);
            }
            
            void applyTextFormattingPayloadToStartAndEnd(SeString destPayload, Payload startPayload, Payload endPayload)
            {
                destPayload.Payloads.Insert(0, startPayload);
                destPayload.Payloads.Add(endPayload);
            }

            void applyTextFormattingPayloadsToSpecificPosition(SeString destPayload, Payload startPayload, Payload endPayload, List<Payload> preferedPayload)
            {
                int payloadStartIndex = destPayload.Payloads.IndexOf(preferedPayloads.First());
                destPayload.Payloads.Insert(payloadStartIndex, startPayload);

                int payloadEndIndex = destPayload.Payloads.IndexOf(preferedPayloads.Last());
                destPayload.Payloads.Insert(payloadEndIndex + 1, endPayload);
            }
        }
    }
}
