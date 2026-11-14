using System;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 关卡调整 — 通过游戏自己的 RunManager.ChangeLevel 切换场景
/// （旧的字段暴力猜测 + 伪造 LoadLevelRPC 全部无效，已废弃）。
/// 注意：ChangeLevel 仅房主/单人可触发（游戏自身的 MasterClient 守卫）。
/// </summary>
public static class LevelAdjust
{
    public static string statusMessage = "";

    /// <summary>完成当前关卡，进入下一关（游戏自己的通关流程）。</summary>
    public static void CompleteLevel()
    {
        SetLevel(RunManager.ChangeLevelType.Normal, completed: true);
    }

    /// <summary>前往商店。</summary>
    public static void GoShop()
    {
        SetLevel(RunManager.ChangeLevelType.Shop, completed: true);
    }

    /// <summary>返回大厅。</summary>
    public static void GoLobby()
    {
        SetLevel(RunManager.ChangeLevelType.LobbyMenu, completed: false);
    }

    /// <summary>返回主菜单。</summary>
    public static void GoMainMenu()
    {
        SetLevel(RunManager.ChangeLevelType.MainMenu, completed: false);
    }

    /// <summary>进入教程。</summary>
    public static void GoTutorial()
    {
        SetLevel(RunManager.ChangeLevelType.Tutorial, completed: false);
    }

    /// <summary>按游戏关卡资源名切换（走 /level 同源的 debugLevel + ChangeLevel）。</summary>
    public static void GoNamedLevel(string levelName)
    {
        NativeGameApi.GoToLevel(levelName);
        statusMessage = NativeGameApi.LastStatus;
    }

    private static void SetLevel(RunManager.ChangeLevelType type, bool completed)
    {
        try
        {
            if (!NativeGameApi.IsHost())
            {
                statusMessage = "host only";
                return;
            }
            if (RunManager.instance == null)
            {
                statusMessage = "RunManager 未就绪";
                return;
            }
            RunManager.instance.ChangeLevel(completed, false, type);
            statusMessage = "已请求切换: " + type;
        }
        catch (Exception ex)
        {
            statusMessage = "切换失败: " + ex.Message;
            Debug.LogWarning("[LevelAdjust] " + ex.Message);
        }
    }
}
