using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using Pilz.Dalamud.Tools.Strings;
using PlayerTags.Configuration;
using PlayerTags.Configuration.GameConfig;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Action = System.Action;

namespace PlayerTags.Features
{
    /// <summary>
    /// A feature that adds tags to chat messages.
    /// </summary>
    public class ChatTagTargetFeature : TagTargetFeature
    {
        /// <summary>
        /// A match found within a string.
        /// </summary>
        private class StringMatch
        {
            /// <summary>
            /// The string that the match was found in.
            /// </summary>
            public SeString SeString { get; init; }

            public List<Payload> DisplayTextPayloads { get; init; } = new();

            /// <summary>
            /// The matching game object if one exists
            /// </summary>
            public GameObject? GameObject { get; init; }

            /// <summary>
            /// A matching player payload if one exists.
            /// </summary>
            public PlayerPayload? PlayerPayload { get; init; }

            public RawPayload LinkTerminatorPayload { get; init; }

            public Payload? PlayerNamePayload
            {
                get
                {
                    Payload textPayload = null;
                    string textMatch = GetMatchTextInternal();
                    string textMatchShort = BuildPlayername(textMatch);

                    textPayload = DisplayTextPayloads.FirstOrDefault(n => n is TextPayload textPayload && (textPayload.Text.Contains(textMatch) || ((!string.IsNullOrEmpty(textMatchShort)) && textPayload.Text.Contains(textMatchShort))));
                    textPayload ??= PlayerPayload;
                    textPayload ??= DisplayTextPayloads.FirstOrDefault();

                    return textPayload;
                }
            }

            public bool IsLocalPlayer
            {
                get
                {
                    return GetMatchTextInternal() == PluginServices.ClientState.LocalPlayer.Name.TextValue;
                }
            }

            public StringMatch(SeString seString)
            {
                SeString = seString;
            }

            private string GetMatchTextInternal()
            {
                if (GameObject != null)
                    return GameObject.Name.TextValue;
                else if (PlayerPayload != null)
                    return PlayerPayload.PlayerName;
                else
                    return SeString.TextValue;
            }

            /// <summary>
            /// Gets the matches text.
            /// </summary>
            /// <returns>The match text.</returns>
            public string GetMatchText()
            {
                var playerNamePayload = PlayerNamePayload;
                if (playerNamePayload is PlayerPayload pp)
                    return pp.PlayerName;
                else if (playerNamePayload is TextPayload tp)
                    return tp.Text;
                else
                    return SeString.TextValue;
            }
        }

        private PluginConfiguration m_PluginConfiguration;
        private PluginData m_PluginData;

        public ChatTagTargetFeature(PluginConfiguration pluginConfiguration, PluginData pluginData)
        {
            m_PluginConfiguration = pluginConfiguration;
            m_PluginData = pluginData;

            PluginServices.ChatGui.ChatMessage += Chat_ChatMessage;
        }

        public override void Dispose()
        {
            PluginServices.ChatGui.ChatMessage -= Chat_ChatMessage;
            base.Dispose();
        }

        private void Chat_ChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (m_PluginConfiguration.ZoneConfig[ActivityContextManager.CurrentActivityContext.ZoneType].IsApplyTagsToAllChatMessagesEnabled)
            {
                AddTagsToChat(sender, type, true);
                AddTagsToChat(message, type, false);
            }
        }

        protected override bool IsIconVisible(Tag tag)
        {
            if (tag.IsRoleIconVisibleInChat.InheritedValue != null)
            {
                return tag.IsRoleIconVisibleInChat.InheritedValue.Value;
            }

            return false;
        }

        protected override bool IsTextVisible(Tag tag)
        {
            if (tag.IsTextVisibleInChat.InheritedValue != null)
            {
                return tag.IsTextVisibleInChat.InheritedValue.Value;
            }

            return false;
        }

