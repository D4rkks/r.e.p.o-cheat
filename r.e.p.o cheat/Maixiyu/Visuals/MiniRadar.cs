using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 小地图/雷达 - 在屏幕角落显示一个圆形雷达
/// 显示敌人（红点）、玩家（绿点）、物品（黄点）、撤离点（蓝点）
/// </summary>
public static class MiniRadar
{
    public static bool isRadarEnabled = false;
    public static float radarRadius = 120f;       // 雷达圆半径（像素）
    public static float radarRange = 50f;          // 扫描范围（米）
    public static float radarX = 20f;              // 雷达位置X（左下角偏移）
    public static float radarY = 20f;              // 雷达位置Y（底部偏移）

    private static Texture2D radarBgTex;
    private static Texture2D dotTex;

    // 颜色
    private static readonly Color bgColor = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color borderColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
    private static readonly Color playerDotColor = new Color(0f, 1f, 0f, 1f);
    private static readonly Color enemyDotColor = new Color(1f, 0.2f, 0.2f, 1f);
    private static readonly Color itemDotColor = new Color(1f, 1f, 0f, 0.9f);
    private static readonly Color extractDotColor = new Color(0.3f, 0.5f, 1f, 1f);
    private static readonly Color selfDotColor = Color.white;

    private static void EnsureTextures()
    {
        if (radarBgTex == null)
        {
            radarBgTex = new Texture2D(1, 1);
            radarBgTex.SetPixel(0, 0, Color.white);
            radarBgTex.Apply();
        }
        if (dotTex == null)
        {
            dotTex = new Texture2D(1, 1);
            dotTex.SetPixel(0, 0, Color.white);
            dotTex.Apply();
        }
    }

    /// <summary>
    /// 在 OnGUI 中调用绘制雷达
    /// </summary>
    public static void DrawRadar()
    {
        if (!isRadarEnabled) return;

        try
        {
            EnsureTextures();

            GameObject localPlayer = MaiDebugCheats.GetLocalPlayer();
            if ((Object)(object)localPlayer == (Object)null) return;

            Vector3 playerPos = localPlayer.transform.position;
            Camera _radarCam = GameHelper.GetActiveCamera();
            float playerYaw = _radarCam != null ? ((Component)_radarCam).transform.eulerAngles.y : 0f;

            // 雷达中心位置（左下角）
            float cx = radarX + radarRadius;
            float cy = Screen.height - radarY - radarRadius;

            // 绘制背景圆（用矩形近似、或用多层半透明）
            DrawFilledCircle(cx, cy, radarRadius, bgColor);
            DrawCircleOutline(cx, cy, radarRadius, borderColor, 2f);

            // 十字线
            DrawLine(cx - radarRadius * 0.8f, cy, cx + radarRadius * 0.8f, cy, new Color(0.3f, 0.8f, 0.3f, 0.3f));
            DrawLine(cx, cy - radarRadius * 0.8f, cx, cy + radarRadius * 0.8f, new Color(0.3f, 0.8f, 0.3f, 0.3f));

            // 自己（中心白点）
            DrawDot(cx, cy, 4f, selfDotColor);

            // 绘制敌人
            try
            {
                Enemy[] enemies = SceneCache.GetObjects<Enemy>(0.25f);
                if (enemies != null)
                {
                    foreach (Enemy enemy in enemies)
                    {
                        if ((Object)(object)enemy == (Object)null) continue;
                        Vector3 ePos = ((Component)enemy).transform.position;
                        DrawEntityDot(cx, cy, playerPos, ePos, playerYaw, enemyDotColor, 3f);
                    }
                }
            }
            catch { }

            // 绘制其他玩家
            try
            {
                List<PlayerAvatar> players = SemiFunc.PlayerGetList();
                if (players != null)
                {
                    foreach (PlayerAvatar pa in players)
                    {
                        if ((Object)(object)pa == (Object)null) continue;
                        GameObject go = ((Component)pa).gameObject;
                        if (go == localPlayer) continue;
                        Vector3 pPos = go.transform.position;
                        DrawEntityDot(cx, cy, playerPos, pPos, playerYaw, playerDotColor, 3f);
                    }
                }
            }
            catch { }

            // 绘制高价值物品
            try
            {
                ValuableObject[] items = SceneCache.GetObjects<ValuableObject>(0.5f);
                if (items != null)
                {
                    foreach (ValuableObject item in items)
                    {
                        if ((Object)(object)item == (Object)null) continue;
                        Vector3 iPos = ((Component)item).transform.position;
                        DrawEntityDot(cx, cy, playerPos, iPos, playerYaw, itemDotColor, 2f);
                    }
                }
            }
            catch { }

            // 绘制撤离点
            try
            {
                ExtractionPoint[] extracts = SceneCache.GetObjects<ExtractionPoint>(1f);
                if (extracts != null)
                {
                    foreach (ExtractionPoint ep in extracts)
                    {
                        if ((Object)(object)ep == (Object)null) continue;
                        Vector3 epPos = ((Component)ep).transform.position;
                        DrawEntityDot(cx, cy, playerPos, epPos, playerYaw, extractDotColor, 4f);
                    }
                }
            }
            catch { }

            // 图例
            float legendY = cy + radarRadius + 5f;
            DrawLegend(cx - radarRadius, legendY);
        }
        catch { }
    }

