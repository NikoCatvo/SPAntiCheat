using HarmonyLib;
using Hazel;
using NitroShield.Rpc;
using System;
using System.Collections.Generic;

namespace NitroShield
{
    internal static class Anticheat
    {
        public static bool Enabled = true;
        // /ace 开关 – 控制 SMAC 检测是否启用
        public static bool AceEnabled = true;
        // 检测模式：true 时仅检测违规行为而不进行任何惩罚或阻断（即使作为 Host）
        public static bool DetectOnly = false;
        // SMAC‑style 检测映射（仅在游戏进行阶段生效，已去除与 StateChecks/Protections 冲突的键）
        private static readonly Dictionary<RpcCalls, string> SMAC_Violations = new()
        {
            { RpcCalls.SetScanner, "异常医学扫描" },
            { RpcCalls.SetTasks, "异常任务设置" },
            { RpcCalls.StartMeeting, "异常会议控制" },
            { RpcCalls.ReportDeadBody, "异常报尸" },
            { RpcCalls.Shapeshift, "异常变形" },
            { RpcCalls.StartVanish, "异常消失/出现" },
            { RpcCalls.CompleteTask, "异常任务完成" }
        };
        public static bool ModdedLobby = false;
        public static bool IsModded() => ModdedLobby || Constants.IsVersionModded();

        public static bool CrashProtection = true;
        public static bool CheckMalformed  = true;
        public static bool CheckFlood      = true;
        public static int  FloodThreshold  = 50;
        public static float FloodWindow    = 1.0f;
        public static bool DetectUnknownRpc = true;

        public static readonly Dictionary<RpcCalls, RpcCheck> RpcHandlers = new()
        {
            { RpcCalls.CompleteTask,     new CompleteTask()     { Name = "CompleteTask",     DisplayName = "完成任务" } },
            { RpcCalls.CheckName,        new CheckName()        { Name = "CheckName",        DisplayName = "检查名字" } },
            { RpcCalls.SetName,          new SetName()          { Name = "SetName",          DisplayName = "设置名字" } },
            { RpcCalls.SendChat,         new SendChat()         { Name = "SendChat",         DisplayName = "发送聊天" } },
            { RpcCalls.ReportDeadBody,   new ReportDeadBody()   { Name = "ReportDeadBody",   DisplayName = "报告尸体" } },
            { RpcCalls.SetStartCounter,  new SetStartCounter()  { Name = "SetStartCounter",  DisplayName = "设置开始倒计时" } },
            { RpcCalls.EnterVent,        new EnterVent()        { Name = "EnterVent",        DisplayName = "进入通风口" } },
            { RpcCalls.ExitVent,         new ExitVent()         { Name = "ExitVent",         DisplayName = "离开通风口" } },
            { RpcCalls.BootFromVent,     new BootFromVent()     { Name = "BootFromVent",     DisplayName = "从通风口踢出" } },
            { RpcCalls.SnapTo,           new SnapTo()           { Name = "SnapTo",           DisplayName = "传送（SnapTo）" } },
            { RpcCalls.ClimbLadder,      new ClimbLadder()      { Name = "ClimbLadder",      DisplayName = "爬梯子" } },
            { RpcCalls.CheckMurder,      new CheckMurder()      { Name = "CheckMurder",      DisplayName = "检测击杀" } },
            { RpcCalls.MurderPlayer,     new MurderPlayer()     { Name = "MurderPlayer",     DisplayName = "击杀玩家" } },
            { RpcCalls.Shapeshift,       new Shapeshift()       { Name = "Shapeshift",       DisplayName = "变形" } },
            { RpcCalls.StartVanish,      new StartVanish()      { Name = "StartVanish",      DisplayName = "消失" } },
            { RpcCalls.ProtectPlayer,    new ProtectPlayer()    { Name = "ProtectPlayer",    DisplayName = "保护玩家" } },
            { RpcCalls.UpdateSystem,     new UpdateSystem()     { Name = "UpdateSystem",     DisplayName = "更新系统（sabotge）" } },
            { RpcCalls.CloseDoorsOfType, new CloseDoorsOfType() { Name = "CloseDoorsOfType", DisplayName = "关闭门" } },
            { RpcCalls.PlayAnimation,    new PlayAnimation()    { Name = "PlayAnimation",    DisplayName = "播放任务动画" } },
            { RpcCalls.Exiled,           new Exiled()           { Name = "Exiled",           DisplayName = "流放（Exiled）" } },
            { RpcCalls.SetColor,         new SetColor()         { Name = "SetColor",         DisplayName = "设置颜色" } },
            { RpcCalls.SetScanner,       new SetScanner()       { Name = "SetScanner",       DisplayName = "医学扫描" } },
            { RpcCalls.UsePlatform,      new UsePlatform()      { Name = "UsePlatform",      DisplayName = "使用传送台" } },
            { RpcCalls.SetLevel,         new SetLevel()         { Name = "SetLevel",         DisplayName = "设置等级" } },
        };

