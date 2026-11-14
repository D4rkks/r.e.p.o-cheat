// Stub cho các phần không port (MidJoin, Loader.RunCoroutine) — chỉ API được dùng.
using System.Collections;
using UnityEngine;

namespace r.e.p.o_cheat
{
    internal static class MidJoin
    {
        public static void Tick() { }
        public static string GetDisplayName(PlayerAvatar avatar, string fallback = null)
        {
            if (avatar == null) return string.IsNullOrWhiteSpace(fallback) ? "User" : fallback;
            string nick = avatar.photonView != null && avatar.photonView.Owner != null
                ? avatar.photonView.Owner.NickName
                : fallback;
            return string.IsNullOrWhiteSpace(nick) ? (string.IsNullOrWhiteSpace(fallback) ? "User" : fallback) : nick.Trim();
        }
    }

    internal static class MxLoader
    {
        private class Runner : MonoBehaviour { }

        private static GameObject _runnerGo;

        public static void RunCoroutine(IEnumerator routine)
        {
            if (_runnerGo == null)
            {
                _runnerGo = new GameObject("mx_coroutine_runner");
                Object.DontDestroyOnLoad(_runnerGo);
                _runnerGo.AddComponent<Runner>();
                _runnerGo.hideFlags = HideFlags.HideAndDontSave;
            }
            _runnerGo.GetComponent<Runner>().StartCoroutine(routine);
        }
    }
}
