using System;
using AmongUs.GameOptions;
using InnerNet;

namespace NitroShield
{
    internal static class Strings
    {
        public static string NotifyBlockedTeleport => "已阻止针对你的强制传送。";
        public static string NotifyBlockedVentKick => "已阻止通风口踢出/传送漏洞。";
        public static string NotifyBlockedVotingOverload => "已阻止投票过载崩溃尝试。";
        public static string NotifyBannedListSaved => "封禁名单已保存（共 {0} 条）。";
        public static string NotifyLoadedBlocklist => "已加载 {0} 条黑名单术语。";

        public static string ViolationMalformedRpc(string name, string rpc)
            => $"{name} 发送了畸形 {rpc} 指令（崩溃尝试）。";
        public static string ViolationFlood(string name, int count, float window)
            => $"{name} 正在洪水发送指令（{count}次/{window:0.#}秒）- 崩溃尝试。";
        public static string ViolationUnknownRpc(string name, byte callId)
            => $"{name} 发送了未注册的指令（{callId}）- 可能是作弊/崩溃。";
        public static string ViolationHostOnlyRpc(string name, string rpc)
            => $"{name} 以非主机身份发送了仅主机可用的指令 {rpc}。";
        public static string ViolationCheatClient(string name, string client)
            => $"{name} 正在运行 {client}（已检测到作弊客户端）。";
        public static string ViolationCosmetics(string name, string rpc)
            => $"{name} 在游戏中更改了外观（{rpc}）。";
        public static string ViolationLobbyRpc(string name, string rpc)
            => $"{name} 在大厅中发送了游戏内指令 {rpc}。";
        public static string ViolationChatOversized(string name, int len)
            => $"{name} 发送了超大聊天消息（{len}字符）- 崩溃尝试。";
        public static string ViolationChatSpam(string name, int count, float window)
            => $"{name} 正在刷屏聊天（{count}条消息/{window:0.#}秒）。";
        public static string ViolationTaskNoShip(string name, uint taskIndex)
            => $"{name} 在没有飞船状态的情况下完成了任务 {taskIndex}。";
        public static string ViolationTaskAsImpostor(string name, uint taskIndex)
            => $"{name} 以内鬼身份完成了任务 {taskIndex}。";
        public static string ViolationTaskCount(string name, uint taskIndex, int count)
            => $"{name} 完成了任务 {taskIndex}，但只有 {count} 个任务。";
        public static string ViolationTaskBurst(string name)
            => $"{name} 在0.1秒内瞬间完成了多个任务 - 极速完成外挂。";
        public static string ViolationVentNoShip(string name)
            => $"{name} 在没有飞船状态的情况下进入通风口。";
        public static string ViolationVentNoRole(string name, string roleType)
            => $"{name} 进入了通风口，但角色（{roleType}）无法使用通风口。";
        public static string ViolationExitVentNoShip(string name)
            => $"{name} 在没有飞船状态的情况下离开通风口。";
        public static string ViolationExitVentNoRole(string name, string roleType)
            => $"{name} 离开了通风口，但角色（{roleType}）无法使用通风口。";
        public static string ViolationClimbLadderNoShip(string name)
            => $"{name} 在没有飞船状态的情况下爬梯子。";
        public static string ViolationClimbLadderDead(string name)
            => $"{name} 在死亡状态下爬梯子。";
        public static string ViolationSnapToLobby(string name)
            => $"{name} 在大厅中使用了传送。";
        public static string ViolationInvalidKill(string name, string targetName)
            => $"{name} 对 {targetName} 发送了无效击杀。";
        public static string ViolationIllegalMurder(string name)
            => $"{name} 执行了非法的击杀。";
        public static string ViolationShapeshift(string name)
            => $"{name} 在不是变形者身份的情况下变形。";
        public static string ViolationVanish(string name)
            => $"{name} 在非活体幽灵状态下消失。";
        public static string ViolationProtect(string name)
            => $"{name} 在非守护天使状态下保护玩家。";
        public static string ViolationSystemNotFound(string name, string system)
            => $"{name} 更新了该地图不存在的系统 {system}。";
        public static string ViolationSystemDead(string name, string system)
            => $"{name} 死亡时更新了系统 {system}。";
        public static string ViolationMushroom(string name)
            => $"{name} 试图强制触发蘑菇混合混乱破坏。";
        public static string ViolationReactorForceFix(string name)
            => $"{name} 试图强制修复反应堆破坏。";
        public static string ViolationReactorForceCall(string name)
            => $"{name} 试图强制呼叫反应堆破坏。";
        public static string ViolationInvalidSabotageTarget(string name, string target)
            => $"{name} 试图破坏无效系统：{target}。";
        public static string ViolationSabotageNotImpostor(string name, string target)
            => $"{name} 不是内鬼却试图破坏 {target}。";
        public static string ViolationSabotageHideSeek(string name, string target)
            => $"{name} 在捉迷藏模式中试图破坏 {target}。";
        public static string ViolationCommsSabotage(string name)
            => $"{name} 试图破坏通讯系统（Comms）。";
        public static string ViolationOxygenSabotage(string name)
            => $"{name} 试图破坏氧气系统（LifeSupp）。";
        public static string ViolationSabotageDuringMeeting(string name, string target)
            => $"{name} 在会议期间试图破坏 {target}。";
        public static string ViolationSwitchCrash(string name, byte switches)
            => $"{name} 发送了批量电力开关数据（0x{switches:X2}）- 疑似灯光破坏外挂，已拦截。";
        public static string ViolationInvalidSwitch(string name, byte switches)
            => $"{name} 切换了无效开关（{switches}）。";
        public static string ViolationTaskAnimLobby(string name)
            => $"{name} 在大厅中播放了任务动画。";
        public static string ViolationTaskAnimImpostor(string name)
            => $"{name} 以内鬼身份播放了任务动画。";
        public static string ViolationTaskAnimNoVisual(string name)
            => $"{name} 在关闭视觉任务时播放了任务动画。";
        public static string ViolationExiled(string name)
            => $"{name} 发送了无效的流放指令。";
        public static string ViolationSetColorNetId(string name, uint netId)
            => $"{name} 的颜色设置具有错误的网络ID（应为{netId}）。";
        public static string ViolationSetColorColor(string name, byte color)
            => $"{name} 的颜色设置使用了无效颜色（{color}）。";
        public static string ViolationScannerNoMap(string name)
            => $"{name} 在地图生成前进行了医学扫描。";
        public static string ViolationScannerImpostor(string name)
            => $"{name} 以内鬼身份进行了医学扫描。";
        public static string ViolationScannerNoTask(string name)
            => $"{name} 没有医学扫描任务却进行了扫描。";
        public static string ViolationNonHostKick(string name)
            => $"{name} 不是房主却试图踢出/封禁玩家。";
        public static string ViolationNonHostKickExploit(string name, string op)
            => $"{name} 利用通风口踢人漏洞（{op}），已被阻止。";
        // ---- 通风管道传送攻击（匿名警告：外挂传别人时 netobject 是被强传的受害者，无法定位发送者）----
        public static string ViolationVentAttackSeq(ushort seqId)
            => $"检测到伪造的通风口操作（序列号异常 {seqId}）- 疑似通风管道传送外挂，已拦截。";
        public static string ViolationVentAttackBoot()
            => "检测到通风口强制踢出/传送攻击（BootImpostors 伪造）- 已拦截。";
        public static string ViolationVentAttackBadId(byte ventId)
            => $"检测到非法通风口编号（{ventId}）的伪造报文 - 已拦截。";
        // 受害者视角：netobject 玩家是被强传的人，绝不因此惩罚他
        public static string ViolationVentForceTarget(string name, string op)
            => $"{name} 被远程强制执行了 {op} - 检测到通风管道传送外挂（{name} 可能是受害者），已拦截。";
        public static string ViolationVentForceSuspect(string name, string op, string suspect)
            => $"{name} 被远程强制执行了 {op} - 疑似 {suspect} 的通风管道传送外挂（已拦截，请房主核实）。";
        // 仅用于玩家亲自发送的 EnterVent/ExitVent（netobject 即发送者本人）等场景
        public static string ViolationVentBoot(string name)
            => $"{name} 伪造了通风口强制踢出操作（BootImpostors）- 通风管道传送。";
        public static string ViolationVentBadId(string name, byte ventId)
            => $"{name} 使用了非法通风口编号（{ventId}）- 伪造通风传送。";
        public static string ViolationVentClean(string name, string op)
            => $"{name} 非法执行了通风口清洁操作（{op}）。";
        public static string ViolationVentMoveNoVent(string name)
            => $"{name} 被强制执行了通风口间移动（不在任何通风口内）- 检测到传送攻击，已拦截。";
        public static string ViolationPlatformWrongMap(string name)
            => $"{name} 在错误的地图上使用了传送台。";
        public static string ViolationPlatformNoMap(string name)
            => $"{name} 在没有地图的情况下使用了传送台。";
        public static string ViolationPlatformHideSeek(string name)
            => $"{name} 在捉迷藏模式中使用了传送台。";
        public static string ViolationLevelTooHigh(string name, uint level)
            => $"{name} 发送了不可能的高等级（{level}）。";
        public static string ViolationLevelAfterStart(string name)
            => $"{name} 在游戏开始后发送了等级。";
        public static string ViolationUnknownClientVote(string id)
            => $"未知客户端（{id}）试图发起投票。";
        public static string ViolationDeadVote(string name)
            => $"{name} 在死亡状态下试图发起投票。";
        public static string ViolationOutsideMeetingVote(string name)
            => $"{name} 在会议外试图发起投票。";
        public static string ViolationHideSeekMeeting(string name)
            => $"{name} 在捉迷藏模式中试图发起会议。";
        public static string ViolationHideSeekCloseDoors
            => "有玩家在躲猫猫模式中关闭了门 - 内鬼破坏/作弊！";
        public static string ViolationEarlyMeeting(string name, string kind, float remaining, float grace)
            => $"{name} 过早发起{kind}（早了 {remaining:0.0}秒，宽限时间为 {grace:0}秒）。";
        public static string ViolationBlockedTerm(string name, string term)
            => $"'{name}' 包含被屏蔽的词（'{term}'）。";
        public static string ViolationNameTooLong(string name, int len)
            => $"'{name}' 过长（{len}字符）。";
        public static string ViolationNameInvalidChars(string name)
            => $"'{name}' 包含非法格式字符。";
        public static string ViolationStartCounterSpoof(string name, sbyte counter)
            => $"{name} 篡改了开始倒计时（{counter}）。";
        public static string ViolationBannedList(string name)
            => $"{name} 在封禁名单上，已被踢出。";
        public static string BannedToBlacklist(string name)
            => $"已将 {name} 添加到黑名单，下次自动封禁。";
        public static string AutoBanOnJoin(string name)
            => $"{name} 在黑名单中，已自动封禁。";
        public static string MuteMajorityVotes(string name)
            => $"{name} 获得多数票 - 本次会议禁言。";
        public static string MuteChatConsensus(string name, int count)
            => $"{name} 被聊天投票禁言（{count}票）。";
        public static string LogYourIdentifiers(string friendCode, string playerName)
            => $"[Shield] 你的标识 -> 好友码：'{friendCode}'  名字：'{playerName}'";
        public static string LogFailedToSaveBannedList(string msg) => $"无法保存封禁名单：{msg}";
        public static string LogLoadBlocklist(int count) => $"[NitroShield] 已加载 {count} 条黑名单术语。";
        public static string ConfigHeaderBlocklist =>
            "# NitroShield 辱骂名字黑名单。" +
            "# 每行一个词。以 # 开头的行为注释。" +
            "# 匹配不区分大小写。" +
            "# 请使用纯小写字母输入每个词。";