    private static void DrawEntityDot(float cx, float cy, Vector3 playerPos, Vector3 entityPos, float playerYaw, Color color, float size)
    {
        float dx = entityPos.x - playerPos.x;
        float dz = entityPos.z - playerPos.z;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);

        if (dist > radarRange) return;

        // 旋转使上方始终是玩家面朝方向
        float angle = -playerYaw * Mathf.Deg2Rad;
        float rotX = dx * Mathf.Cos(angle) - dz * Mathf.Sin(angle);
        float rotZ = dx * Mathf.Sin(angle) + dz * Mathf.Cos(angle);

        float scale = radarRadius / radarRange;
        float screenX = cx + rotX * scale;
        float screenY = cy - rotZ * scale;  // 屏幕Y轴翻转

        // 限制在雷达区域内
        float distFromCenter = Mathf.Sqrt((screenX - cx) * (screenX - cx) + (screenY - cy) * (screenY - cy));
        if (distFromCenter > radarRadius - 3f)
        {
            float clampFactor = (radarRadius - 3f) / distFromCenter;
            screenX = cx + (screenX - cx) * clampFactor;
            screenY = cy + (screenY - cy) * clampFactor;
        }

        DrawDot(screenX, screenY, size, color);
    }

    private static void DrawDot(float x, float y, float size, Color color)
    {
        Color prevColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(x - size, y - size, size * 2f, size * 2f), dotTex);
        GUI.color = prevColor;
    }

    private static void DrawFilledCircle(float cx, float cy, float radius, Color color)
    {
        // 用多个同心矩形近似圆
        Color prevColor = GUI.color;
        GUI.color = color;
        int steps = (int)(radius / 2f);
        for (int i = 0; i < steps; i++)
        {
            float y = -radius + (2f * radius * i / steps);
            float halfWidth = Mathf.Sqrt(radius * radius - y * y);
            GUI.DrawTexture(new Rect(cx - halfWidth, cy + y, halfWidth * 2f, 2f * radius / steps + 1f), radarBgTex);
        }
        GUI.color = prevColor;
    }

    private static void DrawCircleOutline(float cx, float cy, float radius, Color color, float thickness)
    {
        Color prevColor = GUI.color;
        GUI.color = color;
        int segments = 64;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (2f * Mathf.PI * i) / segments;
            float angle2 = (2f * Mathf.PI * (i + 1)) / segments;
            float x1 = cx + Mathf.Cos(angle1) * radius;
            float y1 = cy + Mathf.Sin(angle1) * radius;
            float x2 = cx + Mathf.Cos(angle2) * radius;
            float y2 = cy + Mathf.Sin(angle2) * radius;
            DrawLineSegment(x1, y1, x2, y2, thickness, color);
        }
        GUI.color = prevColor;
    }

    private static void DrawLine(float x1, float y1, float x2, float y2, Color color)
    {
        DrawLineSegment(x1, y1, x2, y2, 1f, color);
    }

    private static void DrawLineSegment(float x1, float y1, float x2, float y2, float thickness, Color color)
    {
        Color prevColor = GUI.color;
        Matrix4x4 savedMatrix = GUI.matrix;
        GUI.color = color;

        Vector2 start = new Vector2(x1, y1);
        Vector2 end = new Vector2(x2, y2);
        float length = Vector2.Distance(start, end);
        float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;

        GUIUtility.RotateAroundPivot(angle, start);
        GUI.DrawTexture(new Rect(start.x, start.y - thickness / 2f, length, thickness), radarBgTex);

        GUI.matrix = savedMatrix;
        GUI.color = prevColor;
    }

    private static GUIStyle legendStyle;

    private static GUIStyle GetLegendStyle()
    {
        if (legendStyle == null)
        {
            legendStyle = new GUIStyle(GUI.skin.label);
            legendStyle.fontSize = 10;
            legendStyle.normal.textColor = Color.white;
        }
        return legendStyle;
    }

    private static void DrawLegend(float x, float y)
    {
        GUIStyle style = GetLegendStyle();

        float spacing = 12f;
        DrawDot(x + 4f, y + 6f, 3f, enemyDotColor);
        GUI.Label(new Rect(x + 10f, y, 40f, 14f), L.T("radar.enemy"), style);

        DrawDot(x + 54f, y + 6f, 3f, playerDotColor);
        GUI.Label(new Rect(x + 60f, y, 40f, 14f), L.T("radar.player"), style);

        DrawDot(x + 104f, y + 6f, 2f, itemDotColor);
        GUI.Label(new Rect(x + 110f, y, 40f, 14f), L.T("radar.item"), style);

        DrawDot(x + 154f, y + 6f, 4f, extractDotColor);
        GUI.Label(new Rect(x + 160f, y, 40f, 14f), L.T("radar.extract"), style);
    }
}
