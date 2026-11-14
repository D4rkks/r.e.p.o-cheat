using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 一键全升级工具
/// </summary>
public static class UpgradeHelper
{
    /// <summary>
    /// 全部 13 种升级（v0.4.x 的真实升级字典），走游戏的 PunManager 升级 API + UpdateStat 同步。
    /// </summary>
    public static void MaxAllUpgrades()
    {
        try
        {
            string steamID = MaiPlayerController.GetLocalPlayerSteamID();
            if (string.IsNullOrEmpty(steamID)) return;

            foreach (var upgrade in Upgrades)
            {
                SetUpgrade(steamID, upgrade.Dict, 30, upgrade.Apply);
            }

            Hax2.sliderValueStrength = 30f;
            Hax2.oldSliderValue = 30f;
            Hax2.sliderValue = 30f;
            Hax2.grabRange = 30f;
            Hax2.throwStrength = 30f;
            Hax2.extraJumps = 30;
            Hax2.tumbleLaunch = 20f;
            RebuildLocalGrabPhysics();

            Debug.Log("[UpgradeHelper] All 13 upgrades maxed via PunManager API!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UpgradeHelper] Error: " + ex.Message);
        }
    }

    /// <summary>v0.4.x 真实升级字典名 + 对应 PunManager 立即生效方法（委托在调用时才读取 instance）。</summary>
    private static readonly (string Dict, Action<string, int> Apply)[] Upgrades =
    {
        ("playerUpgradeHealth", (id, v) => PunManager.instance.UpgradePlayerHealth(id, v)),
        ("playerUpgradeStamina", (id, v) => PunManager.instance.UpgradePlayerEnergy(id, v)),
        ("playerUpgradeExtraJump", (id, v) => PunManager.instance.UpgradePlayerExtraJump(id, (int)v)),
        ("playerUpgradeLaunch", (id, v) => PunManager.instance.UpgradePlayerTumbleLaunch(id, (int)v)),
        ("playerUpgradeTumbleClimb", (id, v) => PunManager.instance.UpgradePlayerTumbleClimb(id, v)),
        ("playerUpgradeTumbleWings", (id, v) => PunManager.instance.UpgradePlayerTumbleWings(id, v)),
        ("playerUpgradeSpeed", (id, v) => PunManager.instance.UpgradePlayerSprintSpeed(id, (int)v)),
        ("playerUpgradeCrouchRest", (id, v) => PunManager.instance.UpgradePlayerCrouchRest(id, v)),
        ("playerUpgradeStrength", (id, v) => PunManager.instance.UpgradePlayerGrabStrength(id, (int)v)),
        ("playerUpgradeThrow", (id, v) => PunManager.instance.UpgradePlayerThrowStrength(id, (int)v)),
        ("playerUpgradeRange", (id, v) => PunManager.instance.UpgradePlayerGrabRange(id, (int)v)),
        ("playerUpgradeMapPlayerCount", (id, v) => PunManager.instance.UpgradeMapPlayerCount(id, v)),
        ("playerUpgradeDeathHeadBattery", (id, v) => PunManager.instance.UpgradeDeathHeadBattery(id, v)),
    };

    private static readonly Dictionary<string, FieldInfo> DictFields = new Dictionary<string, FieldInfo>();

    /// <summary>把本地玩家某项升级设到目标等级（走 PunManager.UpgradePlayerX 差值 + UpdateStat 同步）。</summary>
    public static void SetLocalLevel(string dictName, int target, Action<string, int> apply)
    {
        string steamID = MaiPlayerController.GetLocalPlayerSteamID();
        if (string.IsNullOrEmpty(steamID) || PunManager.instance == null)
        {
            return;
        }
        SetUpgrade(steamID, dictName, Mathf.Max(0, target), apply);
    }

    /// <summary>
    /// Vanilla PhysGrabber: grabStrength = 1 + level*0.2, throwStrength = level*0.3, grabRange = 4 + level.
    /// Writing the slider as an absolute (5, 30, …) makes the hold spring yank objects
    /// toward the grab point (usually above) so they launch up on release — carts included.
    /// </summary>
    public static float GameGrabStrength(int level)
    {
        return 1f + Mathf.Max(0, level) * 0.2f;
    }

    public static float GameThrowStrength(int level)
    {
        return Mathf.Max(0, level) * 0.3f;
    }

