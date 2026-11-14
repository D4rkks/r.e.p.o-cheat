using System;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 反踢保护: 阻止房主踢出自己
/// </summary>
public static class AntiKick
{
    public static bool isEnabled = false;
    private static Harmony _harmony;
    private static bool _patched = false;

    public static void Enable()
    {
        isEnabled = true;
        if (!_patched)
        {
            try
            {
                _harmony = new Harmony("dark_cheat.antikick");

                // Patch PhotonNetwork.CloseConnection
                var closeMethod = typeof(PhotonNetwork).GetMethod("CloseConnection",
                    BindingFlags.Static | BindingFlags.Public);
                if (closeMethod != null)
                {
                    var prefix = typeof(AntiKick).GetMethod("Prefix_CloseConnection",
                        BindingFlags.Static | BindingFlags.Public);
                    _harmony.Patch(closeMethod, new HarmonyMethod(prefix));
                }

                // Patch PhotonNetwork.LeaveRoom — 阻止非自愿退出
                var leaveMethod = typeof(PhotonNetwork).GetMethod("LeaveRoom",
                    BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(bool) }, null);
                if (leaveMethod != null)
                {
                    var prefix2 = typeof(AntiKick).GetMethod("Prefix_LeaveRoom",
                        BindingFlags.Static | BindingFlags.Public);
                    _harmony.Patch(leaveMethod, new HarmonyMethod(prefix2));
                }

                // Patch PhotonNetwork.Disconnect
                var disconnectMethod = typeof(PhotonNetwork).GetMethod("Disconnect",
                    BindingFlags.Static | BindingFlags.Public);
                if (disconnectMethod != null)
                {
                    var prefix3 = typeof(AntiKick).GetMethod("Prefix_Disconnect",
                        BindingFlags.Static | BindingFlags.Public);
                    _harmony.Patch(disconnectMethod, new HarmonyMethod(prefix3));
                }

                _patched = true;
                Debug.Log("[AntiKick] Harmony patches applied.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AntiKick] Patch failed: " + ex.Message);
            }
        }
    }

    public static void Disable()
    {
        isEnabled = false;
        if (_patched && _harmony != null)
        {
            try
            {
                _harmony.UnpatchAll(_harmony.Id);
                _patched = false;
                Debug.Log("[AntiKick] Harmony patches removed.");
            }
            catch { }
        }
    }

    public static void Toggle()
    {
        if (isEnabled) Disable();
        else Enable();
    }

    // 内部标志：当用户主动离开时设为true
    public static bool _voluntaryLeave = false;

    /// <summary>
    /// 用户主动离开房间（允许）
    /// 注意：_voluntaryLeave 在 Photon 回调中重置，而非立即重置，因为 LeaveRoom/Disconnect 是异步的
    /// </summary>
    public static void VoluntaryLeaveRoom()
    {
        _voluntaryLeave = true;
        PhotonNetwork.LeaveRoom(false);
        // 不在此处重置，等 OnLeftRoom 回调触发后重置
    }

    public static void VoluntaryDisconnect()
    {
        _voluntaryLeave = true;
        PhotonNetwork.Disconnect();
        // 不在此处重置，等 OnDisconnected 回调触发后重置
    }

    /// <summary>
    /// 在 Photon 回调中调用此方法重置主动离开标志
    /// </summary>
    public static void ResetVoluntaryFlag()
    {
        _voluntaryLeave = false;
    }

    // === Harmony Prefixes ===

    public static bool Prefix_CloseConnection(Player kickPlayer)
    {
        if (!isEnabled) return true;
        if (kickPlayer != null && kickPlayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("[AntiKick] 阻止被踢! CloseConnection 拦截成功");
            return false; // 阻止执行
        }
        return true; // 踢别人正常执行
    }

    public static bool Prefix_LeaveRoom()
    {
        if (!isEnabled) return true;
        if (_voluntaryLeave) return true; // 主动离开
        // 允许游戏正常的房间切换 / 场景加载流程
        if (!PhotonNetwork.InRoom) return true;
        if (PhotonNetwork.NetworkClientState != ClientState.Joined) return true;
        Debug.Log("[AntiKick] 阻止非自愿 LeaveRoom!");
        return false;
    }

    public static bool Prefix_Disconnect()
    {
        if (!isEnabled) return true;
        if (_voluntaryLeave) return true;
        // 允许游戏正常的连接切换 / 重连流程
        if (!PhotonNetwork.InRoom) return true;
        if (PhotonNetwork.NetworkClientState != ClientState.Joined) return true;
        Debug.Log("[AntiKick] 阻止非自愿 Disconnect!");
        return false;
    }
}
