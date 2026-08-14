using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace NitroShield
{
    internal static class BannedPlayers
    {
        public static bool Enabled = true;
        public static string RawText = "";

        private static readonly HashSet<string> _entries = new(StringComparer.OrdinalIgnoreCase);
        private static string Path_ => System.IO.Path.Combine(Paths.ConfigPath, "SPAntiCheat_banned.txt");

        public static void Load()
        {
            if (!File.Exists(Path_))
            {
                // 初始文件不写入任何注释，仅保留纯粹的封禁条目
                RawText = "";
                File.WriteAllText(Path_, RawText);
            }
            else
            {
                RawText = File.ReadAllText(Path_);
            }
            Reparse();
        }

        public static void Save()
        {
            try { File.WriteAllText(Path_, RawText); }
            catch (Exception e) { NitroShieldPlugin.Log.LogWarning(Strings.LogFailedToSaveBannedList(e.Message)); }
            Reparse();
            GameNotification.Show(string.Format(Strings.NotifyBannedListSaved, _entries.Count));
        }

        private static void Reparse()
        {
            _entries.Clear();
            foreach (var raw in RawText.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                _entries.Add(line);
            }
        }

        public static void Add(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry)) return;
            // 防止重复封禁：若已经在列表中直接返回
            if (IsBannedByCode(entry))
            {
                NitroShieldPlugin.Log.LogInfo($"[BannedPlayers] {entry} 已在封禁名单中，跳过写入");
                return;
            }
            if (!RawText.EndsWith("\n")) RawText += "\n";
            RawText += entry.Trim() + "\n";
            Save();
        }

        public static bool IsBanned(PlayerControl p)
        {
            if (!Enabled || p == null || p.Data == null || _entries.Count == 0) return false;
            var code = p.Data.FriendCode ?? "";
            return IsBannedByCode(code);
        }

        public static bool IsBannedByCode(string code)
        {
            if (!Enabled || _entries.Count == 0) return false;
            if (string.IsNullOrWhiteSpace(code)) return false;
            return _entries.Contains(code);
        }

        public static List<string> EntryList()
        {
            var list = new List<string>(_entries);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        public static void Remove(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry)) return;
            var kept = new List<string>();
            foreach (var raw in RawText.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) { kept.Add(raw); continue; }
                if (string.Equals(line, entry.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                kept.Add(raw);
            }
            RawText = string.Join("\n", kept);
            Save();
        }
    }
}
