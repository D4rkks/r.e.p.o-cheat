using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace r.e.p.o_cheat
{
    // Top-right overlay: alive count, who is dead and what each dead player is looking at.
    // Only "dead player is controlling their own head" (PlayerDeathHead.spectated) is synced over the network;
    // which living player someone spectates is local to their machine (SpectateCamera.player), so it's only known for us.
    public static class PlayerStatusOverlay
    {
        public static bool enabled = true;

        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo IsDisabledField = typeof(PlayerAvatar).GetField("isDisabled", Flags);
        private static readonly FieldInfo IsLocalField = typeof(PlayerAvatar).GetField("isLocal", Flags);
        private static readonly FieldInfo DeathHeadField = typeof(PlayerAvatar).GetField("playerDeathHead", Flags);
        private static readonly FieldInfo HeadSpectatedField = typeof(PlayerDeathHead).GetField("spectated", Flags);
        private static readonly FieldInfo HeadInExtractionField = typeof(PlayerDeathHead).GetField("inExtractionPoint", Flags);
        private static readonly FieldInfo HeadInTruckField = typeof(PlayerDeathHead).GetField("inTruck", Flags);
        private static readonly FieldInfo HealthField = typeof(PlayerHealth).GetField("health", Flags);
        private static readonly FieldInfo MaxHealthField = typeof(PlayerHealth).GetField("maxHealth", Flags);
        private static readonly FieldInfo SpectatePlayerField = typeof(SpectateCamera).GetField("player", Flags);

        private static readonly List<string> lines = new List<string>();
        private static string header = "";
        private static float nextRefresh;
        private static GUIStyle style;
        private static Texture2D background;

        private static T Get<T>(FieldInfo field, object target, T fallback)
        {
            if (field == null || target == null) return fallback;
            try { return (T)field.GetValue(target); } catch { return fallback; }
        }

        private static void Refresh()
        {
            lines.Clear();
            header = "";
            List<PlayerAvatar> players;
            try { players = SemiFunc.PlayerGetList(); } catch { return; }
            if (players == null || players.Count == 0) return;

            int alive = 0;
            foreach (var player in players)
            {
                if (player == null) continue;
                string name = SemiFunc.PlayerGetName(player) ?? "Unknown";
                bool dead = Get(IsDisabledField, player, false);
                bool local = Get(IsLocalField, player, false);
                string you = local ? " (you)" : "";

                if (!dead)
                {
                    alive++;
                    var health = player.playerHealth;
                    int hp = Get(HealthField, health, -1);
                    int max = Get(MaxHealthField, health, -1);
                    string hpText = hp >= 0 ? $"  {hp}/{max} HP" : "";
                    lines.Add($"<color=lime>ALIVE</color>  {name}{you}<color=#aaaaaa>{hpText}</color>");
                    continue;
                }

                string watching;
                var head = Get<PlayerDeathHead>(DeathHeadField, player, null);
                if (head != null && Get(HeadSpectatedField, head, false))
                {
                    watching = "controlling own head";
                }
                else if (local && SpectateCamera.instance != null)
                {
                    var target = Get<PlayerAvatar>(SpectatePlayerField, SpectateCamera.instance, null);
                    watching = target != null ? "watching " + (SemiFunc.PlayerGetName(target) ?? "?") : "spectating";
                }
                else
                {
                    watching = "spectating a player";
                }

                string headState = "";
                if (head != null && Get(HeadInExtractionField, head, false)) headState = "  <color=yellow>[head in extraction]</color>";
                else if (head != null && Get(HeadInTruckField, head, false)) headState = "  <color=yellow>[head in truck]</color>";

                lines.Add($"<color=red>DEAD</color>  {name}{you}  <color=#aaaaaa>→ {watching}</color>{headState}");
            }

            header = $"Players alive: {alive}/{players.Count}";
        }

        public static void Draw()
        {
            if (!enabled) return;
            if (Time.time >= nextRefresh)
            {
                nextRefresh = Time.time + 0.5f;
                Refresh();
            }
            if (lines.Count == 0) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13, wordWrap = false };
                background = new Texture2D(1, 1);
                background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
                background.Apply();
            }

            const float width = 360f, lineHeight = 18f, padding = 8f;
            float height = padding * 2 + lineHeight * (lines.Count + 1) + 4f;
            float x = Screen.width - width - 10f, y = 10f;

            GUI.DrawTexture(new Rect(x, y, width, height), background);
            GUI.Label(new Rect(x + padding, y + padding, width - padding * 2, lineHeight), $"<b>{header}</b>", style);
            for (int i = 0; i < lines.Count; i++)
            {
                GUI.Label(new Rect(x + padding, y + padding + 4f + lineHeight * (i + 1), width - padding * 2, lineHeight), lines[i], style);
            }
        }
    }
}
