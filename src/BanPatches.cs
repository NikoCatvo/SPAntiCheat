using HarmonyLib;
using InnerNet;
using System.Collections.Generic;

namespace NitroShield
{
    [HarmonyPatch]
    internal static class BanPatches
    {
        private static readonly Dictionary<int, string> _capturedFriendCodes = new();

        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
        [HarmonyPrefix]
        private static bool CanBan_Prefix(ref bool __result)
        {
            __result = AmongUsClient.Instance.AmHost;
            return false;
        }

        // 房主踢人时捕获好友码加入封禁名单
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
        [HarmonyPrefix]
        private static bool KickPlayer_Prefix(ref int clientId, ref bool ban)
        {
            if (!ban || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return true;

            string friendCode = null;
            string playerName = null;

            if (_capturedFriendCodes.TryGetValue(clientId, out var cachedCode) && !string.IsNullOrWhiteSpace(cachedCode))
            {
                friendCode = cachedCode;
                _capturedFriendCodes.Remove(clientId);
            }

            if (string.IsNullOrWhiteSpace(friendCode))
            {
                try
                {
                    var client = AmongUsClient.Instance.GetRecentClient(clientId);
                    if (client != null)
                    {
                        friendCode = client.FriendCode;
                        playerName = client.PlayerName;
                        if (string.IsNullOrWhiteSpace(friendCode) && client.Character != null && client.Character.Data != null)
                        {
                            friendCode = client.Character.Data.FriendCode;
                            playerName = client.Character.Data.PlayerName;
                        }
                    }
                }
                catch { }

                if (string.IsNullOrWhiteSpace(friendCode))
                {
                    try
                    {
                        if (PlayerControl.AllPlayerControls != null)
                        {
                            foreach (var pc in PlayerControl.AllPlayerControls)
                            {
                                if (pc != null && pc.OwnerId == clientId && pc.Data != null)
                                {
                                    friendCode = pc.Data.FriendCode;
                                    playerName = pc.Data.PlayerName;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            NitroShieldPlugin.Log.LogInfo($"[KickPlayer] id={clientId}, code='{friendCode}', name='{playerName}'");

            if (string.IsNullOrWhiteSpace(friendCode))
            {
                NitroShieldPlugin.Log.LogWarning($"[KickPlayer] id={clientId} 好友码为空，无法封禁");
                return true;
            }

            bool alreadyBanned = BannedPlayers.IsBannedByCode(friendCode);
            if (!alreadyBanned)
            {
                BannedPlayers.Add(friendCode);
                GameNotification.Show($"{playerName ?? "未知玩家"}已加入封禁名单");
            }
            else
            {
                NitroShieldPlugin.Log.LogInfo($"[KickPlayer] {friendCode} 已在封禁名单中，未重复提示");
            }

            return true;
        }

        [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
        [HarmonyPostfix]
        private static void BanMenuSelect_Postfix(int clientId)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            string code = null;
            try
            {
                var client = AmongUsClient.Instance.GetRecentClient(clientId);
                if (client != null)
                {
                    code = client.FriendCode;
                    if (string.IsNullOrWhiteSpace(code) && client.Character != null && client.Character.Data != null)
                        code = client.Character.Data.FriendCode;
                }
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(code))
            {
                _capturedFriendCodes[clientId] = code;
                NitroShieldPlugin.Log.LogInfo($"[BanMenu.Select] 捕获 id={clientId}, code='{code}'");
            }
            else
            {
                NitroShieldPlugin.Log.LogWarning($"[BanMenu.Select] id={clientId} 好友码为空");
            }
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
        [HarmonyPostfix]
        private static void OnPlayerJoined_Postfix(AmongUsClient __instance, ClientData data)
        {
            if (!AmongUsClient.Instance.AmHost || data == null) return;
            if (string.IsNullOrEmpty(data.FriendCode)) return;
            if (BannedPlayers.IsBannedByCode(data.FriendCode))
            {
                GameNotification.Show(Strings.AutoBanOnJoin(data.PlayerName ?? "未知玩家"));
                AmongUsClient.Instance.KickPlayer(data.Id, true);
            }
        }
    }
}
