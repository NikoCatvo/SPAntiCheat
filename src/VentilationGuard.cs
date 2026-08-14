using Hazel;
using InnerNet;
using System.Collections.Generic;

namespace NitroShield
{
    /// <summary>
    /// 通风管道（Ventilation）系统守卫。
    ///
    /// 目标：防御"房间成员使用外挂进行通风管道传送"。
    ///
    /// 已知攻击方式（参考 Hydra 源码 Teleporter.TeleportToVent / Utilities.KickPlayer）：
    ///   1. 伪造 ShipStatus.UpdateSystem(Ventilation) 报文，payload 布局：
    ///        [byte systemType] [netobject player] [ushort seqId] [byte op] [byte ventId]
    ///      - op = Enter(2)：让房主以为玩家"进入了通风口"
    ///      - op = BootImpostors(5)：让房主把玩家"踢出"到目标通风口 → 传送
    ///      - 用超大 seqId（从 10000 起步）压制正常序列号
    ///   2. 直接发送 BootFromVent RPC（房主专属操作，普通玩家发送即违规/被踢）
    ///   3. 无效 ventId / 无通风权限角色；
    ///
    /// ⚠️ 攻击归属（重要）：
    ///   外挂可以"传送别人"——攻击者伪造报文时，报文内 netobject player 写的是
    ///   【受害者】（被传送的人），攻击者本人从不出现在报文里。
    ///   而 Hazel 协议层（MessageReader 无发送者字段；服务器中继模式下
    ///   HandleGameData 也拿不到 fromClientId）无法定位真实发送者。
    ///   因此：本守卫把 netobject 玩家一律视为"被操作者/受害者"，
    ///   【绝不因通风口异常惩罚 netobject 玩家】。防御只做两件事：
    ///     a) 拦截伪造报文 / 阻断 PerformVentOp → 传送不生效（主动防御）；
    ///     b) 匿名警告"检测到通风管道传送攻击"，房主自行判断。
    ///   嫌疑人推断（FindVentSuspicion）仅用于弱提示，绝不作为惩罚依据。
    ///
    /// 检测维度（均为不依赖坐标的语义检测，避免网络延迟误判）：
    ///   - 角色：存活且不可通风（非内鬼/工程师）的玩家执行 Enter/Move → 远程强传攻击
    ///   - 状态：不在通风口内却执行 Move（凭空瞬移）
    ///   - 序列号：seqId 远高于正常递增范围（Hydra 特征）
    ///   - 操作权限：BootImpostors 只能由房主执行
    ///   - ventId：必须落在当前地图 AllVents 范围内
    /// </summary>
    internal static class VentilationGuard
    {
        public static bool Enabled          = true;   // 总开关
        public static bool CheckVentTp      = true;   // 通风管道传送检测
        public static bool CheckRole        = true;   // 角色权限检测
        public static bool CheckSeq         = true;   // seqId 异常检测
        public static bool CheckBootOps     = true;   // 非房主发送 BootImpostors 检测
        public static bool CheckMoveState   = true;   // 未在通风口内却移动（状态检测，非坐标）
        public static ushort MaxSeqId       = 2000;   // 正常游戏内 vent 操作序列号远小于此值（Hydra 从 10000 起步）

        // ---- 疑似攻击者弱推断 ----
        // 协议层无法获取 RPC 真实发送者（服务器广播），仅能拿到 netobject（被操作者）。
        // 因此这里只记录"合法使用通风口的玩家"（角色合法、状态合法），
        // 当发生远程强传时，若近期恰好有人在使用通风口，将其列为"疑似"做弱提示。
        // 注意：这是启发式推断，可能命中正常玩家，所以永远不用于惩罚，
        // 仅用于给房主提供排查线索（shouldPunish=false）。
        private static readonly Dictionary<int, float> _ventOpHistory = new(); // ownerId -> 最近合法通风口操作时间
        private const float SuspicionWindow = 4f; // 秒：窗口期内多次异常则指向同一个人

        private static void MarkVentOp(int ownerId)
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (!_ventOpHistory.ContainsKey(ownerId)) _ventOpHistory[ownerId] = 0f;
            _ventOpHistory[ownerId] = now;
        }

