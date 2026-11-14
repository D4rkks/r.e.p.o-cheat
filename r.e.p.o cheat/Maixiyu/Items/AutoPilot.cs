using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

namespace r.e.p.o_cheat;

/// <summary>
/// 智能寻路自动完成游戏 - 状态机驱动，自动拾取→搬运→出售→循环
/// </summary>
public static class AutoPilot
{
    public enum PilotState
    {
        Idle,           // 等待激活
        FindingItem,    // 寻找最近的贵重物品
        MovingToItem,   // 移动到物品位置
        PickingUp,      // 拾取物品
        FindingSellPoint, // 寻找交易点
        MovingToSell,   // 移动到交易点
        Selling,        // 出售物品
        Completed       // 回合完成
    }

    public static bool isActive = false;
    public static PilotState currentState = PilotState.Idle;
    public static string statusText = "";

    // 配置
    private static float moveSpeed = 12f;
    private static float pickupRadius = 3f;
    private static float sellRadius = 4f;
    private static float pathRecalcInterval = 0.5f;

    // 内部状态
    private static Vector3 targetPosition;
    private static NavMeshPath currentPath;
    private static int pathIndex;
    private static float lastPathCalcTime;
    private static object currentTargetItem;
    private static ExtractionPoint currentSellPoint;
    private static int itemsDelivered = 0;
    private static float stuckTimer = 0f;
    private static Vector3 lastPosition;
    private static float stuckCheckInterval = 1f;
    private static float lastStuckCheck;

    public static void Toggle()
    {
        isActive = !isActive;
        if (isActive)
        {
            Start();
        }
        else
        {
            Stop();
        }
    }

    public static void Start()
    {
        isActive = true;
        currentState = PilotState.FindingItem;
        itemsDelivered = 0;
        stuckTimer = 0f;
        currentPath = new NavMeshPath();
        statusText = L.T("autopilot.starting");

        // 确保noclip关闭，因为autopilot需要navmesh
        if (NoclipController.noclipActive)
        {
            NoclipController.ToggleNoclip();
        }

        // 激活所有交易点
        try { MiscFeatures.ForceActivateAllExtractionPoints(); } catch { }

        Debug.Log("[AutoPilot] Started");
    }

    public static void Stop()
    {
        isActive = false;
        currentState = PilotState.Idle;
        currentTargetItem = null;
        currentSellPoint = null;
        statusText = L.T("autopilot.stopped");
        Debug.Log("[AutoPilot] Stopped");
    }

