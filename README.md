# SP AntiCheat (System Reminder)

一个面向 Among Us 房主的**服务端侧反作弊模组**。基于 BepInEx 6 + Harmony，在房主侧对全房间的 RPC 进行语义级校验，拦截外挂行为并给出可视化提示。

> 注意：本模组仅在房主侧（Host）执行拦截与惩罚，作为房间成员（非房主）时仅做检测提示，不丢弃任何 RPC。

---

## 功能清单

### 击杀检测
- **CheckMurder / MurderPlayer**：校验击杀者与被击杀者状态（存活、角色、是否在通风口/梯子），拦截非法击杀
- 躲猫猫（Hide & Seek）模式豁免角色判定，避免误判

### 通风口检测
- **EnterVent / ExitVent**：检测非法进出通风口（角色不能通风、ventId 越界）
- **BootFromVent**：检测非房主发送强制踢出通风口 RPC（通风口踢人漏洞）
- **VentilationGuard**：通风管道传送检测系统
  - seqId 异常检测（Hydra 外挂 seqId 从 10000 起步，正常远低于 2000）
  - 非房主发送 BootImpostors 伪造检测
  - ventId 越界检测（可配置开关）
  - 角色权限检测（可配置开关）
  - 状态（inVent）检测（可配置开关）
  - 弱嫌疑人推断（仅提示，不惩罚）

### 破坏检测
- **UpdateSystem**：校验所有破坏系统更新
  - 电力（Electrical）：批量开关检测、越界开关检测
  - 通讯（Comms）、反应堆（Reactor）、氧气（LifeSupp）、直升机（HeliSabotage）、蘑菇（MushroomMixupSabotage）
  - 破坏面板（Sabotage）：无效破坏目标检测
  - 非内鬼激活破坏拦截
  - 会议期间破坏拦截
  - 捉迷藏模式破坏拦截
- **CloseDoorsOfType**：捉迷藏模式关门拦截

### 聊天检测
- **SendChat**：聊天频率检测（洪水攻击拦截）、昵称发送拦截、快速聊天内容检测
- **ChatAcePatch**：'/ace' 命令本地切换反作弊开关

### 名字检测
- **CheckName / SetName**：名字长度、非法字符、空名字检测
- **NameFilter**：名字过滤系统（默认禁用）

### 会议检测
- **ReportDeadBody**：捉迷藏模式尸体报告拦截、早会拦截
- **MeetingTimer**：游戏开始后前 N 秒内的会议拦截（紧急会议/尸体报告）
- **AddVote**：大厅投票踢人检测（已断线玩家投票放行，死亡玩家投票拦截）

### 移动检测
- **SnapTo**：瞬移检测（距离、频率、坐标合法性）
- **ClimbLadder**：梯子使用检测（无飞船状态、死亡后使用）
- **UsePlatform**：传送台使用检测（错误地图、无飞船状态、捉迷藏模式）

### 角色检测
- **Shapeshift**：变形检测（非变形者变形拦截）
- **StartVanish / StartAppear**：消失/出现检测（非幽灵角色拦截）
- **ProtectPlayer**：保护检测（非死亡守护天使拦截）

### 状态检测（StateChecks）
- 化妆品检测（SetColor、SetHat 等，游戏期间仅软提示）
- 大厅非法 RPC 检测（MurderPlayer、EnterVent、Shapeshift 等，大厅阶段拦截）

### 崩溃防护
- 畸形 RPC 检测（RPC 数据长度不足最小值）
- 洪水攻击检测（1 秒内超过 50 个 RPC 自动拦截）
- 未知 RPC 检测（非官方 RPC ID 拦截）
- 超大消息防护（InnerNetClient.HandleGameData >1400 字节丢弃）
- VotingComplete 过载攻击防护（超大数据分配拦截）
- 加固整数反序列化器（防溢出攻击）

### 作弊客户端检测（CheatClients）
- SickoMenu (RPC 164)
- AmongUsMenu / AUM (RPC 85)
- KillNetwork (RPC 250)
- 首次检测到自动标记，同一玩家不再重复提示

### 封禁系统
- 自动封禁：检测到违规行为时自动踢出/封禁玩家
- 好友码封禁：基于 FriendCode 的封禁名单（支持持久化存储）
- 自动封禁加入者：被封禁的玩家再次加入时自动踢出
- 大厅踢人（VoteKick）防护：防房主被投票踢出

### 信任玩家系统
- 豁免指定玩家（Trusted Players）的所有反作弊检测

### 静音管理
- 多数投票静音（MuteOnMajorityVote）
- 聊天共识静音（MuteOnChatConsensus）

### 通知系统
- 左下角 OnGUI 浮动通知覆盖层（始终在最上层）
- 圆角卡片设计，半透明背景，红色描边
- 通知自动淡出（配置透明度 0.4 防遮挡）
- 支持富文本颜色

### 配置系统
- 配置文件路径：`BepInEx/config/com.well.nitroanticheat.cfg`
- 支持房主侧配置项（General、Crash、State、Meeting、Mute、Protections、Ventilation、VoteKick、Rpc 等分区）
- 每个检测模块均可独立开关

---

## 安装

1. 安装 [BepInEx 6](https://github.com/BepInEx/BepInEx/releases)（IL2CPP 版本）
2. 将 `SPAntiCheat.dll` 放入 `BepInEx/plugins/`
3. （可选）编辑 `BepInEx/config/com.well.nitroanticheat.cfg` 调整配置

## 兼容性

- Among Us 版本：2026.6.5+
- BepInEx 版本：6.0.0-be.735+
- 仅房主侧生效，房间成员安装后仅做检测提示

## 构建

```bash
dotnet build -c Release
```

产物：`bin/Release/net6.0/SPAntiCheat.dll`

## 技术说明

- 所有检测基于 Harmony Prefix/Postfix 钩子
- RPC 检测：Hook `PlayerControl.HandleRpc`、`PlayerPhysics.HandleRpc`、`CustomNetworkTransform.HandleRpc`、`ShipStatus.HandleRpc`
- 游戏数据处理：Hook `InnerNetClient.HandleGameData`
- 通风口系统：Hook `VentilationSystem.PerformVentOp`
- 通知系统：独立 OnGUI 覆盖层，不受场景加载影响
- 配置：逐行解析 INI 风格配置文件，非 BepInEx 原生 ConfigEntry
