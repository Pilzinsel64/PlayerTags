using Dalamud.Logging;
using Newtonsoft.Json;
using System;

namespace PlayerTags.Inheritables
{
    public class InheritableValue<T> : IInheritable
        where T : struct
    {
        public IInheritable? Parent { get; set; }

        public InheritableBehavior Behavior { get; set; }

        [JsonProperty]
        public T Value;

        [JsonIgnore]
        public T? InheritedValue
        {
            get
            {
                IInheritable? current = this;
                while (current != null)
                {
                    if (current.Behavior == InheritableBehavior.Enabled && current is InheritableValue<T> currentOfSameType)
                    {
                        return currentOfSameType.Value;
                    }
                    else if (current.Behavior == InheritableBehavior.Disabled)
                    {
                        return default;
                    }

                    current = current.Parent;
                }

                return default;
            }
        }

        public static implicit operator InheritableValue<T>(T value) => new InheritableValue<T>(value)
        {
            Behavior = InheritableBehavior.Enabled
        };

        public InheritableValue(T value)
        {
            Value = value;
        }

        public void SetData(InheritableData inheritableData)
        {
            Behavior = inheritableData.Behavior;

            try
            {
                if (typeof(T).IsEnum && inheritableData.Value != null)
                {
                    if (inheritableData.Value is string stringValue)
                    {
                        Value = (T)Enum.Parse(typeof(T), stringValue);
                    }
                    else
                    {
                        Value = (T)Enum.ToObject(typeof(T), inheritableData.Value);
                    }
                }
                else if (inheritableData.Value == null)
                {
                    // This should never happen
                    PluginServices.PluginLog.Error($"Expected value of type {Value.GetType()} but received null");
                }
                else if (typeof(T) == typeof(Guid) && inheritableData.Value is string strValue)
                {
                    Value = (T)(object)Guid.Parse(strValue);
                }
                else
                {
                    Value = (T)Convert.ChangeType(inheritableData.Value, typeof(T));
                }
            }
            catch (Exception ex)
            {
                PluginServices.PluginLog.Error(ex, $"Failed to convert {inheritableData.Value.GetType()} value '{inheritableData.Value}' to {Value.GetType()}");
            }
        }

        public InheritableData GetData()
        {
            return new InheritableData
            {
                Behavior = Behavior,
                Value = Value
            };
        }
    }
}