    /// <summary>
    /// 在 Update() 中调用
    /// </summary>
    public static void UpdateAutoPilot()
    {
        if (!isActive) return;

        GameObject localPlayer = MaiDebugCheats.GetLocalPlayer();
        if (localPlayer == null)
        {
            statusText = L.T("autopilot.no_player");
            return;
        }

        Vector3 myPos = localPlayer.transform.position;

        // 卡住检测
        if (Time.time - lastStuckCheck > stuckCheckInterval)
        {
            if (Vector3.Distance(myPos, lastPosition) < 0.3f && 
                (currentState == PilotState.MovingToItem || currentState == PilotState.MovingToSell))
            {
                stuckTimer += stuckCheckInterval;
                if (stuckTimer > 3f)
                {
                    // 卡住了，尝试跳跃+向上偏移
                    localPlayer.transform.position = myPos + Vector3.up * 2f;
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPosition = myPos;
            lastStuckCheck = Time.time;
        }

        switch (currentState)
        {
            case PilotState.FindingItem:
                FindNearestItem(myPos);
                break;

            case PilotState.MovingToItem:
                MoveAlongPath(localPlayer, myPos);
                // 到达物品附近
                if (Vector3.Distance(myPos, targetPosition) < pickupRadius)
                {
                    currentState = PilotState.PickingUp;
                    statusText = L.T("autopilot.picking");
                }
                break;

            case PilotState.PickingUp:
                PickupItem(localPlayer, myPos);
                break;

            case PilotState.FindingSellPoint:
                FindNearestSellPoint(myPos);
                break;

            case PilotState.MovingToSell:
                MoveAlongPath(localPlayer, myPos);
                if (Vector3.Distance(myPos, targetPosition) < sellRadius)
                {
                    currentState = PilotState.Selling;
                    statusText = L.T("autopilot.selling");
                }
                break;

            case PilotState.Selling:
                SellItem(myPos);
                break;

            case PilotState.Completed:
                statusText = L.T("autopilot.done", itemsDelivered);
                Stop();
                break;
        }
    }

    private static void FindNearestItem(Vector3 myPos)
    {
        statusText = L.T("autopilot.finding_item");

        var items = MaiDebugCheats.valuableObjects;
        if (items == null || items.Count == 0)
        {
            // 没有更多物品，检查是否完成
            currentState = PilotState.Completed;
            return;
        }

        object nearest = null;
        float nearestDist = float.MaxValue;

        foreach (object item in items)
        {
            if (item == null) continue;
            try
            {
                UnityEngine.Object unityObj = item as UnityEngine.Object;
                if (unityObj != null && unityObj == null) continue;

                Transform t = null;
                if (item is Component comp)
                    t = comp.transform;

                if (t == null) continue;

                float dist = Vector3.Distance(myPos, t.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = item;
                }
            }
            catch { }
        }

        if (nearest == null)
        {
            currentState = PilotState.Completed;
            return;
        }

        currentTargetItem = nearest;
        Transform target = (nearest is Component c) ? c.transform : null;
        if (target != null)
        {
            targetPosition = target.position;
            CalculatePath(myPos, targetPosition);
            currentState = PilotState.MovingToItem;
            statusText = L.T("autopilot.moving_item", nearestDist.ToString("F0"));
        }
    }

    private static void FindNearestSellPoint(Vector3 myPos)
    {
        statusText = L.T("autopilot.finding_sell");

        ExtractionPoint[] points = SceneCache.GetObjects<ExtractionPoint>(1f);
        if (points.Length == 0)
        {
            Debug.LogWarning("[AutoPilot] No sell points found");
            currentState = PilotState.Completed;
            return;
        }

        ExtractionPoint nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var ep in points)
        {
            float dist = Vector3.Distance(myPos, ((Component)ep).transform.position);
            if (dist < nearestDist) { nearestDist = dist; nearest = ep; }
        }

        if (nearest == null)
        {
            currentState = PilotState.Completed;
            return;
        }

        currentSellPoint = nearest;
        targetPosition = ((Component)nearest).transform.position;
        CalculatePath(myPos, targetPosition);
        currentState = PilotState.MovingToSell;
        statusText = L.T("autopilot.moving_sell", nearestDist.ToString("F0"));
    }

    private static void PickupItem(GameObject localPlayer, Vector3 myPos)
    {
        // 直接将物品传送到玩家手中（模拟拾取）
        if (currentTargetItem == null)
        {
            currentState = PilotState.FindingItem;
            return;
        }

        try
        {
            Transform itemT = (currentTargetItem is Component comp) ? comp.transform : null;
            if (itemT != null)
            {
                // 将物品传送到交易点而非搬运（更可靠）
                ExtractionPoint[] points = SceneCache.GetObjects<ExtractionPoint>(1f);
                if (points.Length > 0)
                {
                    ExtractionPoint nearest = null;
                    float nearestDist = float.MaxValue;
                    foreach (var ep in points)
                    {
                        float dist = Vector3.Distance(myPos, ((Component)ep).transform.position);
                        if (dist < nearestDist) { nearestDist = dist; nearest = ep; }
                    }
                    if (nearest != null && currentTargetItem is Component itemComp)
                    {
                        if (MaiItemTeleport.TryGetOpenExtractionDrop(out Vector3 dropPos, out Quaternion dropRot, out _))
                        {
                            MaiItemTeleport.TeleportComponent(itemComp, dropPos, dropRot);
                        }
                        else
                        {
                            Transform pose = nearest.safetySpawn != null ? nearest.safetySpawn : ((Component)nearest).transform;
                            MaiItemTeleport.TeleportComponent(itemComp, MaiItemTeleport.SnapToGround(pose.position), pose.rotation);
                        }
                        itemsDelivered++;
                        currentTargetItem = null;
                        currentState = PilotState.FindingItem;
                        statusText = L.T("autopilot.delivered", itemsDelivered);
                        return;
                    }
                }
            }
        }
        catch { }

        // Fallback: 搬运到交易点
        currentState = PilotState.FindingSellPoint;
    }

    private static void SellItem(Vector3 myPos)
    {
        // 物品应该已经在交易点附近了
        itemsDelivered++;
        currentTargetItem = null;
        currentState = PilotState.FindingItem;
        statusText = L.T("autopilot.delivered", itemsDelivered);
    }

    private static void CalculatePath(Vector3 from, Vector3 to)
    {
        pathIndex = 0;
        lastPathCalcTime = Time.time;

        // 尝试使用 NavMesh 计算路径
        NavMeshHit fromHit, toHit;
        if (NavMesh.SamplePosition(from, out fromHit, 10f, NavMesh.AllAreas) &&
            NavMesh.SamplePosition(to, out toHit, 10f, NavMesh.AllAreas))
        {
            if (NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, currentPath))
            {
                if (currentPath.status == NavMeshPathStatus.PathComplete ||
                    currentPath.status == NavMeshPathStatus.PathPartial)
                {
                    return; // 使用 NavMesh 路径
                }
            }
        }

        // NavMesh 不可用时，直接走直线
        currentPath = new NavMeshPath();
    }

