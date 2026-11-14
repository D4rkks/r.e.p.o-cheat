using System;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 商店作弊: 免费购买 + 刷钱
/// </summary>
public static class ShopHack
{
    public static bool freeShopEnabled = false;
    public static int moneySpawnAmount = 45000;

    /// <summary>
    /// 在指定位置生成金钱物品
    /// </summary>
    public static void SpawnMoney(Vector3 position, int value)
    {
        try
        {
            // 使用已有的 ItemSpawner.SpawnMoney（如果可用）
            var spawnerType = typeof(Hax2).Assembly.GetType("r.e.p.o_cheat.ItemSpawner");
            if (spawnerType != null)
            {
                var method = spawnerType.GetMethod("SpawnMoney", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, new object[] { position, value });
                    return;
                }
            }
            // Fallback: 直接使用 AssetManager + NetworkInstantiate
            DirectSpawnMoney(position, value);
        }
        catch (Exception ex) { Debug.LogWarning("[ShopHack] SpawnMoney error: " + ex.Message); }
    }

    private static void DirectSpawnMoney(Vector3 position, int value)
    {
        try
        {
            if (!SemiFunc.IsMultiplayer()) return;
            var amInstance = AssetManager.instance;
            if (amInstance == null) return;
            var surplusField = amInstance.GetType().GetField("surplusValuableSmall",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (surplusField == null) return;
            GameObject prefab = surplusField.GetValue(amInstance) as GameObject;
            if (prefab == null) return;

            var instantiateMethod = typeof(PhotonNetwork).GetMethod("NetworkInstantiate",
                BindingFlags.Static | BindingFlags.NonPublic, null,
                new Type[3] { typeof(InstantiateParameters), typeof(bool), typeof(bool) }, null);
            var levelPrefixField = typeof(PhotonNetwork).GetField("currentLevelPrefix",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (instantiateMethod == null || levelPrefixField == null) return;

            object levelPrefix = levelPrefixField.GetValue(null);
            object[] data = new object[1] { value };
            InstantiateParameters param = new InstantiateParameters(
                "Valuables/" + prefab.name, position, Quaternion.identity, (byte)0, data,
                (byte)levelPrefix, null, PhotonNetwork.LocalPlayer, PhotonNetwork.ServerTimestamp);
            instantiateMethod.Invoke(null, new object[3] { param, true, false });
        }
        catch (Exception ex) { Debug.LogWarning("[ShopHack] DirectSpawnMoney error: " + ex.Message); }
    }

    /// <summary>
    /// 在本地玩家位置生成金钱
    /// </summary>
    public static void SpawnMoneyAtPlayer(int value = 45000)
    {
        GameObject player = DebugCheats.GetLocalPlayer();
        if (player == null) return;
        SpawnMoney(player.transform.position + Vector3.up * 1.5f, value);
    }

    /// <summary>
    /// 免费购买：把本轮货币设为极大值，走游戏自己的 SetRunStatSet 同步路径
    /// （旧的 ShopItem/ItemShop 暴力字段扫描全部无效，已废弃）。
    /// </summary>
    public static int SetAllShopItemsFree()
    {
        try
        {
            if (!NativeGameApi.IsHost())
            {
                Debug.Log("[ShopHack] host only");
                return 0;
            }
            SemiFunc.StatSetRunCurrency(9999999);
            Debug.Log("[ShopHack] Shop effectively free: run currency set to 9,999,999");
            return 1;
        }
        catch (Exception ex) { Debug.LogWarning("[ShopHack] SetFree error: " + ex.Message); }
        return 0;
    }

    /// <summary>
    /// 直接修改本轮团队金币（游戏自己的 SetRunStatSet 同步路径，替代旧的暴力字段扫描）。
    /// </summary>
    public static bool AddMoney(int amount)
    {
        try
        {
            if (!NativeGameApi.IsHost())
            {
                Debug.Log("[ShopHack] host only");
                return false;
            }
            int current = SemiFunc.StatGetRunCurrency();
            SemiFunc.StatSetRunCurrency(current + amount);
            Debug.Log($"[ShopHack] Run currency: {current} → {current + amount}");
            return true;
        }
        catch (Exception ex) { Debug.LogWarning("[ShopHack] AddMoney error: " + ex.Message); }
        return false;
    }
}
