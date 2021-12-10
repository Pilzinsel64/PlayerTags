﻿using Dalamud.Game.Text.SeStringHandling;
using PlayerTags.Inheritables;
using PlayerTags.PluginStrings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerTags.Data
{
    public class Tag
    {
        public IPluginString Name { get; init; }

        private Tag? m_Parent = null;
        public Tag? Parent
        {
            get => m_Parent;
            set
            {
                if (m_Parent != value)
                {
                    if (m_Parent != null)
                    {
                        if (m_Parent.Children.Contains(this))
                        {
                            m_Parent.Children.Remove(this);
                        }
                    }

                    m_Parent = value;
                    if (m_Parent != null)
                    {
                        m_Parent.Children.Add(this);
                        foreach ((var name, IInheritable inheritable) in Inheritables)
                        {
                            inheritable.Parent = m_Parent.Inheritables[name];
                        }
                    }
                }
            }
        }

        public List<Tag> Children { get; } = new List<Tag>();

        public IEnumerable<Tag> Descendents
        {
            get
            {
                IEnumerable<Tag> descendents = Children.Prepend(this);

                foreach (var child in Children)
                {
                    descendents = descendents.Union(child.Descendents);
                }

                return descendents.Distinct();
            }
        }

        private Dictionary<string, IInheritable>? m_Inheritables = null;
        public Dictionary<string, IInheritable> Inheritables
        {
            get
            {
                if (m_Inheritables == null)
                {
                    m_Inheritables = new Dictionary<string, IInheritable>();

                    var inheritableFields = GetType().GetFields().Where(field => typeof(IInheritable).IsAssignableFrom(field.FieldType));
                    foreach (var inheritableField in inheritableFields)
                    {
                        IInheritable? inheritable = inheritableField.GetValue(this) as IInheritable;
                        if (inheritable != null)
                        {
                            Inheritables[inheritableField.Name] = inheritable;
                        }
                    }
                }

                return m_Inheritables!;
            }
        }

        public InheritableValue<bool> IsSelected = new InheritableValue<bool>(false);
        public InheritableValue<bool> IsExpanded = new InheritableValue<bool>(false);

        [InheritableCategory("General")]
        public InheritableReference<string> GameObjectNamesToApplyTo = new InheritableReference<string>("");

        [InheritableCategory("Icon")]
        public InheritableValue<BitmapFontIcon> Icon = new InheritableValue<BitmapFontIcon>(BitmapFontIcon.Aethernet);
        [InheritableCategory("Icon")]
        public InheritableValue<bool> IsIconVisibleInChat = new InheritableValue<bool>(false);
        [InheritableCategory("Icon")]
        public InheritableValue<bool> IsIconVisibleInNameplates = new InheritableValue<bool>(false);

        [InheritableCategory("Text")]
        public InheritableReference<string> Text = new InheritableReference<string>("");
        [InheritableCategory("Text")]
        public InheritableValue<ushort> TextColor = new InheritableValue<ushort>(6);
        [InheritableCategory("Text")]
        public InheritableValue<ushort> TextGlowColor = new InheritableValue<ushort>(6);
        [InheritableCategory("Text")]
        public InheritableValue<bool> IsTextItalic = new InheritableValue<bool>(false);
        [InheritableCategory("Text")]
        public InheritableValue<bool> IsTextVisibleInChat = new InheritableValue<bool>(false);
        [InheritableCategory("Text")]
        public InheritableValue<bool> IsTextVisibleInNameplates = new InheritableValue<bool>(false);

        [InheritableCategory("Position")]
        public InheritableValue<TagPosition> TagPositionInChat = new InheritableValue<TagPosition>(TagPosition.Before);
        [InheritableCategory("Position")]
        public InheritableValue<TagPosition> TagPositionInNameplates = new InheritableValue<TagPosition>(TagPosition.Before);
        [InheritableCategory("Position")]
        public InheritableValue<NameplateElement> TagTargetInNameplates = new InheritableValue<NameplateElement>(NameplateElement.Name);

        [InheritableCategory("ActivityVisibility")]
        public InheritableValue<bool> IsVisibleInPveDuties = new InheritableValue<bool>(false);
        [InheritableCategory("ActivityVisibility")]
        public InheritableValue<bool> IsVisibleInPvpDuties = new InheritableValue<bool>(false);
        [InheritableCategory("ActivityVisibility")]
        public InheritableValue<bool> IsVisibleInOverworld = new InheritableValue<bool>(false);

        [InheritableCategory("PlayerVisibility")]
        public InheritableValue<bool> IsVisibleForSelf = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerVisibility")]
        public InheritableValue<bool> IsVisibleForFriendPlayers = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerVisibility")]
        public InheritableValue<bool> IsVisibleForPartyPlayers = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerVisibility")]
        public InheritableValue<bool> IsVisibleForAlliancePlayers = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerVisibility")]
        public InheritableValue<bool> IsVisibleForEnemyPlayers = new InheritableValue<bool>(false);
        [InheritableCategory("PlayerVisibility")]
        public InheritableValue<bool> IsVisibleForOtherPlayers = new InheritableValue<bool>(false);

        public string[] SplitGameObjectNamesToApplyTo
        {
            get
            {
                if (GameObjectNamesToApplyTo == null || GameObjectNamesToApplyTo.InheritedValue == null)
                {
                    return new string[] { };
                }

                return GameObjectNamesToApplyTo.InheritedValue.Split(';', ',').Where(item => !string.IsNullOrEmpty(item)).ToArray();
            }
        }

        private string[] CleanGameObjectNamesToApplyTo
        {
            get
            {
                return SplitGameObjectNamesToApplyTo.Select(gameObjectName => gameObjectName.ToLower().Trim()).ToArray();
            }
        }

        public Tag(IPluginString name)
        {
            Name = name;
        }

        public bool IncludesGameObjectNameToApplyTo(string gameObjectName)
        {
            return CleanGameObjectNamesToApplyTo.Contains(gameObjectName.ToLower());
        }

        public void AddGameObjectNameToApplyTo(string gameObjectName)
        {
            if (IncludesGameObjectNameToApplyTo(gameObjectName))
            {
                return;
            }

            List<string> newSplitGameObjectNamesToApplyTo = SplitGameObjectNamesToApplyTo.ToList();

            newSplitGameObjectNamesToApplyTo.Add(gameObjectName);

            GameObjectNamesToApplyTo = string.Join(",", newSplitGameObjectNamesToApplyTo);
        }

        public void RemoveGameObjectNameToApplyTo(string gameObjectName)
        {
            if (!IncludesGameObjectNameToApplyTo(gameObjectName))
            {
                return;
            }

            List<string> newSplitGameObjectNamesToApplyTo = SplitGameObjectNamesToApplyTo.ToList();

            var index = Array.IndexOf(CleanGameObjectNamesToApplyTo, gameObjectName.ToLower());
            newSplitGameObjectNamesToApplyTo.RemoveAt(index);

            GameObjectNamesToApplyTo = string.Join(",", newSplitGameObjectNamesToApplyTo);
        }

        public Dictionary<string, InheritableData> GetChanges(Dictionary<string, InheritableData>? defaultChanges = null)
        {
            Dictionary<string, InheritableData> changes = new Dictionary<string, InheritableData>();

            foreach ((var name, var inheritable) in Inheritables)
            {
                // If there's a default for this name, only set the value if it's different from the default
                if (defaultChanges != null && defaultChanges.TryGetValue(name, out var defaultInheritableData))
                {
                    var inheritableData = inheritable.GetData();
                    if (inheritableData.Behavior != defaultInheritableData.Behavior ||
                        !inheritableData.Value.Equals(defaultInheritableData.Value))
                    {
                        changes[name] = inheritable.GetData();
                    }
                }
                // If there's no default, then only set the value if it's not inherited
                else if (inheritable.Behavior != InheritableBehavior.Inherit)
                {
                    changes[name] = inheritable.GetData();
                }
            }

            return changes;
        }

        public void SetChanges(Dictionary<string, InheritableData> changes)
        {
            foreach ((var name, var inheritableData) in changes)
            {
                Inheritables[name].SetData(inheritableData);
            }
        }
    }
}