        private static readonly Dictionary<RpcCalls, int> MinBytes = new()
        {
            { RpcCalls.PlayAnimation, 1 }, { RpcCalls.CompleteTask, 1 }, { RpcCalls.SyncSettings, 1 },
            { RpcCalls.SetInfected, 1 }, { RpcCalls.CheckName, 1 }, { RpcCalls.SetName, 1 },
            { RpcCalls.CheckColor, 1 }, { RpcCalls.SetColor, 1 }, { RpcCalls.ReportDeadBody, 1 },
            { RpcCalls.MurderPlayer, 1 }, { RpcCalls.SendChat, 1 }, { RpcCalls.StartMeeting, 1 },
            { RpcCalls.SetScanner, 2 }, { RpcCalls.SendChatNote, 2 }, { RpcCalls.SetStartCounter, 1 },
            { RpcCalls.EnterVent, 1 }, { RpcCalls.ExitVent, 1 }, { RpcCalls.SnapTo, 8 },
            { RpcCalls.VotingComplete, 1 }, { RpcCalls.CastVote, 2 }, { RpcCalls.AddVote, 1 },
            { RpcCalls.CloseDoorsOfType, 1 }, { RpcCalls.SetTasks, 1 }, { RpcCalls.ClimbLadder, 2 },
        };

        private static HashSet<byte> _knownRpcIds;
        private static bool _knownRpcTried;
        private static HashSet<byte> KnownRpcIds()
        {
            if (_knownRpcTried) return _knownRpcIds;
            _knownRpcTried = true;
            try
            {
                var set = new HashSet<byte>();
                foreach (RpcCalls r in Enum.GetValues(typeof(RpcCalls))) set.Add((byte)r);
                if (set.Count > 0) _knownRpcIds = set;
            }
            catch { _knownRpcIds = null; }
            return _knownRpcIds;
        }

        public enum Punishments { None, Kick, Ban }

        public static Punishments Punishment = Punishments.None;
        public static bool SendNotification = true;
        public static bool DiscardRpc = true;

        private static readonly RateTracker _rpcRate = new();
        // 跟踪玩家完成任务时间戳（0.1 秒内完成 3 个以上任务 = 极速完成外挂）
        private static readonly Dictionary<int, List<float>> _taskTimes = new();

        /// <summary>记录完成时间，若 0.1 秒内完成 3 个以上任务则判定为极速完成外挂。</summary>
        internal static bool RecordTaskBurst(PlayerControl player)
        {
            if (player == null) return false;

            float now = UnityEngine.Time.realtimeSinceStartup;
            if (!_taskTimes.TryGetValue(player.OwnerId, out var times))
            {
                times = new List<float>();
                _taskTimes[player.OwnerId] = times;
            }
            times.Add(now);
            times.RemoveAll(t => now - t > 0.1f);
            return times.Count >= 3;
        }

        internal static void ResetTaskTracking()
        {
            _taskTimes.Clear();
        }
        // 最近一次死亡的时间戳（使用 Time.realtimeSinceStartup），用于宽限期检测
        private static readonly Dictionary<int, float> _lastDeathTime = new();

        public static void RecordDeath(PlayerControl player)
        {
            if (player == null) return;
            _lastDeathTime[player.OwnerId] = UnityEngine.Time.realtimeSinceStartup;
        }

        public static bool IsRecentDeath(PlayerControl player, int milliseconds = 3000)
        {
            if (player == null) return false;
            if (_lastDeathTime.TryGetValue(player.OwnerId, out var ts))
                return UnityEngine.Time.realtimeSinceStartup - ts <= milliseconds / 1000f;
            return false;
        }
        // 记录游戏正式开始的时间（首次收到 SyncSettings RPC）
        public static float GameStartTime = -1f;
        // 记录游戏正式开始的时间（首次收到 SyncSettings RPC）
        // 用于早期会议阻止检测（游戏开始后前几秒）
        private static bool InEarlyMeetingGrace()
        {
            // 优先使用轮次开始时间（更可靠），若未记录则回退到 SyncSettings 的时间
            if (MeetingTimer.IsRoundStarted)
                return MeetingTimer.SecondsSinceRoundStart() < MeetingTimer.GraceSeconds;
            if (GameStartTime < 0) return false;
            return UnityEngine.Time.realtimeSinceStartup - GameStartTime < MeetingTimer.GraceSeconds;
        }

