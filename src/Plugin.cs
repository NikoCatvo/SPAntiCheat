using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace NitroShield
{
    [BepInPlugin(Guid, "System Reminder", "1.0.0")]
    [BepInProcess("Among Us.exe")]
    public class NitroShieldPlugin : BasePlugin
    {
        public const string Guid = "com.well.nitroshield";
        public const string ConfigFileName = "com.well.nitroanticheat.cfg";

        public new static ManualLogSource Log;
        private static Harmony _harmony;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("SP Anti Cheat loading...");

            LoadForcedConfig();
            // NameFilter.Load(); // Blocklist功能已禁用
            BannedPlayers.Load();


            _harmony = new Harmony(Guid);
            _harmony.PatchAll();
            AddComponent<NotificationOverlay>();
            AddComponent<Updater>();

            Log.LogInfo("SP Anti Cheat 已加载。");
        }

        private static string ConfigPath_ => Path.Combine(Paths.ConfigPath, ConfigFileName);

        private static void LoadForcedConfig()
        {
            if (!File.Exists(ConfigPath_))
            {
                Log.LogWarning("未找到配置文件 " + ConfigPath_ + "，使用默认值。");
                return;
            }

            try
            {
                var sections = ParseConfigFile(ConfigPath_);
                ApplyConfig(sections);
            }
            catch (Exception e)
            {
                Log.LogWarning("读取配置失败: " + e.Message);
            }
        }

        private static Dictionary<string, Dictionary<string, string>> ParseConfigFile(string path)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            var lines = File.ReadAllLines(path);
            string currentSection = "";

            foreach (var line in lines)
            {
                var t = line.Trim();
                if (t.Length == 0) continue;
                if (t.StartsWith("#")) continue;

                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    currentSection = t.Substring(1, t.Length - 2);
                    continue;
                }

                int eqIdx = t.IndexOf('=');
                if (eqIdx <= 0) continue;
                string key = t.Substring(0, eqIdx).Trim();
                string val = t.Substring(eqIdx + 1).Trim();

                if (!result.ContainsKey(currentSection))
                    result[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                result[currentSection][key] = val;
            }
            return result;
        }

        private static void ApplyConfig(Dictionary<string, Dictionary<string, string>> sections)
        {
            if (sections.ContainsKey("General"))
            {
                var g = sections["General"];
                ReadBool(g, "AntiCheatEnabled", ref Anticheat.Enabled);
                ReadBool(g, "DetectCheatClients", ref CheatClients.Enabled);
                ReadBool(g, "NameFilterEnabled", ref NameFilter.Enabled);
                ReadBool(g, "MeetingTimerEnabled", ref MeetingTimer.Enabled);
                ReadBool(g, "ModdedLobby", ref Anticheat.ModdedLobby);
                ReadBool(g, "DiscardRpc", ref Anticheat.DiscardRpc);
                ReadBool(g, "BannedListEnabled", ref BannedPlayers.Enabled);
                ReadBool(g, "SendNotification", ref Anticheat.SendNotification);
                ReadBool(g, "AceEnabled", ref Anticheat.AceEnabled);
                if (g.ContainsKey("Punishment"))
                    Anticheat.Punishment = ParsePunishment(g["Punishment"]);
            }

            if (sections.ContainsKey("Crash"))
            {
                var c = sections["Crash"];
                ReadBool(c, "CrashProtection", ref Anticheat.CrashProtection);
                ReadBool(c, "CheckMalformed", ref Anticheat.CheckMalformed);
                ReadBool(c, "CheckFlood", ref Anticheat.CheckFlood);
                ReadBool(c, "DetectUnknownRpc", ref Anticheat.DetectUnknownRpc);
                ReadInt(c, "FloodThreshold", ref Anticheat.FloodThreshold);
                ReadFloat(c, "FloodWindowSeconds", ref Anticheat.FloodWindow);
            }

            if (sections.ContainsKey("State"))
            {
                var s = sections["State"];
                ReadBool(s, "StateChecks", ref StateChecks.Enabled);
                ReadBool(s, "CheckCosmetics", ref StateChecks.CheckCosmetics);
                ReadBool(s, "CheckLobbyRpcs", ref StateChecks.CheckLobbyRpcs);
            }

            if (sections.ContainsKey("Meeting"))
            {
                var m = sections["Meeting"];
                ReadFloat(m, "GraceSeconds", ref MeetingTimer.GraceSeconds);
                ReadBool(m, "EmergencyOnly", ref MeetingTimer.EmergencyOnly);
            }

            if (sections.ContainsKey("Mute"))
            {
                var mu = sections["Mute"];
                ReadBool(mu, "MuteOnMajorityVote", ref MuteManager.MuteOnMajorityVote);
                ReadBool(mu, "MuteOnChatConsensus", ref MuteManager.MuteOnChatConsensus);
            }

            if (sections.ContainsKey("Protections"))
            {
                var p = sections["Protections"];
                ReadBool(p, "BlockVentKickExploit", ref Protections.BlockVentKickExploit);
                ReadBool(p, "BlockVotingOverload", ref Protections.BlockVotingOverload);
                ReadBool(p, "BlockLargeMessages", ref Protections.BlockLargeMessages);
                ReadInt(p, "MaxMessageLength", ref Protections.MaxMessageLength);
            }

            if (sections.ContainsKey("Ventilation"))
            {
                var v = sections["Ventilation"];
                ReadBool(v, "Enabled", ref VentilationGuard.Enabled);
                ReadBool(v, "CheckVentTp", ref VentilationGuard.CheckVentTp);
                ReadBool(v, "CheckRole", ref VentilationGuard.CheckRole);
                ReadBool(v, "CheckSeq", ref VentilationGuard.CheckSeq);
                ReadBool(v, "CheckBootOps", ref VentilationGuard.CheckBootOps);
                ReadBool(v, "CheckMoveState", ref VentilationGuard.CheckMoveState);
            }

            if (sections.ContainsKey("VoteKick"))
            {
                var v = sections["VoteKick"];
                ReadBool(v, "Enabled", ref Rpc.VoteKickGuard.Enabled);
            }

            if (sections.ContainsKey("Rpc"))
            {
                var r = sections["Rpc"];
                foreach (var kvp in Anticheat.RpcHandlers)
                {
                    if (r.ContainsKey(kvp.Value.Name))
                    {
                        if (bool.TryParse(r[kvp.Value.Name], out bool val))
                            kvp.Value.Enabled = val;
                    }
                }
            }
        }

        private static void ReadBool(Dictionary<string, string> section, string key, ref bool field)
        {
            if (section.ContainsKey(key) && bool.TryParse(section[key], out bool val))
                field = val;
        }

        private static void ReadString(Dictionary<string, string> section, string key, ref string field)
        {
            if (section.ContainsKey(key) && !string.IsNullOrWhiteSpace(section[key]))
                field = section[key].Trim();
        }

        private static void ReadInt(Dictionary<string, string> section, string key, ref int field)
        {
            if (section.ContainsKey(key) && int.TryParse(section[key], out int val))
                field = val;
        }

        private static void ReadFloat(Dictionary<string, string> section, string key, ref float field)
        {
            if (section.ContainsKey(key) && float.TryParse(section[key], out float val))
                field = val;
        }

        private static Anticheat.Punishments ParsePunishment(string s)
        {
            s = s.Trim();
            if (s.Equals("None", StringComparison.OrdinalIgnoreCase) || s == "无")
                return Anticheat.Punishments.None;
            if (s.Equals("Kick", StringComparison.OrdinalIgnoreCase) || s == "踢出")
                return Anticheat.Punishments.Kick;
            if (s.Equals("Ban", StringComparison.OrdinalIgnoreCase) || s == "封禁")
                return Anticheat.Punishments.Ban;
            return Anticheat.Punishments.None;
        }
    }
}
