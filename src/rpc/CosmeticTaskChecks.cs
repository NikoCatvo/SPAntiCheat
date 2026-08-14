using Hazel;

namespace NitroShield.Rpc
{
    internal class PlayAnimation : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            reader.ReadByte();
            if (player?.Data == null) return;

            // 仅当明确在大厅（且未进入游戏）时标记
            bool inLobby = LobbyBehaviour.Instance != null && ShipStatus.Instance == null && GameManager.Instance == null;
            if (inLobby)
            {
                Anticheat.Flag(player, Strings.ViolationTaskAnimLobby(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            }
            if (RoleManager.IsImpostorRole(player.Data.RoleType))
            {
                Anticheat.Flag(player, Strings.ViolationTaskAnimImpostor(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            }
            if (GameManager.Instance != null && !GameManager.Instance.LogicOptions.GetVisualTasks())
            {
                Anticheat.Flag(player, Strings.ViolationTaskAnimNoVisual(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            }
        }
    }

    internal class Exiled : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            Anticheat.Flag(player, Strings.ViolationExiled(Anticheat.Name(player)));
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
        }
    }

    internal class SetColor : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            uint netId = reader.ReadUInt32();
            byte color = reader.ReadByte();
            if (player?.Data == null) return;

            if (netId != player.Data.NetId)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationSetColorNetId(Anticheat.Name(player), netId), false);
            }
            if (color >= Palette.ColorNames.Length)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                Anticheat.Flag(player, Strings.ViolationSetColorColor(Anticheat.Name(player), color), false);
            }
        }
    }

    internal class SetScanner : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            bool scanning = reader.ReadBoolean();
            if (player?.Data == null || !scanning) return;

            if (GameManager.Instance != null && !GameManager.Instance.LogicOptions.GetVisualTasks())
                return;

            if (ShipStatus.Instance == null)
            {
                Anticheat.Flag(player, Strings.ViolationScannerNoMap(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }

            if (RoleManager.IsImpostorRole(player.Data.RoleType))
            {
                Anticheat.Flag(player, Strings.ViolationScannerImpostor(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }

            if (player.Data.IsDead)
            {
                Anticheat.Flag(player, Strings.ViolationScannerNoTask(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }

            if (player.inVent)
            {
                Anticheat.Flag(player, Strings.ViolationScannerNoTask(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }

            bool hasTasks = player.Data.Tasks != null && player.Data.Tasks.Count > 0;
            if (!hasTasks) return;

            bool hasScanTask = false;
            foreach (NetworkedPlayerInfo.TaskInfo task in player.Data.Tasks)
            {
                if (task.TypeId == (byte)TaskTypes.SubmitScan) { hasScanTask = true; break; }
            }
            if (!hasScanTask)
            {
                Anticheat.Flag(player, Strings.ViolationScannerNoTask(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            }
        }
    }

        internal class UsePlatform : RpcCheck
    {
        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            if (player?.Data == null) return;

            if (MapUtil.GetCurrentMap() != MapNames.Airship)
            {
                Anticheat.Flag(player, Strings.ViolationPlatformWrongMap(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }
            if (ShipStatus.Instance == null)
            {
                Anticheat.Flag(player, Strings.ViolationPlatformNoMap(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
                return;
            }
            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
            {
                Anticheat.Flag(player, Strings.ViolationPlatformHideSeek(Anticheat.Name(player)));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            }
        }
    }

    internal class SetLevel : RpcCheck
    {
        private const uint MaxLevel = 10000;

        public override void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc)
        {
            uint level = reader.ReadPackedUInt32();
            if (player?.Data == null) return;

            if (level > MaxLevel)
            {
                Anticheat.Flag(player, Strings.ViolationLevelTooHigh(Anticheat.Name(player), level));
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) blockRpc = true;
            }
            // 原版客户端在游戏期间也会合法发送 SetLevel 同步表情/XP，不做标记
        }
    }
}
