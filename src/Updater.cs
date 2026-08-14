using System;
using UnityEngine;

namespace NitroShield
{
    public class Updater : MonoBehaviour
    {
        public Updater(IntPtr ptr) : base(ptr) { }

        private float _banTimer;
        private static bool _hostPromptShown = false;
        private bool _loggedSelf;

        private void Update()
        {
            LogSelfOnce();
            EnforceBans();
            MuteManager.CheckMajorityVotes();
        }

        // 在组件启动时立即检查是否为房主并弹出提示
        private void Start()
        {
            if (!_hostPromptShown && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                GameNotification.Show("您已加载反作弊系统，部分检测可能不准确，请仔细辨别");
                _hostPromptShown = true;
            }
        }

        private void LogSelfOnce()
        {
            if (_loggedSelf) return;
            var me = PlayerControl.LocalPlayer;
            if (me == null || me.Data == null) return;
            _loggedSelf = true;
            NitroShieldPlugin.Log.LogInfo(Strings.LogYourIdentifiers(me.Data.FriendCode ?? "", me.Data.PlayerName ?? ""));
        }

        private void EnforceBans()
        {
            if (!BannedPlayers.Enabled) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            _banTimer += Time.deltaTime;
            if (_banTimer < 2f) return;
            _banTimer = 0f;

            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p == null || Anticheat.IsExempt(p)) continue;
                if (BannedPlayers.IsBanned(p))
                {
                    GameNotification.Show(Strings.ViolationBannedList(Anticheat.Name(p)));
                    AmongUsClient.Instance.KickPlayer(p.OwnerId, true);
                }
            }
        }
    }
}
