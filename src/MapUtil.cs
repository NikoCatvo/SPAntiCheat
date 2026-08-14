using System.Collections.Generic;

namespace NitroShield
{
    internal static class MapUtil
    {
        public static MapNames GetCurrentMap()
        {
            try
            {
                if (ShipStatus.Instance == null)
                {
                    if (AmongUsClient.Instance != null &&
                        AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
                        return (MapNames)AmongUsClient.Instance.TutorialMapId;
                    return (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId;
                }
                return (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId;
            }
            catch { return MapNames.Skeld; }
        }

        // 所有可能存在的破坏类型
        private static readonly SystemTypes[] AllSabotageTypes =
        {
            SystemTypes.Reactor, SystemTypes.Laboratory, SystemTypes.HeliSabotage,
            SystemTypes.LifeSupp, SystemTypes.Comms, SystemTypes.Electrical,
            SystemTypes.MushroomMixupSabotage
        };

        public static bool IsValidSabotageTarget(SystemTypes target)
        {
            // 优先检查当前地图是否拥有该系统
            if (ShipStatus.Instance != null)
            {
                if (ShipStatus.Instance.Systems.ContainsKey(target))
                    return true;
            }

            // 回退：检查是否在已知破坏类型列表中
            foreach (var t in AllSabotageTypes)
                if (t == target) return true;

            return false;
        }
    }
}
