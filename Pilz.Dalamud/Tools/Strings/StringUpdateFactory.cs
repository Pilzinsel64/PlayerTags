using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pilz.Dalamud.Tools.Strings
{
    public static class StringUpdateFactory
    {
        public static void ApplyStringChanges(StringChangesProps props)
        {
            if (props.StringChanges != null && props.StringChanges.Any())
            {
                var seString = props.Destination;
                List<StringPosition> stringPositionsOrdered = GetOrderedStringPositions(props);

                foreach (var stringPosition in stringPositionsOrdered)
                {
                    var stringChange = props.StringChanges.GetChange(stringPosition);
                    if (stringChange != null && stringChange.Payloads.Any())
                    {
                        AddSpacesBetweenTextPayloads(stringChange.Payloads, stringPosition);

                        if (stringPosition == StringPosition.Before)
                        {
                            Payload anchorFirst = stringChange.ForceUsingSingleAnchorPayload ? props.AnchorPayload : props.AnchorPayloads?.FirstOrDefault();

                            if (anchorFirst != null)
                            {
                                var anchorPayloadIndex = seString.Payloads.IndexOf(anchorFirst);
                                seString.Payloads.InsertRange(anchorPayloadIndex, stringChange.Payloads);
                            }
                            else
                                seString.Payloads.InsertRange(0, stringChange.Payloads);
                        }
                        else if (stringPosition == StringPosition.After)
                        {
                            Payload anchorLast = stringChange.ForceUsingSingleAnchorPayload ? props.AnchorPayload : props.AnchorPayloads?.LastOrDefault();

                            if (anchorLast != null)
                            {
                                var anchorPayloadIndex = seString.Payloads.IndexOf(anchorLast);
                                seString.Payloads.InsertRange(anchorPayloadIndex + 1, stringChange.Payloads);
                            }
                            else
                                seString.Payloads.AddRange(stringChange.Payloads);
                        }
                        else if (stringPosition == StringPosition.Replace)
                        {
                            Payload anchorReplace = props.AnchorPayload;

                            if (anchorReplace != null)
                            {
                                var anchorPayloadIndex = seString.Payloads.IndexOf(anchorReplace);
                                seString.Payloads.InsertRange(anchorPayloadIndex, stringChange.Payloads);
                                seString.Remove(anchorReplace);
                            }
                            else
                            {
                                seString.Payloads.Clear();
                                seString.Payloads.AddRange(stringChange.Payloads);
                            }
                        }
                    }
                }
            }
        }

        private static void AddSpacesBetweenTextPayloads(List<Payload> payloads, StringPosition tagPosition)
        {
            if (payloads != null && payloads.Any())
            {
                var indicesToInsertSpacesAt = new List<int>();
                var lastTextPayloadIndex = -1;

                static TextPayload getNewTextPayload() => new(" ");

                foreach (var payload in payloads.Reverse<Payload>())
                {
                    if (payload is IconPayload iconPayload)
                        lastTextPayloadIndex = -1;
                    else if (payload is TextPayload textPayload)
                    {
                        if (lastTextPayloadIndex != -1)
                            indicesToInsertSpacesAt.Add(payloads.IndexOf(textPayload) + 1);
                        lastTextPayloadIndex = payloads.IndexOf(textPayload);
                    }
                }

                foreach (var indexToInsertSpaceAt in indicesToInsertSpacesAt)
                    payloads.Insert(indexToInsertSpaceAt, getNewTextPayload());

                // Decide whether to add a space to the end
                if (tagPosition == StringPosition.Before)
                {
                    var significantPayloads = payloads.Where(payload => payload is TextPayload || payload is IconPayload);
                    if (significantPayloads.LastOrDefault() is TextPayload)
                        payloads.Add(getNewTextPayload());
                }
                // Decide whether to add a space to the beginning
                else if (tagPosition == StringPosition.After)
                {
                    var significantPayloads = payloads.Where(payload => payload is TextPayload || payload is IconPayload);
                    if (significantPayloads.FirstOrDefault() is TextPayload)
                        payloads.Insert(0, getNewTextPayload());
                }
            }
        }

        private static List<StringPosition> GetOrderedStringPositions(StringChangesProps props)
        {
            var tagPositionsOrdered = new List<StringPosition>();

            // If there's no anchor payload, do replaces first so that befores and afters are based on the replaced data
            if (props.AnchorPayloads == null || !props.AnchorPayloads.Any())
                tagPositionsOrdered.Add(StringPosition.Replace);

            tagPositionsOrdered.Add(StringPosition.Before);
            tagPositionsOrdered.Add(StringPosition.After);

            // If there is an anchor payload, do replaces last so that we still know which payload needs to be removed
            if (props.AnchorPayloads != null && props.AnchorPayloads.Any())
                tagPositionsOrdered.Add(StringPosition.Replace);

            return tagPositionsOrdered;
        }
    }
}
