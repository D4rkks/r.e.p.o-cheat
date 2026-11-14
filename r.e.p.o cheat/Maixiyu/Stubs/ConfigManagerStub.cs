// Stub ConfigManager (Mai-xiyu): PlayerPrefs-backed, chỉ những member được dùng.
using UnityEngine;

namespace r.e.p.o_cheat
{
    internal static class ConfigManager
    {
        public static bool NoWeaponCooldownEnabled = false;

        public static void SaveToggle(string key, bool value) => PlayerPrefs.SetInt("mx_" + key, value ? 1 : 0);
        public static bool LoadToggle(string key, bool def = false) => PlayerPrefs.GetInt("mx_" + key, def ? 1 : 0) == 1;
        public static void SaveFloat(string key, float value) => PlayerPrefs.SetFloat("mx_" + key, value);
        public static float LoadFloat(string key, float def = 0f) => PlayerPrefs.GetFloat("mx_" + key, def);
        public static void SaveInt(string key, int value) => PlayerPrefs.SetInt("mx_" + key, value);
        public static int LoadInt(string key, int def = 0) => PlayerPrefs.GetInt("mx_" + key, def);
        public static void SaveAllToggles() { }
        public static void LoadAllToggles() { }
    }
}