        /// <summary>
        /// Searches the given string for game object matches.
        /// </summary>
        /// <param name="seString">The string to search.</param>
        /// <returns>A list of matched game objects.</returns>
        private List<StringMatch> GetStringMatches(SeString seString)
        {
            List<StringMatch> stringMatches = new();
            Stack<PlayerPayload> curPlayerPayload = new();
            Stack<List<Payload>> curRefPayloads = new();
            var defaultRawPayload = RawPayload.LinkTerminator.Data;

            foreach (var payload in seString.Payloads)
            {

                if (payload is PlayerPayload playerPayload)
                {
                    curPlayerPayload.Push(playerPayload);
                    curRefPayloads.Push(new List<Payload>());
                }
                else if (payload is RawPayload rawPayload)
                {
                    if (defaultRawPayload.SequenceEqual(rawPayload.Data))
                        finishCurrentMatch(rawPayload);
                }
                else
                {
                    if (curRefPayloads.TryPeek(out List<Payload> result))
                        result.Add(payload);
                }
            }

            // Finally finish, if not closed by RawPayload
            finishCurrentMatch(null);

            void finishCurrentMatch(RawPayload linkTerminatorPayload)
            {
                if (curPlayerPayload.TryPop(out PlayerPayload playerPayload))
                {
                    var gameObject = PluginServices.ObjectTable.FirstOrDefault(gameObject => gameObject.Name.TextValue == playerPayload.PlayerName);
                    var stringMatch = new StringMatch(seString)
                    {
                        GameObject = gameObject,
                        PlayerPayload = playerPayload,
                        LinkTerminatorPayload = linkTerminatorPayload,
                        DisplayTextPayloads = curRefPayloads.Pop()
                    };
                    stringMatches.Add(stringMatch);
                }
            }

            return stringMatches;
        }

        private void SplitOffPartyNumberPrefix(SeString sender, XivChatType type)
        {
            if (type == XivChatType.Party || type == XivChatType.Alliance)
            {
                PlayerPayload lastPlayerPayload = null;
                foreach (var payload in sender.Payloads.ToArray())
                {
                    if (payload is PlayerPayload playerPayload)
                        lastPlayerPayload = playerPayload;
                    else if (payload is TextPayload playerNamePayload && lastPlayerPayload != null)
                    {
                        // Get position of player name in payload
                        var indexOfPlayerName = playerNamePayload.Text.IndexOf(BuildPlayername(lastPlayerPayload.PlayerName));

                        if (indexOfPlayerName == -1)
                            indexOfPlayerName = playerNamePayload.Text.IndexOf(lastPlayerPayload.PlayerName);

                        if (indexOfPlayerName > 0)
                        {
                            // Split off the name from the prefix number
                            var prefixPayload = new TextPayload(playerNamePayload.Text[..indexOfPlayerName]);
                            playerNamePayload.Text = playerNamePayload.Text[indexOfPlayerName..];

                            // Add prefix number before the player name payload
                            var playerNamePayloadIndex = sender.Payloads.IndexOf(playerNamePayload);
                            sender.Payloads.Insert(playerNamePayloadIndex, prefixPayload);
                        }
                    }
                }
            }
        }

