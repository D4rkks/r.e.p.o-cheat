using System;
using UnityEngine;

namespace r.e.p.o_cheat
{
    // Drop zone defined relative to the extraction point: offset in its local (yaw-only) frame, plus size and extra yaw
    public static class ExtractionZone
    {
        // Calibrated in-game on the extraction platform
        private const float DefaultOffsetX = 0f, DefaultOffsetY = 0.15f, DefaultOffsetZ = 0f;
        private const float DefaultSizeX = 3.3f, DefaultSizeZ = 2.9f, DefaultYaw = 0f;

        public static bool showZone = false;
        public static float offsetX = DefaultOffsetX, offsetY = DefaultOffsetY, offsetZ = DefaultOffsetZ;
        public static float sizeX = DefaultSizeX, sizeZ = DefaultSizeZ, yaw = DefaultYaw;

        private static ExtractionPoint cachedTarget;
        private static float lastTargetLookup = -10f;

        public static ExtractionPoint Target
        {
            get
            {
                if (cachedTarget == null || Time.time - lastTargetLookup > 1f)
                {
                    cachedTarget = ItemTeleport.FindTargetExtractionPoint();
                    lastTargetLookup = Time.time;
                }
                return cachedTarget;
            }
        }

        private static Quaternion BaseRotation(ExtractionPoint ep) => Quaternion.Euler(0f, ep.transform.eulerAngles.y, 0f);

        public static void GetZone(ExtractionPoint ep, out Vector3 center, out Quaternion rotation)
        {
            Quaternion baseRot = BaseRotation(ep);
            center = ep.transform.position + baseRot * new Vector3(offsetX, offsetY, offsetZ);
            rotation = baseRot * Quaternion.Euler(0f, yaw, 0f);
        }

        public static void ResetToDefaults()
        {
            offsetX = DefaultOffsetX;
            offsetY = DefaultOffsetY;
            offsetZ = DefaultOffsetZ;
            sizeX = DefaultSizeX;
            sizeZ = DefaultSizeZ;
            yaw = DefaultYaw;
        }

        public static void LogConfig()
        {
            Hax2.Log1($"ExtractionZone: offset=({offsetX:F2}, {offsetY:F2}, {offsetZ:F2}) size=({sizeX:F2}, {sizeZ:F2}) yaw={yaw:F0}");
        }

        private static float Round(float v) => Mathf.Round(v * 20f) / 20f;

        public static void DrawMenu(float x, float y)
        {
            showZone = GUI.Toggle(new Rect(x, y, 250, 20), showZone, "Show Extraction Zone (calibration)");
            if (!showZone) return;

            var ep = Target;
            if (ep == null)
            {
                GUI.Label(new Rect(x, y + 25, 400, 20), "No extraction point found.");
                return;
            }

            float colW = 260f;
            offsetX = FineSlider("Offset X", offsetX, -5f, 5f, x, y + 25, colW);
            offsetY = FineSlider("Offset Y (floor)", offsetY, -2f, 2f, x, y + 70, colW);
            offsetZ = FineSlider("Offset Z", offsetZ, -5f, 5f, x, y + 115, colW);
            sizeX = FineSlider("Size X", sizeX, 0.5f, 6f, x + colW + 20, y + 25, colW);
            sizeZ = FineSlider("Size Z", sizeZ, 0.5f, 6f, x + colW + 20, y + 70, colW);
            yaw = Mathf.Round(LabeledSlider("Yaw", yaw, -180f, 180f, x + colW + 20, y + 115, colW, "F0"));

            if (GUI.Button(new Rect(x, y + 165, colW, 28), "Log zone config (F12)")) LogConfig();
            if (GUI.Button(new Rect(x + colW + 20, y + 165, colW, 28), "Reset to defaults")) ResetToDefaults();
        }

        private static float FineSlider(string label, float value, float min, float max, float x, float y, float width)
        {
            return Round(LabeledSlider(label, value, min, max, x, y, width, "F2"));
        }

        private static float LabeledSlider(string label, float value, float min, float max, float x, float y, float width, string format)
        {
            GUI.Label(new Rect(x, y, width, 20), $"{label}: {value.ToString(format)}");
            return GUI.HorizontalSlider(new Rect(x, y + 22, width, 18), value, min, max);
        }

        public static void DrawEsp()
        {
            if (!showZone) return;
            var ep = Target;
            var cam = Camera.main;
            if (ep == null || cam == null) return;

            GetZone(ep, out Vector3 center, out Quaternion rot);
            Vector3 r = rot * Vector3.right * (sizeX / 2f);
            Vector3 f = rot * Vector3.forward * (sizeZ / 2f);

            Vector3 backLeft = center - r - f;
            Vector3 backRight = center + r - f;
            Vector3 frontRight = center + r + f;
            Vector3 frontLeft = center - r + f;

            DrawWorldLine(cam, backLeft, backRight, Color.green);
            DrawWorldLine(cam, backRight, frontRight, Color.green);
            DrawWorldLine(cam, frontLeft, backLeft, Color.green);
            DrawWorldLine(cam, frontRight, frontLeft, Color.red); // +Z edge

            DrawWorldPoint(cam, ep.transform.position, Color.magenta, "origin");
            DrawWorldPoint(cam, center, Color.cyan, "zone");
        }

        private static bool ToScreen(Camera cam, Vector3 world, out Vector2 screen)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            float scaleX = (float)Screen.width / cam.pixelWidth;
            float scaleY = (float)Screen.height / cam.pixelHeight;
            screen = new Vector2(sp.x * scaleX, Screen.height - sp.y * scaleY);
            return sp.z > 0f;
        }

        private static void DrawWorldLine(Camera cam, Vector3 a, Vector3 b, Color color)
        {
            if (!ToScreen(cam, a, out Vector2 sa) || !ToScreen(cam, b, out Vector2 sb)) return;

            float length = Vector2.Distance(sa, sb);
            float angle = Mathf.Atan2(sb.y - sa.y, sb.x - sa.x) * Mathf.Rad2Deg;
            Matrix4x4 matrix = GUI.matrix;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, sa);
            GUI.DrawTexture(new Rect(sa.x, sa.y - 1f, length, 2f), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = Color.white;
        }

        private static void DrawWorldPoint(Camera cam, Vector3 world, Color color, string label)
        {
            if (!ToScreen(cam, world, out Vector2 s)) return;
            GUI.color = color;
            GUI.DrawTexture(new Rect(s.x - 4f, s.y - 4f, 8f, 8f), Texture2D.whiteTexture);
            GUI.Label(new Rect(s.x + 6f, s.y - 10f, 100f, 20f), label);
            GUI.color = Color.white;
        }
    }
}