        // --- Meeting sabotage block handling ---
        private static float _meetingStartTime = -1f;
        internal static bool IsMeetingBlockActive()
        {
            if (MeetingHud.Instance == null) return false;
            if (_meetingStartTime < 0) return false;
            // Only block sabotage after 5 seconds into the meeting
            return UnityEngine.Time.realtimeSinceStartup - _meetingStartTime >= 5f;
        }
        private static void ResetMeetingBlock() => _meetingStartTime = -1f;
        public static void StartMeetingBlock()
        {
            _meetingStartTime = UnityEngine.Time.realtimeSinceStartup;
        }
        public static void EndMeetingBlock()
        {
            ResetMeetingBlock();
        }
        // 记录最近一次针对同一玩家、同一提示（reason）的时间，防止短时间内重复弹窗
        private static readonly Dictionary<int, Dictionary<string, float>> _lastAlert = new();
        internal const float AlertCooldown = 8f; // 秒

        private static bool AmHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
        private static bool IsSelf(PlayerControl player)
            => player != null && (player.AmOwner || player == PlayerControl.LocalPlayer);

        public static bool IsExempt(PlayerControl player)
            => IsSelf(player) || TrustedPlayers.IsTrusted(player);

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        private static class OnPlayerControlRpc
        {
            private static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
                => HandleRpc(typeof(PlayerControl), __instance, callId, reader);
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
        private static class OnPlayerPhysicsRpc
        {
            private static bool Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
                => HandleRpc(typeof(PlayerPhysics), __instance.myPlayer, callId, reader);
        }

        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
        private static class OnNetTransformRpc
        {
            private static bool Prefix(CustomNetworkTransform __instance, byte callId, MessageReader reader)
                => HandleRpc(typeof(CustomNetworkTransform), __instance.myPlayer, callId, reader);
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.HandleRpc))]
        private static class OnShipStatusRpc
        {
            private static bool Prefix(byte callId, MessageReader reader)
                => HandleRpc(typeof(ShipStatus), null, callId, reader);
        }

        // SMAC 检测已移除
        // 记录未知玩家（如关门）提示的上次时间，避免刷屏
        // 已移除刷屏计时器

        private static bool HandleRpc(Type sourceNetObj, PlayerControl player, byte callId, MessageReader reader)
        {
            // 房间成员（非房主）视角：本模组只做检测与提示，
            // 不防御（不拦截异常）、不丢弃（不 return false 丢弃 RPC）。
            // 所有防御性丢弃/惩罚均仅在房主（AmHost）视角生效。
            bool isHost = AmHost;

            if (Protections.ShouldBlock(sourceNetObj, player, callId, reader))
                return isHost && !DetectOnly ? false : true;
            if (player != null && BannedPlayers.IsBanned(player))
                return isHost && !DetectOnly ? false : true;

            if (!Enabled) return true;
            if (IsExempt(player)) return true;

            RpcCalls rpc = (RpcCalls)callId;
            // 记录游戏开始时间（首次同步设置）
            if (rpc == RpcCalls.SyncSettings && GameStartTime < 0)
                GameStartTime = UnityEngine.Time.realtimeSinceStartup;
            bool blockRpc = false;

            if (player != null && CheatClients.Check(player, callId, reader))
                {/* cheat client detected – no punitive action, allow RPC to proceed */}

            if (player != null)
            {
                if (CrashProtection)
                {
                    if (CheckMalformed && MinBytes.TryGetValue(rpc, out int min) && reader.Length < min)
                    {
                        Flag(player, Strings.ViolationMalformedRpc(Name(player), Strings.RpcName(rpc)));
                        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                    }
                    if (CheckFlood && !blockRpc)
                    {
                        int count = _rpcRate.Record(player.OwnerId, UnityEngine.Time.realtimeSinceStartup, FloodWindow);
                        if (count > FloodThreshold)
                        {
                            Flag(player, Strings.ViolationFlood(Name(player), count, FloodWindow));
                            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                        }
                    }
                }

                if (!blockRpc && DetectUnknownRpc && !IsModded())
                {
                    var known = KnownRpcIds();
                    if (known != null && !known.Contains(callId))
                    {
                        Flag(player, Strings.ViolationUnknownRpc(Name(player), callId));
                        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                    }
                }

            if (!blockRpc)
                StateChecks.Check(player, callId, ref blockRpc);

            // 禁止游戏开始后前 10 秒内的会议（包括紧急会议和报告尸体）
            if (!blockRpc && rpc == RpcCalls.StartMeeting)
            {
                if (InEarlyMeetingGrace())
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                    // 使用通用早会违规提示
                    string kind = "会议"; // generic meeting
                    float remaining = MeetingTimer.GraceSeconds - (UnityEngine.Time.realtimeSinceStartup - GameStartTime);
                    Anticheat.Flag(player, Strings.ViolationEarlyMeeting(Name(player), kind, remaining, MeetingTimer.GraceSeconds));
                }
            }
            }

