using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 增强传送系统 - 支持保存传送点、光标传送、随机传送
/// </summary>
public static class TeleportPlus
{
    /// <summary>保存的传送点</summary>
    public class Waypoint
    {
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;

        public Waypoint(string name, Vector3 pos, Quaternion rot)
        {
            Name = name;
            Position = pos;
            Rotation = rot;
        }
    }

    public static List<Waypoint> savedWaypoints = new List<Waypoint>();
    public static int selectedWaypointIndex = 0;
    public static string newWaypointName = "";

    // 传送到屏幕准心位置
    public static void TeleportToCrosshair(float maxDistance = 100f)
    {
        try
        {
            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if ((Object)(object)localPlayer == (Object)null)
            {
                Debug.Log((object)"[传送] 未找到本地玩家");
                return;
            }

            Camera main = Camera.main;
            if ((Object)(object)main == (Object)null)
            {
                Debug.Log((object)"[传送] 未找到摄像机");
                return;
            }

            Ray ray = main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            Vector3 targetPos;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                targetPos = hit.point + Vector3.up * 1.5f;
            }
            else
            {
                targetPos = ray.GetPoint(maxDistance);
            }

            DoTeleport(localPlayer, targetPos);
            Debug.Log((object)$"[传送] 已传送到准心位置 ({targetPos.x:F1}, {targetPos.y:F1}, {targetPos.z:F1})");
        }
        catch (Exception ex)
        {
            Debug.LogError((object)$"[传送] 准心传送失败: {ex.Message}");
        }
    }

    // 保存当前位置为传送点
    public static void SaveCurrentPosition(string name = null)
    {
        try
        {
            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if ((Object)(object)localPlayer == (Object)null) return;

            string wpName = string.IsNullOrEmpty(name) ? $"传送点 {savedWaypoints.Count + 1}" : name;
            Vector3 pos = localPlayer.transform.position;
            Quaternion rot = localPlayer.transform.rotation;
            savedWaypoints.Add(new Waypoint(wpName, pos, rot));
            Debug.Log((object)$"[传送] 保存传送点: {wpName} at ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
        }
        catch (Exception ex)
        {
            Debug.LogError((object)$"[传送] 保存位置失败: {ex.Message}");
        }
    }

    // 传送到已保存的传送点
    public static void TeleportToWaypoint(int index)
    {
        if (index < 0 || index >= savedWaypoints.Count)
        {
            Debug.Log((object)"[传送] 无效的传送点索引");
            return;
        }

        try
        {
            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if ((Object)(object)localPlayer == (Object)null) return;

            Waypoint wp = savedWaypoints[index];
            DoTeleport(localPlayer, wp.Position);
            Debug.Log((object)$"[传送] 已传送到: {wp.Name}");
        }
        catch (Exception ex)
        {
            Debug.LogError((object)$"[传送] 传送到传送点失败: {ex.Message}");
        }
    }

    // 删除传送点
    public static void RemoveWaypoint(int index)
    {
        if (index >= 0 && index < savedWaypoints.Count)
        {
            string name = savedWaypoints[index].Name;
            savedWaypoints.RemoveAt(index);
            if (selectedWaypointIndex >= savedWaypoints.Count)
                selectedWaypointIndex = Math.Max(0, savedWaypoints.Count - 1);
            Debug.Log((object)$"[传送] 已删除传送点: {name}");
        }
    }

    // 传送到随机位置（在地面上）
    public static void TeleportRandom(float range = 50f)
    {
        try
        {
            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if ((Object)(object)localPlayer == (Object)null) return;

            Vector3 currentPos = localPlayer.transform.position;
            float randX = currentPos.x + UnityEngine.Random.Range(-range, range);
            float randZ = currentPos.z + UnityEngine.Random.Range(-range, range);

            // 射线检测地面
            Vector3 targetPos;
            if (Physics.Raycast(new Vector3(randX, currentPos.y + 50f, randZ), Vector3.down, out RaycastHit hit, 200f))
            {
                targetPos = hit.point + Vector3.up * 1.5f;
            }
            else
            {
                targetPos = new Vector3(randX, currentPos.y, randZ);
            }

            DoTeleport(localPlayer, targetPos);
            Debug.Log((object)$"[传送] 随机传送到 ({targetPos.x:F1}, {targetPos.y:F1}, {targetPos.z:F1})");
        }
        catch (Exception ex)
        {
            Debug.LogError((object)$"[传送] 随机传送失败: {ex.Message}");
        }
    }

    // 传送到撤离点
    public static void TeleportToExtraction()
    {
        try
        {
            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if ((Object)(object)localPlayer == (Object)null) return;

            ExtractionPoint[] points = Object.FindObjectsOfType<ExtractionPoint>();
            if (points == null || points.Length == 0)
            {
                Debug.Log((object)"[传送] 未找到撤离点");
                return;
            }

            // 找到最近的激活撤离点
            ExtractionPoint nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var point in points)
            {
                float dist = Vector3.Distance(localPlayer.transform.position, ((Component)point).transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = point;
                }
            }

            if (nearest != null)
            {
                Vector3 targetPos = ((Component)nearest).transform.position + Vector3.up * 1.5f;
                DoTeleport(localPlayer, targetPos);
                Debug.Log((object)$"[传送] 已传送到撤离点 (距离: {nearestDist:F1}m)");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError((object)$"[传送] 撤离点传送失败: {ex.Message}");
        }
    }

    // 核心传送方法 — 游戏自己的 PhysGrabObject.Teleport（非房主会转发 SetPositionRPC 给主机）
    private static void DoTeleport(GameObject player, Vector3 position)
    {
            PlayerAvatar avatar = player.GetComponent<PlayerAvatar>();
            if (avatar != null)
            {
                NativeGameApi.TeleportPlayer(avatar, position, player.transform.rotation);
            }
            else
            {
                player.transform.position = position;
            }
    }
}