    public static float GameGrabRange(int level)
    {
        return 4f + Mathf.Max(0, level);
    }

    public static void RebuildLocalGrabPhysics()
    {
        try
        {
            PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
            if (local == null || local.physGrabber == null)
            {
                return;
            }
            local.physGrabber.grabStrength = GameGrabStrength(Mathf.RoundToInt(Hax2.sliderValueStrength));
            local.physGrabber.throwStrength = GameThrowStrength(Mathf.RoundToInt(Hax2.throwStrength));
            local.physGrabber.grabRange = GameGrabRange(Mathf.RoundToInt(Hax2.grabRange));
        }
        catch
        {
        }
    }

    /// <summary>幂等升级：先应用差值（立即生效），再用游戏自己的 UpdateStat 同步字典到所有客户端。</summary>
    private static void SetUpgrade(string steamID, string dictName, int target, Action<string, int> apply)
    {
        StatsManager stats = StatsManager.instance;
        if (stats == null)
        {
            return;
        }
        Dictionary<string, int> dict = GetDict(stats, dictName);
        if (dict == null)
        {
            return;
        }
        int current = dict.TryGetValue(steamID, out int level) ? level : 0;
        if (current == target)
        {
            return;
        }
        apply?.Invoke(steamID, target - current);
        if (PunManager.instance != null && NativeGameApi.IsHost())
        {
            PunManager.instance.UpdateStat(dictName, steamID, target);
        }
    }

    private static Dictionary<string, int> GetDict(StatsManager stats, string dictName)
    {
        if (!DictFields.TryGetValue(dictName, out FieldInfo field) || field == null)
        {
            field = typeof(StatsManager).GetField(dictName, BindingFlags.Instance | BindingFlags.Public);
            DictFields[dictName] = field;
        }
        return field?.GetValue(stats) as Dictionary<string, int>;
    }

    /// <summary>
    /// 核心方法：6 项核心升级走游戏真实 API（幂等，自动同步字典与立即生效）。
    /// </summary>
    public static void ApplyUpgradeViaDictionary(string steamID, int grabStrength, int throwStrength,
        int sprintSpeed, int grabRange, int extraJump, int tumbleLaunch)
    {
        try
        {
            if (string.IsNullOrEmpty(steamID)) return;
            if (PunManager.instance == null || StatsManager.instance == null) return;

            float cap = Mathf.Clamp(Hax2.AdminUpgradeCap, Hax2.AdminUpgradeCapMin, Hax2.AdminUpgradeCapMax);
            SetUpgrade(steamID, "playerUpgradeStrength", Mathf.RoundToInt(Mathf.Clamp(grabStrength, 0, cap)), (id, v) => PunManager.instance.UpgradePlayerGrabStrength(id, (int)v));
            SetUpgrade(steamID, "playerUpgradeThrow", Mathf.RoundToInt(Mathf.Clamp(throwStrength, 0, cap)), (id, v) => PunManager.instance.UpgradePlayerThrowStrength(id, (int)v));
            SetUpgrade(steamID, "playerUpgradeSpeed", Mathf.RoundToInt(Mathf.Clamp(sprintSpeed, 0, cap)), (id, v) => PunManager.instance.UpgradePlayerSprintSpeed(id, (int)v));
            SetUpgrade(steamID, "playerUpgradeRange", Mathf.RoundToInt(Mathf.Clamp(grabRange, 0, cap)), (id, v) => PunManager.instance.UpgradePlayerGrabRange(id, (int)v));
            SetUpgrade(steamID, "playerUpgradeExtraJump", Mathf.RoundToInt(Mathf.Clamp(extraJump, 0, cap)), (id, v) => PunManager.instance.UpgradePlayerExtraJump(id, (int)v));
            SetUpgrade(steamID, "playerUpgradeLaunch", Mathf.RoundToInt(Mathf.Clamp(tumbleLaunch, 0, cap)), (id, v) => PunManager.instance.UpgradePlayerTumbleLaunch(id, (int)v));

            // 直接设置本地物理属性（立即生效，不依赖RPC）
            try
            {
                ApplyLocalPhysicsUpgrades(grabStrength, sprintSpeed, extraJump);
            }
            catch { }

            Debug.Log($"[Upgrade] Applied for {steamID}: GS={grabStrength} TS={throwStrength} SS={sprintSpeed} GR={grabRange} EJ={extraJump} TL={tumbleLaunch}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Upgrade] Apply failed: " + ex.Message);
        }
    }