        /// <summary>返回近期（SuspicionWindow 内）有通风口相关异常操作的可疑玩家 OwnerId（-1 表示无）。</summary>
        private static int FindVentSuspicion()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            int best = -1;
            float bestTime = 0f;
            foreach (var kv in _ventOpHistory)
            {
                if (now - kv.Value > SuspicionWindow) continue;
                if (kv.Value > bestTime) { bestTime = kv.Value; best = kv.Key; }
            }
            return best;
        }

        internal static void ResetVentOpHistory() => _ventOpHistory.Clear();

        // VentilationSystem.Operation 枚举（按声明顺序，2026 版本）
        private const byte OpStartCleaning  = 0;
        private const byte OpStopCleaning   = 1;
        private const byte OpEnter          = 2;
        private const byte OpExit           = 3;
        private const byte OpMove           = 4;
        private const byte OpBootImpostors  = 5;

        private static bool AmHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

        private static bool IsSelf(PlayerControl p)
            => p != null && (p.AmOwner || p == PlayerControl.LocalPlayer);

        /// <summary>角色是否允许使用通风口（存活的内鬼/工程师；幽灵（死亡）放行）。</summary>
        private static bool CanUseVentLegit(PlayerControl p)
        {
            if (p == null || p.Data == null) return false;
            if (p.Data.IsDead) return true;          // 幽灵可在通风口间传送（原版功能）
            if (p.Data.Role == null) return false;
            return p.Data.Role.CanVent;              // 内鬼/工程师 = true
        }

        /// <summary>按 OwnerId 查找玩家。</summary>
        private static PlayerControl FindPlayer(int ownerId)
        {
            if (PlayerControl.AllPlayerControls == null) return null;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p != null && p.OwnerId == ownerId) return p;
            }
            return null;
        }

        /// <summary>
        /// 检查 ShipStatus.UpdateSystem(Ventilation) 报文是否作弊。
        /// 传入 reader 位置应位于 payload 起点（system type 字节）。
        /// 返回 true 表示应当拦截该 RPC。
        /// </summary>
        public static bool CheckVentilationUpdate(System.Type netObj, byte callId, MessageReader reader)
        {
            if (!Enabled || !CheckVentTp) return false;
            if (netObj != typeof(ShipStatus)) return false;
            if (callId != (byte)RpcCalls.UpdateSystem) return false;

            int startPos = reader.Position;
            try
            {
                if (reader.BytesRemaining < 1) { reader.Position = startPos; return false; }

                // 1. 系统类型
                byte sysByte = reader.ReadByte();
                if (sysByte != (byte)SystemTypes.Ventilation)
                {
                    reader.Position = startPos;
                    return false;
                }

                // 2. netobject：被操作的玩家（Hydra 会把被传送/被踢的玩家写在这里）
                PlayerControl target = null;
                try { target = reader.ReadNetObject<PlayerControl>(); }
                catch { target = null; }

                // 剩余数据不足（seqId+op+ventId = 4 字节）→ 交给原版容错处理，不拦
                if (reader.BytesRemaining < 4)
                {
                    reader.Position = startPos;
                    return false;
                }

                // 3. 序列号 + 操作 + 通风口 id
                ushort seqId = reader.ReadUInt16();
                byte op = reader.ReadByte();
                byte ventId = reader.ReadByte();

                // 房主侧：拦截作弊；非房主侧：房主合法广播放行，仅对明确恶意操作警告
                bool isHost = AmHost;

                // 格式化操作名
                string opName = op switch
                {
                    OpStartCleaning => "StartCleaning",
                    OpStopCleaning  => "StopCleaning",
                    OpEnter         => "Enter",
                    OpExit          => "Exit",
                    OpMove          => "Move",
                    OpBootImpostors => "BootImpostors",
                    _               => $"Op{op}"
                };

                // ---- A. seqId 异常（Hydra 从 10000 起步，正常从 0 小范围递增）----
                // 注意：netobject player 是被操作者（可能被外挂强传的受害者），
                // 不能据此惩罚；seqId 异常只说明"报文是伪造的"，匿名警告即可。
                if (CheckSeq && seqId >= MaxSeqId)
                {
                    Anticheat.Flag(Strings.ViolationVentAttackSeq(seqId));
                    reader.Position = startPos;
                    return isHost; // 房主直接拦掉伪造 seqId 的报文
                }

                // ---- B. 非房主发送 BootImpostors（强制踢出/传送，Host-only 操作）----
                // netobject player 是被强传的受害者，绝不惩罚受害者；
                // 拦截报文（防御生效）+ 匿名警告。
                if (CheckBootOps && op == OpBootImpostors)
                {
                    // 房主自己不会通过网络向自己发 BootImpostors 更新（本地直接处理）；
                    // 网络上收到 BootImpostors 操作 = 作弊者伪造
                    if (isHost)
                    {
                        Anticheat.Flag(Strings.ViolationVentAttackBoot());
                        reader.Position = startPos;
                        return true;
                    }
                }

                // ---- C. ventId 越界（Hydra 用 CUSTOM_VENT_ID=50 之类非法 id）----
                // netobject player 同样可能是受害者，只匿名警告 + 拦截。
                if (ShipStatus.Instance != null)
                {
                    var vents = ShipStatus.Instance.AllVents;
                    if (vents != null && ventId >= vents.Length)
                    {
                        if (isHost)
                        {
                            Anticheat.Flag(Strings.ViolationVentAttackBadId(ventId));
                            reader.Position = startPos;
                            return true;
                        }
                    }
                }

                // ---- D. Enter/Move：远程强传核心检测（不依赖坐标）----
                // 语义：存活且不可通风（船员）的角色"进入/移动通风口"在原版中
                // 不可能发生（船员无法进通风口），出现该操作必然是外挂把目标
                // 强行塞进通风口（netobject = 受害者）。因此：
                //   - 绝不惩罚 netobject 玩家（他是被传送的受害者）；
                //   - 拦截报文 + 匿名/面向受害者的警告（shouldPunish=false）。
                if (op == OpEnter || op == OpMove)
                {
                    bool roleOk = CanUseVentLegit(target);

                    if (CheckRole && !roleOk && isHost)
                    {
                        if (target != null && !target.Data.IsDead)
                        {
                            // 尽力推断疑似外挂（近期合法操作过通风口的玩家），仅提示不惩罚
                            int sus = FindVentSuspicion();
                            if (sus >= 0 && (target == null || target.OwnerId != sus))
                            {
                                var suspect = FindPlayer(sus);
                                if (suspect != null)
                                    Anticheat.Flag(target, Strings.ViolationVentForceSuspect(
                                        Anticheat.Name(target), opName, Anticheat.Name(suspect)), false);
                                else
                                    Anticheat.Flag(target, Strings.ViolationVentForceTarget(Anticheat.Name(target), opName), false);
                            }
                            else
                            {
                                Anticheat.Flag(target, Strings.ViolationVentForceTarget(Anticheat.Name(target), opName), false);
                            }
                        }
                        else
                        {
                            Anticheat.Flag(target, Strings.ViolationVentForceTarget(
                                target != null ? Anticheat.Name(target) : "未知玩家", opName), false);
                        }
                        reader.Position = startPos;
                        return true;
                    }

                    // Move：玩家必须在通风口内（inVent/walkingToVent）才能移动，
                    // 否则是凭空瞬移。基于游戏状态（非坐标），不受延迟影响。
                    // 注意：被外挂强传的受害者同样"不在通风口却被 Move"，
                    // 因此只拦截 + 警告，不惩罚 netobject 玩家。
                    if (op == OpMove && CheckMoveState && roleOk && !IsSelf(target) && isHost
                        && target != null && !target.Data.IsDead
                        && !target.inVent && !target.walkingToVent)
                    {
                        Anticheat.Flag(target, Strings.ViolationVentMoveNoVent(Anticheat.Name(target)), false);
                        reader.Position = startPos;
                        return true;
                    }
                }

                // 仅记录"合法"的通风口操作者，用于后续强传攻击的弱推断
                // （绝不在强传/异常时记录受害者，避免把受害者当成嫌疑人）
                if (target != null && isHost && op is OpEnter or OpMove or OpExit
                    && CanUseVentLegit(target) && !target.Data.IsDead)
                {
                    MarkVentOp(target.OwnerId);
                }

                reader.Position = startPos;
                return false;
            }
            catch
            {
                reader.Position = startPos;
                return false;
            }
        }

        // ==================================================================
        // 语义层守卫：直接 hook VentilationSystem.PerformVentOp。
        // 游戏解析完报文后，最终会在这里执行"玩家进入/移动/踢出通风口"。
        // 在语义层拦截可以做到零字节布局误判，且能保证外挂一切伪造
        // 通风操作的最终失效（PerformVentOp 被阻断 → 传送不生效）。
        // 仅房主视角生效；自身与 Trusted 玩家放行。
        // ==================================================================

        public static bool SemanticCheck(byte playerId, byte op, byte ventId)
        {
            if (!Enabled || !CheckVentTp) return true;          // true = 放行
            if (!AmHost) return true;                            // 非房主不拦截（房主权威）

            // 由 playerId 定位玩家
            PlayerControl pc = null;
            if (PlayerControl.AllPlayerControls != null)
            {
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p != null && p.PlayerId == playerId) { pc = p; break; }
                }
            }
            if (pc == null) return true;
            if (pc.AmOwner || pc == PlayerControl.LocalPlayer) return true; // 自己
            if (pc.Data == null) return true;
            if (Anticheat.IsExempt(pc)) return true;             // Trusted

            // ---- ventId 越界（Hydra CUSTOM_VENT_ID=50 等非法 id）----
            // netobject 玩家可能只是被强传的受害者，只警告不惩罚
            if (ShipStatus.Instance != null && ShipStatus.Instance.AllVents != null
                && ventId >= ShipStatus.Instance.AllVents.Length)
            {
                Anticheat.Flag(pc, Strings.ViolationVentBadId(Anticheat.Name(pc), ventId), false);
                return false;
            }

