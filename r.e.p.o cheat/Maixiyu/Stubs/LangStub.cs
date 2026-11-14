// Stub thay cho LanguageManager của Mai-xiyu: trả về key gốc (không i18n).
namespace r.e.p.o_cheat
{
    internal static class LanguageManager
    {
        public static string GetEnemyName(string rawName) => rawName;
        public static string GetColorName(int index) => "Color " + index;
        public static string T(string key) => key;
        public static string T(string key, params object[] args) => args != null && args.Length > 0 ? string.Format(key, args) : key;
    }

    internal static class L
    {
        public static string T(string key) => LanguageManager.T(key);
        public static string T(string key, params object[] args) => LanguageManager.T(key, args);
    }
}