    /// <summary>
    /// 直接设置本地玩家的物理属性，绕过 RPC 验证
    /// </summary>
    public static void ApplyLocalPhysicsUpgrades(int grabStrength, int sprintSpeed, int extraJump)
    {
        try
        {
            PlayerAvatar localAvatar = SemiFunc.PlayerAvatarLocal();
            if (localAvatar == null) return;

            // Grab/throw/range sliders are upgrade levels. The game converts those
            // to PhysGrabber fields (1+0.2n / 0.3n / 4+n). Do not write the level
            // into grabStrength — that is what launched items and carts on release.
            RebuildLocalGrabPhysics();

            // 速度滑条是升级层数（PunManager 对 SprintSpeed 做 +=），不能把 SprintSpeed
            // 写成绝对值。每 8 秒写成 1 会冲掉商店加成，并和 Super Speed 的 OverrideSpeed 抢值，
            // 再叠上慢走就无法起步疾跑。
            Type pcType = typeof(RunManager).Assembly.GetType("PlayerController");
            if (pcType != null)
            {
                object pcInst = GameHelper.FindObjectOfType(pcType);
                if (pcInst != null)
                {
                    // 尝试设置 extraJump 相关字段
                    var jumpField = pcType.GetField("ExtraJump", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? pcType.GetField("extraJump", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? pcType.GetField("jumpExtra", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? pcType.GetField("JumpExtra", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (jumpField != null)
                    {
                        if (jumpField.FieldType == typeof(int))
                            jumpField.SetValue(pcInst, extraJump);
                        else if (jumpField.FieldType == typeof(float))
                            jumpField.SetValue(pcInst, (float)extraJump);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Upgrade] Local physics apply failed: " + ex.Message);
        }
    }

}

/// <summary>
/// 全物品价值膨胀工具
/// </summary>
public static class ItemInflater
{
    public static void InflateAll(float targetValue = 99999f)
    {
        try
        {
            var items = DebugCheats.valuableObjects;
            if (items == null || items.Count == 0) return;

            int count = 0;
            foreach (object item in items)
            {
                if (item == null) continue;
                try
                {
                    // Check if it's a Unity object that was destroyed
                    UnityEngine.Object unityObj = item as UnityEngine.Object;
                    if (unityObj != null && unityObj == null) continue;

                    PhotonView pv = null;
                    if (item is Component comp)
                        pv = comp.GetComponent<PhotonView>();
                    else
                    {
                        var pvField = item.GetType().GetField("photonView",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (pvField != null)
                            pv = pvField.GetValue(item) as PhotonView;
                    }

                    FieldInfo dollar = item.GetType().GetField("dollarValueCurrent",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    dollar?.SetValue(item, targetValue);
                    if (pv != null && NativeGameApi.IsHost())
                    {
                        pv.RPC("DollarValueSetRPC", RpcTarget.Others, new object[] { targetValue });
                        count++;
                    }
                }
                catch { }
            }
            Debug.Log($"[ItemInflater] Inflated {count} items to ${targetValue}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ItemInflater] Error: " + ex.Message);
        }
    }

    public static void MultiplyAll(float multiplier = 10f)
    {
        try
        {
            var items = DebugCheats.valuableObjects;
            if (items == null || items.Count == 0) return;

            foreach (object item in items)
            {
                if (item == null) continue;
                try
                {
                    UnityEngine.Object unityObj = item as UnityEngine.Object;
                    if (unityObj != null && unityObj == null) continue;

                    FieldInfo valueField = item.GetType().GetField("dollarValueCurrent",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? item.GetType().GetField("dollarValue",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    float currentValue = 0f;
                    if (valueField != null)
                        currentValue = Convert.ToSingle(valueField.GetValue(item));

                    float newValue = currentValue * multiplier;

                    PhotonView pv = null;
                    if (item is Component comp)
                        pv = comp.GetComponent<PhotonView>();
                    else
                    {
                        var pvField = item.GetType().GetField("photonView",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (pvField != null)
                            pv = pvField.GetValue(item) as PhotonView;
                    }

                    if (pv != null && NativeGameApi.IsHost())
                        pv.RPC("DollarValueSetRPC", RpcTarget.Others, new object[] { newValue });
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ItemInflater] Multiply error: " + ex.Message);
        }
    }
}

/// <summary>
/// 搬运目标归零工具
/// </summary>
public static class HaulGoalZero
{
    public static string statusMessage = "";

    public static void ZeroHaulGoal()
    {
        try
        {
            NativeGameApi.LowHaul = true;
            NativeGameApi.ApplyLowHaul();

            RoundDirector instance = RoundDirector.instance;
            if (instance == null)
            {
                statusMessage = L.T("haul.no_round");
                return;
            }

            bool found = false;
            // Search for haul/goal related fields via reflection
            string[] fieldNames = { "haulGoal", "extractionHaulGoal", "goalAmount",
                "shopHaulGoal", "currentHaulGoal", "totalHaulGoal", "requiredHaul" };

            foreach (string name in fieldNames)
            {
                FieldInfo field = typeof(RoundDirector).GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    if (field.FieldType == typeof(int))
                        field.SetValue(instance, 0);
                    else if (field.FieldType == typeof(float))
                        field.SetValue(instance, 0f);
                    found = true;
                    Debug.Log("[HaulGoal] Set " + name + " = 0");
                }
            }

            // Also try property search
            if (!found)
            {
                var allFields = typeof(RoundDirector).GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var f in allFields)
                {
                    string nameLower = f.Name.ToLower();
                    if (nameLower.Contains("haul") || nameLower.Contains("goal") ||
                        nameLower.Contains("target") || nameLower.Contains("quota"))
                    {
                        try
                        {
                            if (f.FieldType == typeof(int))
                            { f.SetValue(instance, 0); found = true; }
                            else if (f.FieldType == typeof(float))
                            { f.SetValue(instance, 0f); found = true; }
                            Debug.Log("[HaulGoal] Found and zeroed: " + f.Name + " (" + f.FieldType + ")");
                        }
                        catch { }
                    }
                }
            }

            statusMessage = found ? L.T("haul.zeroed") : L.T("haul.no_field");
        }
        catch (Exception ex)
        {
            statusMessage = L.T("haul.error", ex.Message);
            Debug.LogWarning("[HaulGoal] Error: " + ex.Message);
        }
    }
}

/// <summary>
/// 物品复制机
/// </summary>
public static class ItemDuplicator
{
    public static bool DuplicateHeldItem()
    {
        try
        {
            object pc = PlayerReflectionCache.PlayerControllerInstance;
            if (pc == null) return false;

            FieldInfo avatarField = pc.GetType().GetField("playerAvatarScript",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (avatarField == null) return false;
            object avatar = avatarField.GetValue(pc);
            if (avatar == null) return false;

            FieldInfo grabberField = avatar.GetType().GetField("physGrabber",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (grabberField == null) return false;
            object grabber = grabberField.GetValue(avatar);
            if (grabber == null) return false;

            FieldInfo grabbedField = grabber.GetType().GetField("grabbed",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (grabbedField == null || !(bool)grabbedField.GetValue(grabber)) return false;

            FieldInfo transformField = grabber.GetType().GetField("grabbedObjectTransform",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (transformField == null) return false;
            Transform grabbedTransform = transformField.GetValue(grabber) as Transform;
            if (grabbedTransform == null) return false;

            string objName = grabbedTransform.gameObject.name;
            if (objName.EndsWith("(Clone)"))
                objName = objName.Substring(0, objName.Length - "(Clone)".Length).Trim();

            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if (localPlayer == null) return false;
            Vector3 spawnPos = localPlayer.transform.position + localPlayer.transform.forward * 1.5f + Vector3.up;

            // Try spawning via MaiItemSpawner
            MaiItemSpawner.SpawnItem(objName, spawnPos);
            Debug.Log("[ItemDuplicator] Duplicated: " + objName);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ItemDuplicator] Error: " + ex.Message);
            return false;
        }
    }
}

/// <summary>
/// 幽灵模式 (Ghost Mode) — 轻量级网络隐身。
/// 
/// 策略（不使用 PhotonNetwork.Destroy，零崩溃风险）：
///   1. 停止 PhotonView 同步 (Synchronization = Off)
///   2. 禁用 PhotonTransformView / PhotonAnimatorView
///   3. 禁用自身所有远程可见的 Renderer 和 Collider
///   4. 广播虚假远离位置 (RPC)，使对方看到「消失」
///   5. 恢复时还原所有组件状态并广播正确位置
/// 
/// 效果：对方世界里玩家消失（或冻在地图外），
/// 但本地仍然正常游玩、可以攻击/拾取。
/// </summary>
public static class StealthMode
{
    public static bool isEnabled = false;
    public static string statusMessage = "";

    // 备份状态（用于恢复）
    private static ViewSynchronization _originalSync = (ViewSynchronization)3;
    private static readonly List<ComponentState> _disabledRenderers = new List<ComponentState>();
    private static readonly List<ComponentState> _disabledColliders = new List<ComponentState>();
    private static bool _hasBackup = false;

    private struct ComponentState
    {
        public Component component;
        public bool wasEnabled;
    }

    public static void Apply()
    {
        try
        {
            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if (localPlayer == null) return;

            PhotonView pv = GetPlayerPhotonView(localPlayer);

            if (isEnabled)
                EnableGhostMode(localPlayer, pv);
            else
                DisableGhostMode(localPlayer, pv);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GhostMode] Error: " + ex.Message);
            statusMessage = "错误: " + ex.Message;
        }
    }

    private static void EnableGhostMode(GameObject localPlayer, PhotonView pv)
    {
        _disabledRenderers.Clear();
        _disabledColliders.Clear();
        _hasBackup = true;

        // ── 1. 停止 PhotonView 同步 ──
        if (pv != null)
        {
            _originalSync = pv.Synchronization;
            pv.Synchronization = (ViewSynchronization)0; // Off
        }

        // ── 2. 禁用 PhotonTransformView / PhotonAnimatorView ──
        DisablePhotonSyncComponents(localPlayer);

        // ── 3. 广播虚假位置 — 把自己「传送」到地图外 ──
        BroadcastFakePosition(localPlayer, pv);

        // ── 4. 禁用远程可见组件 ──
        // 注意：只禁用 Collider 让自己无法被碰到，Renderer 保留本地可见
        // 因为其他玩家看到的是我们最后的同步位置（已在地图外）
        DisableCollidersForGhost(localPlayer);

        statusMessage = "👻 幽灵模式 ON — 对方世界中已消失";
        Debug.Log("[GhostMode] 已启用 — 同步关闭, 虚假位置已广播, 碰撞器已禁用");
    }

    private static void DisableGhostMode(GameObject localPlayer, PhotonView pv)
    {
        // ── 1. 恢复 PhotonView 同步 ──
        if (pv != null && _hasBackup)
        {
            pv.Synchronization = _originalSync;
        }

        // ── 2. 恢复 PhotonTransformView / PhotonAnimatorView ──
        EnablePhotonSyncComponents(localPlayer);

        // ── 3. 恢复碰撞器 ──
        RestoreColliders();

        // ── 4. 广播真实位置 ──
        BroadcastRealPosition(localPlayer, pv);

        _hasBackup = false;
        statusMessage = "幽灵模式 OFF — 已恢复可见";
        Debug.Log("[GhostMode] 已禁用 — 同步恢复, 真实位置已广播");
    }

    // ─── 辅助方法 ──────────────────────────────────────────

    private static PhotonView GetPlayerPhotonView(GameObject localPlayer)
    {
        PhotonView pv = localPlayer.GetComponent<PhotonView>();
        if (pv == null)
        {
            var avatar = localPlayer.GetComponent<PlayerAvatar>();
            if (avatar != null)
            {
                var pvField = avatar.GetType().GetField("photonView",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pvField != null)
                    pv = pvField.GetValue(avatar) as PhotonView;
            }
        }
        return pv;
    }

    private static void DisablePhotonSyncComponents(GameObject localPlayer)
    {
        try
        {
            // 禁用 PhotonTransformView
            var transformViews = localPlayer.GetComponentsInChildren<PhotonTransformView>(true);
            foreach (var tv in transformViews)
            {
                if ((UnityEngine.Object)(object)tv != (UnityEngine.Object)null && ((Behaviour)tv).enabled)
                {
                    ((Behaviour)tv).enabled = false;
                }
            }

            // 禁用 PhotonAnimatorView（通过反射，因为可能不在当前程序集中）
            Type photonAnimViewType = typeof(PhotonView).Assembly.GetType("Photon.Pun.PhotonAnimatorView");
            if (photonAnimViewType != null)
            {
                var animViews = localPlayer.GetComponentsInChildren(photonAnimViewType, true);
                foreach (var av in animViews)
                {
                    if (av != null && av is Behaviour b && b.enabled)
                    {
                        b.enabled = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GhostMode] 禁用同步组件警告: " + ex.Message);
        }
    }

    private static void EnablePhotonSyncComponents(GameObject localPlayer)
    {
        try
        {
            var transformViews = localPlayer.GetComponentsInChildren<PhotonTransformView>(true);
            foreach (var tv in transformViews)
            {
                if ((UnityEngine.Object)(object)tv != (UnityEngine.Object)null)
                    ((Behaviour)tv).enabled = true;
            }

            Type photonAnimViewType = typeof(PhotonView).Assembly.GetType("Photon.Pun.PhotonAnimatorView");
            if (photonAnimViewType != null)
            {
                var animViews = localPlayer.GetComponentsInChildren(photonAnimViewType, true);
                foreach (var av in animViews)
                {
                    if (av != null && av is Behaviour b)
                        b.enabled = true;
                }
            }
        }
        catch { }
    }

    private static void DisableCollidersForGhost(GameObject localPlayer)
    {
        try
        {
            Collider[] colliders = localPlayer.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if ((UnityEngine.Object)(object)col == (UnityEngine.Object)null) continue;
                _disabledColliders.Add(new ComponentState { component = col, wasEnabled = col.enabled });
                col.enabled = false;
            }
        }
        catch { }
    }

    private static void RestoreColliders()
    {
        foreach (var state in _disabledColliders)
        {
            try
            {
                if (state.component != null && state.component is Collider col)
                    col.enabled = state.wasEnabled;
            }
            catch { }
        }
        _disabledColliders.Clear();
    }

    private static void BroadcastFakePosition(GameObject localPlayer, PhotonView pv)
    {
        try
        {
            if (pv == null || !PhotonNetwork.IsConnected) return;

            // 发送一次性的位置 RPC，把自己「传送」到地图外
            Vector3 fakePos = new Vector3(9999f, -999f, 9999f);

            var avatar = localPlayer.GetComponent<PlayerAvatar>();
            if (avatar != null && avatar.photonView != null)
            {
                try
                {
                    // 保存真实位置
                    Vector3 realPos = localPlayer.transform.position;
                    Quaternion realRot = localPlayer.transform.rotation;

                    // 临时恢复同步并设置假位置
                    pv.Synchronization = (ViewSynchronization)3;
                    localPlayer.transform.position = fakePos;

                    // 使用协程延迟关闭同步，确保 Photon 至少序列化一帧
                    var runner = UnityEngine.Object.FindObjectOfType<MonoBehaviour>();
                    if (runner != null)
                    {
                        runner.StartCoroutine(
                            DelayedSyncOff(pv, localPlayer, realPos, realRot));
                    }
                    else
                    {
                        // 后备方案：直接关闭（可能不会广播）
                        pv.Synchronization = (ViewSynchronization)0;
                        localPlayer.transform.position = realPos;
                        localPlayer.transform.rotation = realRot;
                    }

                    Debug.Log("[GhostMode] 已广播虚假位置 → (9999, -999, 9999)");
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GhostMode] 广播虚假位置失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 延迟关闭同步并恢复真实位置，确保 Photon 有时间序列化假位置
    /// </summary>
    private static IEnumerator DelayedSyncOff(PhotonView pv, GameObject localPlayer, Vector3 realPos, Quaternion realRot)
    {
        // 等待几帧让 Photon 序列化并发送假位置
        yield return new WaitForSeconds(0.3f);

        if (pv != null)
            pv.Synchronization = (ViewSynchronization)0;
        if (localPlayer != null)
        {
            localPlayer.transform.position = realPos;
            localPlayer.transform.rotation = realRot;
        }
    }

    private static void BroadcastRealPosition(GameObject localPlayer, PhotonView pv)
    {
        try
        {
            if (pv == null || !PhotonNetwork.IsConnected) return;

            // 恢复同步后，Photon 会自动同步正确位置
            // 可以额外发一个位置 RPC 加速
            Debug.Log("[GhostMode] 同步恢复 — 真实位置将自动广播");
        }
        catch { }
    }
}

/// <summary>
/// 自动完成回合
/// </summary>
public static class AutoCompleteRound
{
    public static void Execute()
    {
        try
        {
            // 激活所有撤离点（游戏自己的 ExtractionPointActivate → 同步 RPC）
            foreach (ExtractionPoint ep in UnityEngine.Object.FindObjectsOfType<ExtractionPoint>())
            {
                if (ep == null) continue;
                PhotonView pv = ep.GetComponent<PhotonView>();
                if (pv != null && RoundDirector.instance != null)
                {
                    RoundDirector.instance.ExtractionPointActivate(pv.ViewID);
                }
            }

            // 通过游戏自己的通关流程完成关卡（进入下一关/商店）
            if (RunManager.instance != null)
            {
                RunManager.instance.ChangeLevel(_completedLevel: true, _levelFailed: false, RunManager.ChangeLevelType.Normal);
            }

            Debug.Log("[AutoComplete] Round auto-completed via game API!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AutoComplete] Error: " + ex.Message);
        }
    }
}

/// <summary>
/// 门/陷阱禁用器
/// </summary>
public static class TrapDisabler
{
    public static int DisableAllTraps()
    {
        return NativeGameApi.DisableAllTraps();
    }
}

/// <summary>
/// 全队传送到撤离点
/// </summary>
public static class TeamTeleport
{
    public static void TeleportAllToExtraction()
    {
        try
        {
            // 先激活所有撤离点
            MiscFeatures.ForceActivateAllExtractionPoints();

            // 找到最近的撤离点
            var points = UnityEngine.Object.FindObjectsOfType<ExtractionPoint>();
            if (points == null || points.Length == 0)
            {
                Debug.LogWarning("[TeamTP] No extraction points found!");
                return;
            }

            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            Vector3 refPos = (localPlayer != null) ? localPlayer.transform.position : Vector3.zero;

            ExtractionPoint nearest = null;
            float minDist = float.MaxValue;
            foreach (var ep in points)
            {
                float dist = Vector3.Distance(refPos, ((Component)ep).transform.position);
                if (dist < minDist) { minDist = dist; nearest = ep; }
            }
            if (nearest == null) return;

            Vector3 targetPos = ((Component)nearest).transform.position + Vector3.up * 1.5f;

            // 遍历所有玩家并传送
            List<PlayerAvatar> players = SemiFunc.PlayerGetList();
            if (players == null) return;

            int count = 0;
            foreach (var avatar in players)
            {
                if ((UnityEngine.Object)(object)avatar == (UnityEngine.Object)null) continue;
                try
                {
                    // 分散位置防止重叠
                    Vector3 offset = new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0, UnityEngine.Random.Range(-1.5f, 1.5f));
                    Vector3 pos = targetPos + offset;

                    NativeGameApi.TeleportPlayer(avatar, pos, ((Component)avatar).transform.rotation);
                    count++;
                }
                catch { }
            }
            Debug.Log($"[TeamTP] Teleported {count} players to extraction point.");
        }
        catch (Exception ex) { Debug.LogWarning("[TeamTP] Error: " + ex.Message); }
    }
}

/// <summary>
/// Keep selected players alive. Local Hurt() is owner-only, so true
/// invincibility is a local godMode + Hurt skip; others get HealOther /
/// Revive, and the host also drops HurtOther RPCs aimed at them.
/// </summary>
public static class PlayerAura
{
    public static bool isEnabled = false;
    public static int targetActorNumber = -1; // -1 = everyone
    private static float lastHealTime = 0f;
    private const float HealInterval = 0.1f;
    private static FieldInfo _godModeField;

    public static void Toggle()
    {
        isEnabled = !isEnabled;
        if (!isEnabled)
        {
            OnDisabled();
        }
    }

    public static void OnDisabled()
    {
        isEnabled = false;
        targetActorNumber = -1;
        RestoreLocalGodMode();
    }

    public static int ActorOf(PlayerAvatar avatar)
    {
        if ((UnityEngine.Object)avatar == null)
        {
            return 0;
        }
        PhotonView pv = avatar.photonView;
        if (pv == null)
        {
            return 0;
        }
        if (pv.Owner != null)
        {
            return pv.Owner.ActorNumber;
        }
        return pv.OwnerActorNr;
    }

    public static bool Covers(PlayerAvatar avatar)
    {
        if (!isEnabled || (UnityEngine.Object)avatar == null)
        {
            return false;
        }
        if (targetActorNumber == -1)
        {
            return true;
        }
        int actor = ActorOf(avatar);
        return actor > 0 && actor == targetActorNumber;
    }

    public static bool BlocksDamage(PlayerHealth health)
    {
        if (!isEnabled || (UnityEngine.Object)health == null)
        {
            return false;
        }
        PlayerAvatar avatar = ((Component)health).GetComponent<PlayerAvatar>();
        if ((UnityEngine.Object)avatar == null)
        {
            avatar = ((Component)health).GetComponentInParent<PlayerAvatar>();
        }
        return Covers(avatar);
    }

    public static void Update()
    {
        if (!isEnabled)
        {
            return;
        }
        if (Time.time - lastHealTime < HealInterval)
        {
            return;
        }
        lastHealTime = Time.time;

        try
        {
            List<PlayerAvatar> players = SemiFunc.PlayerGetList();
            if (players == null)
            {
                return;
            }

            foreach (PlayerAvatar avatar in players)
            {
                if ((UnityEngine.Object)avatar == null)
                {
                    continue;
                }
                if (!Covers(avatar))
                {
                    continue;
                }
                if (!((Component)avatar).gameObject.activeInHierarchy)
                {
                    continue;
                }
                Protect(avatar);
            }
        }
        catch
        {
        }
    }

    private static void Protect(PlayerAvatar avatar)
    {
        try
        {
            PlayerHealth health = avatar.playerHealth;
            bool local = avatar.photonView != null && avatar.photonView.IsMine;
            if (local && health != null)
            {
                SetGodModeField(health, true);
                health.InvincibleSet(2f);
            }

            bool dead = false;
            try
            {
                FieldInfo deadSet = typeof(PlayerAvatar).GetField("deadSet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo disabled = typeof(PlayerAvatar).GetField("isDisabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                dead = (deadSet?.GetValue(avatar) is bool d && d) || (disabled?.GetValue(avatar) is bool off && off);
            }
            catch
            {
            }
            if (dead)
            {
                Players.HealRevivePlayer(avatar, SemiFunc.PlayerGetName(avatar) ?? "");
            }
            if (health == null)
            {
                return;
            }
            int max = Mathf.Max(100, Players.GetPlayerMaxHealth(health));
            int current = Players.GetPlayerHealth(avatar);
            if (dead || current < max)
            {
                health.HealOther(max, effect: false);
                PhotonView pv = avatar.photonView;
                if (NativeGameApi.IsHost() && SemiFunc.IsMultiplayer() && pv != null)
                {
                    pv.RPC("UpdateHealthRPC", RpcTarget.All, max, max, false, false);
                }
            }
        }
        catch
        {
        }
    }

    private static void SetGodModeField(PlayerHealth health, bool value)
    {
        if (_godModeField == null)
        {
            _godModeField = typeof(PlayerHealth).GetField("godMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        _godModeField?.SetValue(health, value);
    }

    private static void RestoreLocalGodMode()
    {
        try
        {
            PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
            if (local != null && local.playerHealth != null && !Hax2.godModeActive)
            {
                SetGodModeField(local.playerHealth, false);
            }
        }
        catch
        {
        }
    }
}

/// <summary>
/// 为指定玩家应用升级
/// </summary>
public static class UpgradeForPlayer
{
    /// <summary>
    /// 为指定玩家应用升级 — 使用 StatsManager 字典直接修改
    /// </summary>
    public static void ApplyUpgrades(string steamID, int grabStrength, int throwStrength,
        int sprintSpeed, int grabRange, int extraJump, int tumbleLaunch)
    {
        try
        {
            if (string.IsNullOrEmpty(steamID)) return;
            UpgradeHelper.ApplyUpgradeViaDictionary(steamID, grabStrength, throwStrength, sprintSpeed, grabRange, extraJump, tumbleLaunch);

            // 也直接设置目标玩家的 physGrabber
            try
            {
                PlayerAvatar avatar = SemiFunc.PlayerAvatarGetFromSteamID(steamID);
                if (avatar != null && avatar.physGrabber != null)
                {
                    avatar.physGrabber.grabStrength = UpgradeHelper.GameGrabStrength(grabStrength);
                    avatar.physGrabber.throwStrength = UpgradeHelper.GameThrowStrength(throwStrength);
                    avatar.physGrabber.grabRange = UpgradeHelper.GameGrabRange(grabRange);
                }
            }
            catch { }
        }
        catch (Exception ex) { Debug.LogWarning("[Upgrade] Error: " + ex.Message); }
    }
}