switch (op)
            {
                case OpEnter:
                {
                    // 存活且角色不可通风 → 他人远程强制传送（netobject = 受害者）。
                    // 阻断传送（return false）+ 仅警告，绝不惩罚受害者。
                    if (CheckRole && !pc.Data.IsDead && (pc.Data.Role == null || !pc.Data.Role.CanVent))
                    {
                        int sus = FindVentSuspicion();
                        if (sus >= 0 && sus != pc.OwnerId)
                        {
                            var suspect = FindPlayer(sus);
                            if (suspect != null)
                                Anticheat.Flag(pc, Strings.ViolationVentForceSuspect(
                                    Anticheat.Name(pc), OpName(op), Anticheat.Name(suspect)), false);
                            else
                                Anticheat.Flag(pc, Strings.ViolationVentForceTarget(Anticheat.Name(pc), OpName(op)), false);
                        }
                        else
                        {
                            Anticheat.Flag(pc, Strings.ViolationVentForceTarget(Anticheat.Name(pc), OpName(op)), false);
                        }
                        return false;
                    }
                    break;
                }

                case OpMove:
                {
                    // 存活且角色不可通风 → 他人远程强制通风口间移动（传送），仅警告不惩罚
                    if (CheckRole && !pc.Data.IsDead && (pc.Data.Role == null || !pc.Data.Role.CanVent))
                    {
                        int sus = FindVentSuspicion();
                        if (sus >= 0 && sus != pc.OwnerId)
                        {
                            var suspect = FindPlayer(sus);
                            if (suspect != null)
                                Anticheat.Flag(pc, Strings.ViolationVentForceSuspect(
                                    Anticheat.Name(pc), OpName(op), Anticheat.Name(suspect)), false);
                            else
                                Anticheat.Flag(pc, Strings.ViolationVentForceTarget(Anticheat.Name(pc), OpName(op)), false);
                        }
                        else
                        {
                            Anticheat.Flag(pc, Strings.ViolationVentForceTarget(Anticheat.Name(pc), OpName(op)), false);
                        }
                        return false;
                    }

                    // 在通风口间移动的前提是玩家当前已在通风口内（原版：inVent=true 才允许 Move）。
                    // 否则就是凭空瞬移（Hydra 传送 / 强传受害者）。基于状态而非坐标，不受延迟影响。
                    // 被强传的受害者同样满足"不在通风口却被 Move"，因此仅警告不惩罚。
                    if (CheckMoveState && !pc.Data.IsDead && !pc.inVent && !pc.walkingToVent)
                    {
                        Anticheat.Flag(pc, Strings.ViolationVentMoveNoVent(Anticheat.Name(pc)), false);
                        return false;
                    }
                    break;
                }

                case OpExit:
                {
                    // 退出通风口本身无碍，幽灵/内鬼/工程师均可，不做拦截
                    break;
                }

                case OpStartCleaning:
                case OpStopCleaning:
                {
                    // 清洁通风口是房主/幽灵操作，玩家发起视为异常但仅记录
                    Anticheat.Flag(pc, Strings.ViolationVentClean(Anticheat.Name(pc), OpName(op)), false);
                    break;
                }

                case OpBootImpostors:
                {
                    // 只有房主能发起 BootImpostors（把内鬼踢出通风口）。
                    // 玩家伪造 = 强制传送/踢人漏洞；报文 netobject 是被强传的受害者，
                    // 所以阻断 + 匿名警告，不惩罚受害者。
                    Anticheat.Flag(Strings.ViolationVentAttackBoot());
                    return false;
                }
            }
            return true;
        }

        internal static string OpName(byte op) => op switch
        {
            OpStartCleaning => "StartCleaning",
            OpStopCleaning  => "StopCleaning",
            OpEnter         => "Enter",
            OpExit          => "Exit",
            OpMove          => "Move",
            OpBootImpostors => "BootImpostors",
            _               => $"Op{op}"
        };

        [HarmonyLib.HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.PerformVentOp))]
        internal static class PerformVentOpGuard
        {
            private static bool Prefix(byte playerId, VentilationSystem.Operation op, byte ventId)
            {
                // 返回 false 表示拦截，阻止该通风操作生效
                return SemanticCheck(playerId, (byte)op, ventId);
            }
        }
    }
}