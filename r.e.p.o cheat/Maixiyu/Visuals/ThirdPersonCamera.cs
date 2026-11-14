using System;
using System.Reflection;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 第三人称相机系统 - 在自身页面切换第三人称视角
/// </summary>
public static class ThirdPersonCamera
{
    public static bool isEnabled = false;
    public static float cameraDistance = 3.0f;
    public static float cameraHeight = 1.5f;

    private static Transform originalParent;
    private static Vector3 originalLocalPos;
    private static Quaternion originalLocalRot;
    private static bool savedState = false;
    private static Camera mainCam;

    // 玩家身体渲染器状态保存/恢复
    private static Renderer[] playerRenderers;
    private static bool[] originalRendererStates;
    private static int originalCullingMask;
    private static bool savedRendererStates = false;

    /// <summary>
    /// 启用第三人称视角
    /// </summary>
    public static void Enable()
    {
        if (isEnabled) return;

        mainCam = GameHelper.GetActiveCamera();
        if ((UnityEngine.Object)(object)mainCam == (UnityEngine.Object)null) return;

        // 保存原始相机状态
        originalParent = mainCam.transform.parent;
        originalLocalPos = mainCam.transform.localPosition;
        originalLocalRot = mainCam.transform.localRotation;
        savedState = true;
        isEnabled = true;

        // 显示玩家身体模型
        EnablePlayerBody();

        Debug.Log("[ThirdPerson] Enabled");
    }

    /// <summary>
    /// 禁用第三人称视角，恢复原始相机
    /// </summary>
    public static void Disable()
    {
        if (!isEnabled) return;
        isEnabled = false;

        // 恢复玩家身体渲染器状态
        RestorePlayerBody();

        if (savedState && mainCam != null && (UnityEngine.Object)(object)mainCam != (UnityEngine.Object)null)
        {
            try
            {
                mainCam.transform.localPosition = originalLocalPos;
                mainCam.transform.localRotation = originalLocalRot;
            }
            catch { }
        }
        savedState = false;

        Debug.Log("[ThirdPerson] Disabled");
    }

    /// <summary>
    /// 切换第三人称
    /// </summary>
    public static void Toggle()
    {
        if (isEnabled) Disable();
        else Enable();
    }

    /// <summary>
    /// 在 LateUpdate 中调用 - 更新相机位置
    /// </summary>
    public static void LateUpdateCamera()
    {
        if (!isEnabled) return;

        if (mainCam == null || (UnityEngine.Object)(object)mainCam == (UnityEngine.Object)null)
        {
            mainCam = GameHelper.GetActiveCamera();
            if ((UnityEngine.Object)(object)mainCam == (UnityEngine.Object)null) return;
        }

        GameObject localPlayer = DebugCheats.GetLocalPlayer();
        if ((UnityEngine.Object)(object)localPlayer == (UnityEngine.Object)null) return;

        try
        {
            Transform playerT = localPlayer.transform;
            Vector3 playerPos = playerT.position + Vector3.up * cameraHeight;

            // 取相机的前方向（基于相机父级的旋转，即玩家面朝方向）
            Vector3 forward = mainCam.transform.parent != null
                ? mainCam.transform.parent.forward
                : mainCam.transform.forward;

            // 计算目标相机位置（在玩家身后）
            Vector3 desiredPos = playerPos - forward * cameraDistance;

            // 完全穿墙相机 - 不做射线检测，相机可穿过所有障碍物

            // 设置相机位置并看向玩家
            mainCam.transform.position = desiredPos;
            mainCam.transform.LookAt(playerPos);

            // 每帧确保玩家身体可见
            EnsurePlayerBodyVisible();
        }
        catch { }
    }

    /// <summary>
    /// 启用玩家身体渲染器，保存原始状态
    /// </summary>
    private static void EnablePlayerBody()
    {
        try
        {
            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if ((UnityEngine.Object)(object)localPlayer == (UnityEngine.Object)null) return;

            playerRenderers = localPlayer.GetComponentsInChildren<Renderer>(true);
            if (playerRenderers == null || playerRenderers.Length == 0) return;

            originalRendererStates = new bool[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if ((UnityEngine.Object)(object)playerRenderers[i] != (UnityEngine.Object)null)
                {
                    originalRendererStates[i] = playerRenderers[i].enabled;
                    playerRenderers[i].enabled = true;
                }
            }

            // 保存并修改相机 cullingMask，确保包含 Player 图层
            if (mainCam != null && (UnityEngine.Object)(object)mainCam != (UnityEngine.Object)null)
            {
                originalCullingMask = mainCam.cullingMask;
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0)
                {
                    mainCam.cullingMask |= (1 << playerLayer);
                }
            }

            // 尝试通过反射设置玩家模型可见（部分游戏用 layer 隐藏身体）
            TrySetPlayerLayerVisible(localPlayer, true);

            savedRendererStates = true;
        }
        catch { }
    }

    /// <summary>
    /// 恢复玩家身体渲染器原始状态
    /// </summary>
    private static void RestorePlayerBody()
    {
        try
        {
            if (!savedRendererStates) return;

            if (playerRenderers != null && originalRendererStates != null)
            {
                for (int i = 0; i < playerRenderers.Length; i++)
                {
                    if (playerRenderers[i] != null && (UnityEngine.Object)(object)playerRenderers[i] != (UnityEngine.Object)null)
                    {
                        playerRenderers[i].enabled = originalRendererStates[i];
                    }
                }
            }

            // 恢复相机 cullingMask
            if (mainCam != null && (UnityEngine.Object)(object)mainCam != (UnityEngine.Object)null)
            {
                mainCam.cullingMask = originalCullingMask;
            }

            // 恢复玩家 layer
            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if ((UnityEngine.Object)(object)localPlayer != (UnityEngine.Object)null)
            {
                TrySetPlayerLayerVisible(localPlayer, false);
            }

            savedRendererStates = false;
            playerRenderers = null;
            originalRendererStates = null;
        }
        catch { }
    }

    /// <summary>
    /// 每帧确保玩家身体可见（游戏可能每帧重新隐藏身体）
    /// </summary>
    private static void EnsurePlayerBodyVisible()
    {
        try
        {
            if (playerRenderers == null) return;
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null && (UnityEngine.Object)(object)playerRenderers[i] != (UnityEngine.Object)null
                    && !playerRenderers[i].enabled)
                {
                    playerRenderers[i].enabled = true;
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// 尝试设置玩家子对象的 layer，解决游戏通过 layer 隐藏身体的问题
    /// </summary>
    private static void TrySetPlayerLayerVisible(GameObject player, bool visible)
    {
        try
        {
            if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null) return;

            int playerLayer = LayerMask.NameToLayer("Player");
            int defaultLayer = 0; // Default layer

            // 查找并修改身体子对象的 layer
            Transform[] children = player.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if ((UnityEngine.Object)(object)child == (UnityEngine.Object)null) continue;
                string name = child.gameObject.name.ToLower();
                // 只处理看起来是身体部位的子对象
                if (name.Contains("body") || name.Contains("mesh") || name.Contains("visual") || 
                    name.Contains("model") || name.Contains("avatar") || name.Contains("skin"))
                {
                    if (visible && child.gameObject.layer != playerLayer)
                    {
                        // 设置为相机可见的 layer
                        child.gameObject.layer = defaultLayer;
                    }
                }
            }
        }
        catch { }
    }
}