        public static string ConfigHeaderBanned =>
            "# SP Anti Cheat 封禁玩家名单。" +
            "# 每行一个条目。" +
            "# 匹配不区分大小写。" +
            "# 房主手动封禁时好友码自动加入本文件。";

        public static string RoleName(RoleTypes role) => role switch
        {
            RoleTypes.Crewmate      => "船员",
            RoleTypes.Impostor      => "内鬼",
            RoleTypes.Engineer      => "工程师",
            RoleTypes.Scientist     => "科学家",
            RoleTypes.GuardianAngel => "守护天使",
            RoleTypes.Shapeshifter  => "变形者",
            RoleTypes.Phantom       => "幽灵",
            RoleTypes.Tracker       => "追踪者",
            _ => role.ToString()
        };

        public static string SystemName(SystemTypes sys) => sys switch
        {
            SystemTypes.Electrical    => "电力",
            SystemTypes.Reactor       => "反应堆",
            SystemTypes.Laboratory    => "实验室",
            SystemTypes.HeliSabotage  => "直升机",
            SystemTypes.LifeSupp      => "氧气",
            SystemTypes.Comms         => "通讯",
            SystemTypes.Sabotage      => "破坏面板",
            SystemTypes.MedBay        => "医务室",
            SystemTypes.Security      => "监控室",
            SystemTypes.Ventilation   => "通风系统",
            SystemTypes.MushroomMixupSabotage => "蘑菇交换",
            _ => sys.ToString()
        };