    private static void MoveAlongPath(GameObject localPlayer, Vector3 myPos)
    {
        // 定期重新计算路径
        if (Time.time - lastPathCalcTime > pathRecalcInterval)
        {
            // 更新目标位置（物品可能移动了）
            if (currentState == PilotState.MovingToItem && currentTargetItem != null)
            {
                try
                {
                    Transform t = (currentTargetItem is Component c) ? c.transform : null;
                    if (t != null) targetPosition = t.position;
                }
                catch { }
            }
            CalculatePath(myPos, targetPosition);
        }

        Vector3 nextPoint;
        if (currentPath != null && currentPath.corners != null && currentPath.corners.Length > 0)
        {
            // 沿 NavMesh 路径移动
            if (pathIndex >= currentPath.corners.Length)
                pathIndex = currentPath.corners.Length - 1;

            nextPoint = currentPath.corners[pathIndex];

            // 到达路径点就前进到下一个
            if (Vector3.Distance(new Vector3(myPos.x, 0, myPos.z), new Vector3(nextPoint.x, 0, nextPoint.z)) < 1.5f)
            {
                pathIndex++;
                if (pathIndex >= currentPath.corners.Length)
                {
                    nextPoint = targetPosition;
                }
                else
                {
                    nextPoint = currentPath.corners[pathIndex];
                }
            }
        }
        else
        {
            // 直线走向目标
            nextPoint = targetPosition;
        }

        // 移动玩家
        Vector3 direction = (nextPoint - myPos).normalized;
        float step = moveSpeed * Time.deltaTime;
        localPlayer.transform.position = myPos + direction * step;

        // 面向移动方向
        if (direction.sqrMagnitude > 0.01f)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
                localPlayer.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// 绘制路径可视化 (在 OnGUI 中调用)
    /// </summary>
    public static void DrawPathGizmo()
    {
        if (!isActive || currentPath == null || currentPath.corners == null || currentPath.corners.Length < 2) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        for (int i = 0; i < currentPath.corners.Length - 1; i++)
        {
            Vector3 sp1 = cam.WorldToScreenPoint(currentPath.corners[i]);
            Vector3 sp2 = cam.WorldToScreenPoint(currentPath.corners[i + 1]);

            if (sp1.z > 0 && sp2.z > 0)
            {
                DrawGUILine(
                    new Vector2(sp1.x, Screen.height - sp1.y),
                    new Vector2(sp2.x, Screen.height - sp2.y),
                    Color.cyan, 2f);
            }
        }
    }

    private static Texture2D pathTex;
    private static void DrawGUILine(Vector2 a, Vector2 b, Color color, float width)
    {
        if (pathTex == null)
        {
            pathTex = new Texture2D(1, 1);
            pathTex.SetPixel(0, 0, Color.white);
            pathTex.Apply();
        }

        Color saved = GUI.color;
        GUI.color = color;

        Vector2 d = b - a;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        float len = d.magnitude;

        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width / 2f, len, width), pathTex);
        GUIUtility.RotateAroundPivot(-angle, a);

        GUI.color = saved;
    }
}
