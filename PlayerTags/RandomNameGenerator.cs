using Dalamud.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace PlayerTags
{
    /// <summary>
    /// Generates names based on existing lists of words.
    /// </summary>
    public static class RandomNameGenerator
    {
        private static string[]? s_Adjectives;
        private static string[] Adjectives
        {
            get
            {
                if (s_Adjectives == null)
                {
                    try
                    {
                        s_Adjectives = File.ReadAllLines(Path.Combine(MyPaths.ResourcePath, Resources.Paths.AdjectivesTxt));
                    }
                    catch (Exception ex)
                    {
                        PluginServices.PluginLog.Error(ex, $"RandomNameGenerator failed to read adjectives");
                    }
                }

                if (s_Adjectives != null)
                {
                    return s_Adjectives;
                }

                return new string[] { };
            }
        }

        private static string[]? s_Nouns;
        private static string[] Nouns
        {
            get
            {
                if (s_Nouns == null)
                {
                    try
                    {
                        s_Nouns = File.ReadAllLines(Path.Combine(MyPaths.ResourcePath, Resources.Paths.NounsTxt));
                    }
                    catch (Exception ex)
                    {
                        PluginServices.PluginLog.Error(ex, $"RandomNameGenerator failed to read nouns");
                    }
                }

                if (s_Nouns != null)
                {
                    return s_Nouns;
                }

                return new string[] { };
            }
        }

        /// <summary>
        /// Generates a name for the given string.
        /// </summary>
        /// <param name="str">The string to generate a name for.</param>
        /// <returns>A generated name.</returns>
        public static string? Generate(string str)
        {
            if (Adjectives == null || Nouns == null)
            {
                return null;
            }

            int hash = GetDeterministicHashCode(str);

            // Use the seed as the hash so the same player always gets the same name
            Random random = new Random(hash);
            var adjective = Adjectives[random.Next(0, Adjectives.Length)];
            var noun = Nouns[random.Next(0, Nouns.Length)];
            var generatedName = $"{adjective} {noun}";

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(generatedName);
        }

        /// <summary>
        /// Gets a deterministic hash code for the given string.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns>A deterministic hash code.</returns>
        private static int GetDeterministicHashCode(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int index = 0; index < str.Length; index += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[index];
                    if (index == str.Length - 1)
                    {
                        break;
                    }

                    hash2 = ((hash2 << 5) + hash2) ^ str[index + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