        public static string RpcName(RpcCalls rpc) => rpc switch
        {
            RpcCalls.SendChat         => "聊天",
            RpcCalls.CompleteTask     => "完成任务",
            RpcCalls.CheckName        => "检查名字",
            RpcCalls.SetName          => "设置名字",
            RpcCalls.ReportDeadBody   => "报告尸体",
            RpcCalls.SetStartCounter  => "设置开始倒计时",
            RpcCalls.EnterVent        => "进入通风口",
            RpcCalls.ExitVent         => "离开通风口",
            RpcCalls.SnapTo           => "传送",
            RpcCalls.ClimbLadder      => "爬梯子",
            RpcCalls.MurderPlayer     => "击杀玩家",
            RpcCalls.CheckMurder      => "检测击杀",
            RpcCalls.CheckColor       => "检查颜色",
            RpcCalls.SetColor         => "设置颜色",
            RpcCalls.SetHatStr        => "设置帽子",
            RpcCalls.SetSkinStr       => "设置皮肤",
            RpcCalls.SetVisorStr      => "设置护目镜",
            RpcCalls.SetPetStr        => "设置宠物",
            RpcCalls.SetNamePlateStr  => "设置名牌",
            RpcCalls.StartMeeting     => "发起会议",
            RpcCalls.CastVote         => "投票",
            RpcCalls.AddVote          => "投票",
            RpcCalls.VotingComplete   => "投票完成",
            RpcCalls.CloseMeeting     => "结束会议",
            RpcCalls.Exiled           => "流放",
            RpcCalls.Shapeshift       => "变形",
            RpcCalls.CheckShapeshift  => "检测变形",
            RpcCalls.RejectShapeshift => "拒绝变形",
            RpcCalls.ProtectPlayer    => "保护玩家",
            RpcCalls.CheckProtect     => "检测保护",
            RpcCalls.StartVanish      => "消失",
            RpcCalls.CheckVanish      => "检测消失",
            RpcCalls.StartAppear      => "出现",
            RpcCalls.CheckAppear      => "检测出现",
            RpcCalls.TriggerSpores    => "触发孢子",
            RpcCalls.CheckSpore       => "检测孢子",
            RpcCalls.UpdateSystem     => "更新系统",
            RpcCalls.CloseDoorsOfType => "关闭门",
            RpcCalls.PlayAnimation    => "播放动画",
            RpcCalls.SetLevel         => "设置等级",
            RpcCalls.SetScanner       => "设置扫描仪",
            RpcCalls.UsePlatform      => "使用传送台",
            RpcCalls.BootFromVent     => "从通风口弹出",
            RpcCalls.UseZipline       => "使用滑索",
            RpcCalls.CheckZipline     => "检测滑索",
            RpcCalls.SyncSettings     => "同步设置",
            RpcCalls.SetInfected      => "设置感染",
            RpcCalls.SetTasks         => "设置任务",
            RpcCalls.SendChatNote     => "发送聊天备注",
            _ => rpc.ToString()
        };
    }
}
