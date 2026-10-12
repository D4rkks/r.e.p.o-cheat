using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat
{
    public static class ExtractionControl
    {
        private static readonly FieldInfo StateField = typeof(ExtractionPoint).GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo LockedField = typeof(ExtractionPoint).GetField("isLocked", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly List<ExtractionPoint> points = new List<ExtractionPoint>();
        private static int selected;
        private static float lastRefresh = -10f;

        private static void Refresh()
        {
            if (Time.time - lastRefresh < 1f) return;
            lastRefresh = Time.time;

            points.Clear();
            foreach (var ep in UnityEngine.Object.FindObjectsOfType<ExtractionPoint>())
            {
                if (ep != null && ep.gameObject.activeInHierarchy) points.Add(ep);
            }
            // Stable order so the selection doesn't jump around while moving
            points.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
        }

        private static string Describe(ExtractionPoint ep)
        {
            string state = StateField?.GetValue(ep)?.ToString() ?? "?";
            bool locked = LockedField != null && (bool)LockedField.GetValue(ep);
            GameObject player = DebugCheats.GetLocalPlayer();
            string distance = player != null ? $"{Vector3.Distance(player.transform.position, ep.transform.position):F0}m" : "?";
            return $"{state}{(locked ? " (locked)" : "")} - {distance}";
        }

        public static void DrawMenu(float x, float y)
        {
            Refresh();
            GUI.Label(new Rect(x, y, 540, 20), "Extraction points:");
            if (points.Count == 0)
            {
                GUI.Label(new Rect(x, y + 26, 540, 20), "None found.");
                return;
            }

            selected = Mathf.Clamp(selected, 0, points.Count - 1);
            if (GUI.Button(new Rect(x, y + 22, 40, 28), "<")) selected = (selected - 1 + points.Count) % points.Count;
            if (GUI.Button(new Rect(x + 500, y + 22, 40, 28), ">")) selected = (selected + 1) % points.Count;

            var ep = points[selected];
            if (ep == null) return;
            GUI.Label(new Rect(x + 50, y + 26, 440, 20), $"#{selected + 1}/{points.Count}  {Describe(ep)}");

            if (GUI.Button(new Rect(x, y + 58, 265, 28), "Teleport Me to Extraction")) TeleportTo(ep);
            if (GUI.Button(new Rect(x + 275, y + 58, 265, 28), "Activate Extraction")) Activate(ep);
        }

        private static void Activate(ExtractionPoint ep)
        {
            try
            {
                // Same path as pressing the button: non-host clients send an activation request to the host
                ep.OnClick();
                Hax2.Log1($"Activation requested for extraction '{ep.name}' ({Describe(ep)})");
            }
            catch (Exception e)
            {
                Hax2.Log1($"Error activating extraction: {e.Message}");
            }
        }

        private static void TeleportTo(ExtractionPoint ep)
        {
            try
            {
                GameObject player = DebugCheats.GetLocalPlayer();
                if (player == null)
                {
                    Hax2.Log1("Local player not found!");
                    return;
                }

                Vector3 target = ep.safetySpawn != null
                    ? ep.safetySpawn.position
                    : ep.transform.position + ep.transform.forward * 3f;
                target += Vector3.up * 0.5f;

                Vector3 look = ep.transform.position - target;
                look.y = 0f;
                Quaternion rotation = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : player.transform.rotation;

                // SpawnRPC is host-only; moving our own PlayerController works for everyone
                if (Teleport.TeleportLocalPlayer(target, rotation))
                    Hax2.Log1($"Teleported to extraction '{ep.name}' at {target}");
            }
            catch (Exception e)
            {
                Hax2.Log1($"Error teleporting to extraction: {e.Message}");
            }
        }
    }
}