            if (!blockRpc && RpcHandlers.TryGetValue(rpc, out var check) && check != null && check.Enabled)
            {
                if (check.GetExpectedNetObject() != sourceNetObj)
                    return AmHost ? false : true;

                if (AmHost && check.IsHostOnly())
                {
                    Flag(player, Strings.ViolationHostOnlyRpc(Name(player), Strings.RpcName(rpc)));
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                }
                else
                {
                    int savedPos = reader.Position;
                    try { check.Validate(player, reader, ref blockRpc); }
                    catch (Exception e)
                    {
                        NitroShieldPlugin.Log.LogWarning($"check for {rpc} threw: {e.Message}");
                        blockRpc = false;
                    }
                    reader.Position = savedPos;
                }
            }

            // SMAC‑style额外检测 – 通过 /ace 开关控制，仅在大厅阶段检测
            bool inGameplay = ShipStatus.Instance != null && LobbyBehaviour.Instance == null;
            if (AceEnabled && !inGameplay && !blockRpc && player != null && SMAC_Violations.TryGetValue(rpc, out var smacMsg))
                Flag(player, smacMsg);
            // 仅房主 + 未开启检测模式 + 允许丢弃时才真正丢弃该 RPC；
            // 房间成员（非房主）永不丢弃，只做检测提示。
            if (isHost && !DetectOnly && DiscardRpc && blockRpc) return false;
            return true;
        }

        public static void Flag(PlayerControl player, string reason, bool shouldPunish = true)
        {
            // 玩家名用红色高亮，违规原因保持原色
            var formatted = FormatWithRedName(player, reason);
            NitroShieldPlugin.Log.LogMessage($"[Shield] {reason}");
            if (SendNotification && player != null)
            {
                int pid = player.OwnerId;
                float now = UnityEngine.Time.realtimeSinceStartup;
                if (!_lastAlert.TryGetValue(pid, out var reasonDict))
                {
                    reasonDict = new Dictionary<string, float>();
                    _lastAlert[pid] = reasonDict;
                }
                if (reasonDict.TryGetValue(reason, out var lastTime) && now - lastTime < AlertCooldown)
                {
                    // 同一提示在冷却期内，跳过
                }
                else
                {
                    GameNotification.Show(formatted);
                    reasonDict[reason] = now;
                }
            }
            else if (SendNotification)
            {
                GameNotification.Show(formatted);
            }
            // 防御仅在房主视角生效；作为房间成员（非房主）只检测、不惩罚、不丢弃
            if (!DetectOnly && AmHost && shouldPunish && !IsExempt(player)) Punish(player);
        }

        public static void Flag(string reason)
        {
            NitroShieldPlugin.Log.LogMessage($"[Shield] {reason}");
            if (SendNotification) GameNotification.Show(reason);
        }

        /// <summary>将违规文案中的玩家名替换为红色富文本，其余文字保持不变。</summary>
        private static string FormatWithRedName(PlayerControl player, string reason)
        {
            if (player == null || string.IsNullOrEmpty(reason)) return reason;
            string name = player.Data?.PlayerName;
            if (string.IsNullOrEmpty(name)) return reason;
            // 用红色包裹玩家名（仅在文案中存在该名字时替换）
            string red = $"<color=#FF3B30><b>{name}</b></color>";
            return reason.Replace(name, red);
        }

        private static void Punish(PlayerControl player)
        {
            if (player == null) return;
            switch (Punishment)
            {
                case Punishments.None: break;
                case Punishments.Kick: AmongUsClient.Instance.KickPlayer(player.OwnerId, false); break;
                case Punishments.Ban:
                    if (player != null && player.Data != null && !string.IsNullOrWhiteSpace(player.Data.FriendCode))
                        BannedPlayers.Add(player.Data.FriendCode);
                    AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                    break;
            }
        }

        public static string Name(PlayerControl p) => p?.Data?.PlayerName ?? "未知玩家";
    }
}