        private void ParsePayloadsForOwnPlayer(SeString seString, XivChatType chatType, bool isSender)
        {
            if (PluginServices.ClientState.LocalPlayer != null)
            {
                foreach (var payload in seString.Payloads.ToArray())
                {
                    if (payload is TextPayload textPayload)
                    {
                        List<TextPayload> playerTextPayloads = new List<TextPayload>();

                        var playerName = PluginServices.ClientState.LocalPlayer.Name.TextValue;
                        var playerNameShorted = BuildPlayername(playerName);

                        if (textPayload.Text == playerName || textPayload.Text == playerNameShorted)
                        {
                            playerTextPayloads.Add(textPayload);
                        }
                        else
                        {
                            var usedPlayerName = chatType == XivChatType.Party || chatType == XivChatType.Alliance ? playerNameShorted : playerName;
                            var textMatchIndex = textPayload.Text.IndexOf(usedPlayerName);

                            while (textMatchIndex >= 0)
                            {
                                var textPayloadIndex = seString.Payloads.IndexOf(payload);

                                // Chop text to the left and insert it as a new payload
                                if (textMatchIndex > 0)
                                {
                                    // Add the content before the player
                                    seString.Payloads.Insert(textPayloadIndex++, new TextPayload(textPayload.Text.Substring(0, textMatchIndex)));

                                    // Remove from the chopped text from the original payload
                                    textPayload.Text = textPayload.Text.Substring(textMatchIndex, textPayload.Text.Length - textMatchIndex);
                                }

                                // This is the last reference to the local player in this payload
                                if (textPayload.Text.Length == usedPlayerName.Length)
                                {
                                    playerTextPayloads.Add(textPayload);
                                    break;
                                }

                                // Create the new name payload and add it
                                var playerTextPayload = new TextPayload(usedPlayerName);
                                playerTextPayloads.Add(playerTextPayload);
                                seString.Payloads.Insert(textPayloadIndex, playerTextPayload);

                                // Remove from the chopped text from the original payload
                                textPayload.Text = textPayload.Text.Substring(usedPlayerName.Length);

                                textMatchIndex = textPayload.Text.IndexOf(usedPlayerName);
                            }
                        }

                        foreach (var playerTextPayload in playerTextPayloads)
                        {
                            // Fix displaying of abbreviated  own player name as the game does this after the chat message handler
                            playerTextPayload.Text = BuildPlayername(playerTextPayload.Text);

                            var playerPayload = new PlayerPayload(playerName, PluginServices.ClientState.LocalPlayer.HomeWorld.Id);
                            int playerPayloadIndex = seString.Payloads.IndexOf(playerTextPayload);
                            var hasNumberPrefix = isSender && (chatType == XivChatType.Party || chatType == XivChatType.Alliance);

                            // Ensure to include the group number prefix within the player link
                            if (hasNumberPrefix)
                                playerPayloadIndex--;

                            // Add the Player Link Payload
                            seString.Payloads.Insert(playerPayloadIndex++, playerPayload);

                            // Same as above, but reverse
                            if (hasNumberPrefix)
                                playerPayloadIndex++;

                            // Add the Link Terminator to end the Player Link. This should be done behind the Text Payload (display text).
                            // Normally used to end PlayerPayload linking. But for the own player it has no affect. Anyway, use it, just because. Maybe it's needed in the future somewhere else.
                            seString.Payloads.Insert(++playerPayloadIndex, RawPayload.LinkTerminator);

                            // I M P O R T A N T   N O T I C E:
                            // The PlayerPayload is now just temporary. We keep the TextPayload.
                            // The PayerPayload gets removed at the ChatTagTargetFeature at the end and the TextPayload will be keeped there.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds all configured tags to a chat message.
        /// </summary>
        /// <param name="message">The message to change.</param>
        private void AddTagsToChat(SeString message, XivChatType chatType, bool isSender)
        {
            // Parse Payloads for local player to be able to work with in the following code
            ParsePayloadsForOwnPlayer(message, chatType, isSender);

            // Split out the party/alliance number from the PlayerPayload
            if (isSender)
                SplitOffPartyNumberPrefix(message, chatType);

            var tagsData = m_PluginData.GetTagsData(ActivityContextManager.CurrentActivityContext.ZoneType);
            var stringMatches = GetStringMatches(message);

            foreach (var stringMatch in stringMatches)
            {
                StringChanges stringChanges = new();

                bool isTagEnabled(Tag tag)
                    => tag.TagPositionInChat.InheritedValue != null && tag.TargetChatTypes.InheritedValue != null &&
                    (tag.TargetChatTypes.InheritedValue.Contains(chatType) || (!Enum.IsDefined(chatType) && (tag.TargetChatTypesIncludeUndefined?.InheritedValue ?? false)));

                if (stringMatch.GameObject is PlayerCharacter playerCharacter)
                {
                    // Add the job tag
                    if (playerCharacter.ClassJob.GameData != null && tagsData.JobTags.TryGetValue(playerCharacter.ClassJob.GameData.Abbreviation, out var jobTag))
                    {
                        if (isTagEnabled(jobTag))
                        {
                            var payloads = GetPayloads(jobTag, stringMatch.GameObject);
                            if (payloads.Any())
                            {
                                var insertBehindNumberPrefix = jobTag.InsertBehindNumberPrefixInChat?.Value ?? true;
                                addPayloadChanges(jobTag, payloads);
                            }
                        }
                    }

                    // Add randomly generated name tag payload
                    if (m_PluginConfiguration.IsPlayerNameRandomlyGenerated)
                    {
                        var playerName = stringMatch.GetMatchText();
                        if (playerName != null)
                        {
                            var generatedName = BuildPlayername(RandomNameGenerator.Generate(playerName));
                            if (generatedName != null)
                            {
                                AddPayloadChanges(StringPosition.Replace, Enumerable.Empty<Payload>().Append(new TextPayload(generatedName)), stringChanges, false);
                            }
                        }
                    }
                }

                // Add custom tags
                if (stringMatch.PlayerPayload != null)
                {
                    Identity identity = tagsData.GetIdentity(stringMatch.PlayerPayload);
                    foreach (var customTagId in identity.CustomTagIds)
                    {
                        var customTag = tagsData.CustomTags.FirstOrDefault(tag => tag.CustomId.Value == customTagId);
                        if (customTag != null)
                        {
                            if (isTagEnabled(customTag))
                            {
                                var customTagPayloads = GetPayloads(customTag, stringMatch.GameObject);
                                if (customTagPayloads.Any())
                                {
                                    var insertBehindNumberPrefix = customTag.InsertBehindNumberPrefixInChat?.Value ?? true;
                                    addPayloadChanges(customTag, customTagPayloads);
                                }
                            }
                        }
                    }
                }

                void addPayloadChanges(Tag tag, IEnumerable<Payload> payloads)
                {
                    var insertBehindNumberPrefix = tag.InsertBehindNumberPrefixInChat?.Value ?? true;
                    var insertPositionInChat = tag.TagPositionInChat.InheritedValue.Value;
                    AddPayloadChanges((StringPosition)insertPositionInChat, payloads, stringChanges, insertBehindNumberPrefix);
                }

                // An additional step to apply text color to additional locations
                if (stringMatch.PlayerPayload != null && stringMatch.DisplayTextPayloads.Any())
                {
                    Identity identity = tagsData.GetIdentity(stringMatch.PlayerPayload);

                    if (stringMatch.GameObject is PlayerCharacter playerCharacter1)
                    {
                        if (playerCharacter1.ClassJob.GameData != null && tagsData.JobTags.TryGetValue(playerCharacter1.ClassJob.GameData.Abbreviation, out var jobTag) && isTagEnabled(jobTag))
                            applyTextFormatting(jobTag);
                    }

                    foreach (var customTagId in identity.CustomTagIds)
                    {
                        var customTag = tagsData.CustomTags.FirstOrDefault(tag => tag.CustomId.Value == customTagId);
                        if (customTag != null && isTagEnabled(customTag))
                            applyTextFormatting(customTag);
                    }

                    void applyTextFormatting(Tag tag)
                        => ApplyTextFormatting(stringMatch.GameObject, tag, new[] { message }, new[] { tag.IsTextColorAppliedToChatName }, stringMatch.DisplayTextPayloads);
                }

                // Finally apply the all the changes to the message
                ApplyStringChanges(message, stringChanges, stringMatch.DisplayTextPayloads, stringMatch.PlayerNamePayload);

                // Remove PlayerPayload and LinkTerminator if it's your own character (they just got added temporary)
                if (stringMatch.IsLocalPlayer)
                {
                    if (stringMatch.PlayerPayload != null)
                        message.Remove(stringMatch.PlayerPayload);
                    if (stringMatch.LinkTerminatorPayload != null)
                        message.Remove(stringMatch.LinkTerminatorPayload);
                }
            }
        }
    }
}
