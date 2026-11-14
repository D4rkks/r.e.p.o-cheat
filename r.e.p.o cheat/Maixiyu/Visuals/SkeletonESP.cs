using System;
using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 玩家骨骼 ESP - 使用 GL 线条渲染骨骼连接
/// </summary>
public static class SkeletonESP
{
    public static bool enabled = false;
    public static Color boneColor = Color.green;
    public static float maxDistance = float.MaxValue;

    private static Material lineMaterial;

    // 骨骼连接定义 (从 → 到)
    private static readonly HumanBodyBones[][] boneConnections = new HumanBodyBones[][]
    {
        // 脊柱
        new[] { HumanBodyBones.Hips, HumanBodyBones.Spine },
        new[] { HumanBodyBones.Spine, HumanBodyBones.Chest },
        new[] { HumanBodyBones.Chest, HumanBodyBones.UpperChest },
        new[] { HumanBodyBones.UpperChest, HumanBodyBones.Neck },
        new[] { HumanBodyBones.Neck, HumanBodyBones.Head },

        // 左臂
        new[] { HumanBodyBones.UpperChest, HumanBodyBones.LeftShoulder },
        new[] { HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm },
        new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm },
        new[] { HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand },

        // 右臂
        new[] { HumanBodyBones.UpperChest, HumanBodyBones.RightShoulder },
        new[] { HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm },
        new[] { HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm },
        new[] { HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand },

        // 左腿
        new[] { HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg },
        new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg },
        new[] { HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot },

        // 右腿
        new[] { HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg },
        new[] { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg },
        new[] { HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot },
    };

    private static void EnsureMaterial()
    {
        if (lineMaterial != null) return;
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) return;
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    /// <summary>
    /// 在 OnPostRender 或 OnRenderObject 中调用
    /// </summary>
    public static void RenderSkeletons()
    {
        if (!enabled) return;

        Camera cam = GameHelper.GetActiveCamera();
        if (cam == null) return;

        EnsureMaterial();
        if (lineMaterial == null) return;

        // 获取所有远程玩家
        List<PlayerAvatar> players = null;
        try
        {
            if (GameDirector.instance != null)
                players = GameDirector.instance.PlayerList;
        }
        catch { return; }

        if (players == null || players.Count == 0) return;

        PlayerAvatar localAvatar = null;
        try { localAvatar = SemiFunc.PlayerAvatarLocal(); } catch { }

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.modelview = cam.worldToCameraMatrix;
        GL.Begin(GL.LINES);

        foreach (PlayerAvatar player in players)
        {
            try
            {
                if (player == null || (UnityEngine.Object)(object)player == (UnityEngine.Object)null) continue;

                // 跳过自己
                if (localAvatar != null && player == localAvatar) continue;

                Transform playerTransform = ((Component)player).transform;
                float dist = Vector3.Distance(cam.transform.position, playerTransform.position);
                if (dist > maxDistance) continue;

                Animator animator = ((Component)player).GetComponentInChildren<Animator>();
                if (animator == null) continue;

                // 根据距离调整颜色透明度
                float alpha = Mathf.Clamp01(1f - (dist / maxDistance) * 0.5f);
                Color color = new Color(boneColor.r, boneColor.g, boneColor.b, alpha);
                GL.Color(color);

                foreach (HumanBodyBones[] conn in boneConnections)
                {
                    Transform from = animator.GetBoneTransform(conn[0]);
                    Transform to = animator.GetBoneTransform(conn[1]);

                    if (from == null || to == null) continue;

                    GL.Vertex(from.position);
                    GL.Vertex(to.position);
                }
            }
            catch { continue; }
        }

        GL.End();
        GL.PopMatrix();
    }

    /// <summary>
    /// 在传统 ESP 的 OnGUI 中渲染骨骼关节点（2D 叠加）
    /// </summary>
    public static void RenderSkeletonDots(Camera cam)
    {
        if (!enabled || cam == null) return;

        List<PlayerAvatar> players = null;
        try
        {
            if (GameDirector.instance != null)
                players = GameDirector.instance.PlayerList;
        }
        catch { return; }

        if (players == null) return;

        PlayerAvatar localAvatar = null;
        try { localAvatar = SemiFunc.PlayerAvatarLocal(); } catch { }

        foreach (PlayerAvatar player in players)
        {
            try
            {
                if (player == null || (UnityEngine.Object)(object)player == (UnityEngine.Object)null) continue;
                if (localAvatar != null && player == localAvatar) continue;

                Transform pt = ((Component)player).transform;
                float dist = Vector3.Distance(cam.transform.position, pt.position);
                if (dist > maxDistance) continue;

                Animator animator = ((Component)player).GetComponentInChildren<Animator>();
                if (animator == null) continue;

                float alpha = Mathf.Clamp01(1f - (dist / maxDistance) * 0.5f);

                foreach (HumanBodyBones[] conn in boneConnections)
                {
                    Transform from = animator.GetBoneTransform(conn[0]);
                    Transform to = animator.GetBoneTransform(conn[1]);
                    if (from == null || to == null) continue;

                    Vector3 sp1 = cam.WorldToScreenPoint(from.position);
                    Vector3 sp2 = cam.WorldToScreenPoint(to.position);
                    if (sp1.z <= 0 || sp2.z <= 0) continue;

                    DrawLine(
                        new Vector2(sp1.x, Screen.height - sp1.y),
                        new Vector2(sp2.x, Screen.height - sp2.y),
                        new Color(boneColor.r, boneColor.g, boneColor.b, alpha),
                        2f);
                }
            }
            catch { continue; }
        }
    }

    // GUI.DrawTexture based line drawing
    private static Texture2D lineTexture;
    private static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        if (lineTexture == null)
        {
            lineTexture = new Texture2D(1, 1);
            lineTexture.SetPixel(0, 0, Color.white);
            lineTexture.Apply();
        }

        Color savedColor = GUI.color;
        GUI.color = color;

        Vector2 delta = b - a;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float length = delta.magnitude;

        Matrix4x4 savedMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width / 2f, length, width), lineTexture);
        // 使用保存/恢复 GUI.matrix 代替反向旋转，避免累积误差
        GUI.matrix = savedMatrix;

        GUI.color = savedColor;
    }
}
