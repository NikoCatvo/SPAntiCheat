using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;

namespace NitroShield
{
    internal static class NameFilter
    {
        public static bool Enabled = false; // 默认禁用屏蔽违规词功能

        private static readonly HashSet<string> Blocked = new(StringComparer.Ordinal);
        private static string BlocklistPath => Path.Combine(Paths.ConfigPath, "NitroShield_blocklist.txt");

        private static readonly Dictionary<char, char> LeetMap = new()
        {
            ['0'] = 'o', ['1'] = 'i', ['!'] = 'i', ['|'] = 'i',
            ['3'] = 'e', ['4'] = 'a', ['@'] = 'a', ['5'] = 's',
            ['$'] = 's', ['7'] = 't', ['+'] = 't', ['8'] = 'b',
            ['9'] = 'g', ['6'] = 'g', ['2'] = 'z',
        };

        private static readonly string[] SeedList =
        {
            "antipride",
            "nigger", "nigga", "faggot", "retard", "kike", "spic", "chink", "tranny",
        };

        public static void Load()
        {
            Blocked.Clear();

            if (!File.Exists(BlocklistPath))
            {
                var header = Strings.ConfigHeaderBlocklist;
                File.WriteAllText(BlocklistPath, header + string.Join("\n", SeedList) + "\n");
            }

            foreach (var raw in File.ReadAllLines(BlocklistPath))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var norm = Normalize(line);
                if (norm.Length > 0) Blocked.Add(norm);
            }

            NitroShieldPlugin.Log.LogInfo(Strings.LogLoadBlocklist(Blocked.Count));
        }

        public static bool IsOffensive(string name, out string matched)
        {
            matched = null;
            if (!Enabled || string.IsNullOrEmpty(name)) return false;

            var norm = Normalize(name);
            if (norm.Length == 0) return false;

            foreach (var term in Blocked)
            {
                if (norm.Contains(term))
                {
                    matched = term;
                    return true;
                }
            }
            return false;
        }

        public static string Normalize(string input)
        {
            var noTags = StripTags(input).ToLowerInvariant();

            var sb = new StringBuilder(noTags.Length);
            foreach (var ch in noTags)
            {
                char c = LeetMap.TryGetValue(ch, out var mapped) ? mapped : ch;
                if (c >= 'a' && c <= 'z') sb.Append(c);
            }

            return Deduplicate(sb.ToString());
        }

        private static string StripTags(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool inTag = false;
            foreach (var ch in s)
            {
                if (ch == '<') { inTag = true; continue; }
                if (ch == '>') { inTag = false; continue; }
                if (!inTag) sb.Append(ch);
            }
            return sb.ToString();
        }

        private static string Deduplicate(string s)
        {
            if (s.Length == 0) return s;
            var sb = new StringBuilder(s.Length);
            char prev = '\0';
            foreach (var ch in s)
            {
                if (ch != prev) sb.Append(ch);
                prev = ch;
            }
            return sb.ToString();
        }
    }
}
