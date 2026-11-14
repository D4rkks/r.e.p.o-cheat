using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 自动闪避 v2 — 基于敌人 Animator 状态机检测攻击动画 + 瞬移传送闪避。
/// 
/// 改进亮点：
///   1. Animator 缓存 — 不再每帧 GetComponentInChildren
///   2. 攻击动画检测 — 检查 AnimatorStateInfo 的 Tag/Name，避免敌人仅走近就误触发
///   3. 传送闪避 — 瞬移到敌人身后安全位置，零延迟（替代旧版的 Tumble/力推）
///   4. 无 Animator 降级 — 若敌人无 Animator，仅在极近距离触发（兜底）
/// </summary>
public static class AutoDodge
{
    // ─── 公开配置 ──────────────────────────────────────────
    public static bool isAutoDodgeEnabled = false;
    public static float dodgeDistance = 8f;            // Animator 模式: 攻击检测半径
    public static float dodgeCooldown = 1.5f;          // 闪避冷却（秒）
    public static float teleportDistance = 5f;          // 传送距离（传送到敌人身后多远）
    public static float noAnimatorPanicRange = 3f;     // 无 Animator 的紧急触发距离

    // ─── 统计 ──────────────────────────────────────────────
    public static int dodgeCount = 0;
    public static string lastDodgeInfo = "";

    // ─── 内部状态 ──────────────────────────────────────────
    private static float _lastDodgeTime = 0f;
    private static readonly Dictionary<int, Animator> _animatorCache = new Dictionary<int, Animator>();
    private static float _cacheCleanTimer = 0f;

    // 攻击动画关键字（Tag 或 Name 中包含这些字符串即视为攻击状态）
    private static readonly string[] AttackStateNames = new string[]
    {
        "Attack", "attack", "Bite", "bite", "Swing", "swing",
        "Lunge", "lunge", "Slash", "slash", "Strike", "strike",
        "Hit", "hit", "Charge", "charge", "Slam", "slam",
        "Grab", "grab", "Pounce", "pounce", "Stomp", "stomp"
    };

    private static readonly string[] AttackTags = new string[]
    {
        "Attack", "attack", "Combat", "combat"
    };

    // ─── 主循环 ────────────────────────────────────────────

    /// <summary>
    /// 在 Hax2.Update() 中调用
    /// </summary>
    public static void UpdateAutoDodge()
    {
        if (!isAutoDodgeEnabled) return;
        if (Time.time - _lastDodgeTime < dodgeCooldown) return;

        // 定期清理缓存（15秒间隔）
        _cacheCleanTimer += Time.deltaTime;
        if (_cacheCleanTimer > 15f)
        {
            _cacheCleanTimer = 0f;
            CleanAnimatorCache();
        }

        try
        {
            GameObject localPlayer = DebugCheats.GetLocalPlayer();
            if ((UnityEngine.Object)(object)localPlayer == (UnityEngine.Object)null) return;

            Vector3 playerPos = localPlayer.transform.position;

            Enemy[] enemies = SceneCache.GetObjects<Enemy>(0.25f);
            if (enemies == null || enemies.Length == 0) return;

            // 第一优先级：检查是否有敌人在攻击范围内且正在攻击
            Enemy bestThreat = null;
            float bestThreatDist = dodgeDistance;
            bool threatIsAttacking = false;

            foreach (Enemy enemy in enemies)
            {
                if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null) continue;

                float dist = Vector3.Distance(playerPos, ((Component)enemy).transform.position);
                if (dist > dodgeDistance) continue;

                Animator anim = GetCachedAnimator(enemy);

                if (anim != null)
                {
                    // 有 Animator: 检查攻击状态
                    if (IsEnemyAttacking(anim))
                    {
                        if (!threatIsAttacking || dist < bestThreatDist)
                        {
                            bestThreat = enemy;
                            bestThreatDist = dist;
                            threatIsAttacking = true;
                        }
                    }
                    else if (!threatIsAttacking && dist < noAnimatorPanicRange && dist < bestThreatDist)
                    {
                        // 有 Animator 但不攻击，只在极近距离作为备选
                        bestThreat = enemy;
                        bestThreatDist = dist;
                    }
                }
                else
                {
                    // 无 Animator: 仅在极近距离触发（兜底）
                    if (!threatIsAttacking && dist < noAnimatorPanicRange && dist < bestThreatDist)
                    {
                        bestThreat = enemy;
                        bestThreatDist = dist;
                    }
                }
            }

            if (bestThreat != null)
            {
                PerformTeleportDodge(localPlayer, bestThreat, bestThreatDist, threatIsAttacking);
                _lastDodgeTime = Time.time;
                dodgeCount++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoDodge] 错误: {ex.Message}");
        }
    }

    // ─── Animator 检测 ─────────────────────────────────────

    /// <summary>
    /// 检查敌人是否正在播放攻击动画
    /// </summary>
    private static bool IsEnemyAttacking(Animator anim)
    {
        if (anim == null || !anim.isActiveAndEnabled) return false;

        try
        {
            // 检查基础层 (Layer 0)
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            // 方式1: 检查 Tag
            foreach (string tag in AttackTags)
            {
                if (stateInfo.IsTag(tag)) return true;
            }

            // 方式2: 检查 State Name (Hash 匹配)
            foreach (string name in AttackStateNames)
            {
                if (stateInfo.IsName(name)) return true;
            }

            // 方式3: 检查过渡中的目标状态
            if (anim.IsInTransition(0))
            {
                AnimatorStateInfo nextState = anim.GetNextAnimatorStateInfo(0);
                foreach (string tag in AttackTags)
                {
                    if (nextState.IsTag(tag)) return true;
                }
                foreach (string name in AttackStateNames)
                {
                    if (nextState.IsName(name)) return true;
                }
            }

            // 方式4: 检查更多层级 (如果有)
            if (anim.layerCount > 1)
            {
                for (int layer = 1; layer < Mathf.Min(anim.layerCount, 3); layer++)
                {
                    AnimatorStateInfo layerState = anim.GetCurrentAnimatorStateInfo(layer);
                    foreach (string tag in AttackTags)
                    {
                        if (layerState.IsTag(tag)) return true;
                    }
                }
            }
        }
        catch { }

        return false;
    }

    // ─── 传送闪避 ──────────────────────────────────────────

    /// <summary>
    /// 执行传送闪避 — 瞬移到敌人身后的安全位置
    /// </summary>
    private static void PerformTeleportDodge(GameObject player, Enemy enemy, float currentDist, bool isAttacking)
    {
        try
        {
            Transform enemyT = ((Component)enemy).transform;
            Transform playerT = player.transform;

            // 计算闪避目标位置（多种策略）
            Vector3 safePos = CalculateSafePosition(playerT, enemyT);

            // 确保目标位置不在地面以下
            if (Physics.Raycast(safePos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
            {
                safePos.y = hit.point.y + 0.1f;
            }

            // 执行传送
            playerT.position = safePos;

            // 如果有 Rigidbody，清除速度防止惯性
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if ((UnityEngine.Object)(object)rb == (UnityEngine.Object)null)
                rb = player.GetComponentInChildren<Rigidbody>();
            if ((UnityEngine.Object)(object)rb != (UnityEngine.Object)null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 同步网络位置
            SyncNetworkPosition(player, safePos);

            string reason = isAttacking ? "攻击动画" : "极近距离";
            lastDodgeInfo = $"[{reason}] 闪避 {((UnityEngine.Object)enemy).name} (距离:{currentDist:F1}m) → 传送 {teleportDistance:F0}m";
            Debug.Log($"[AutoDodge] {lastDodgeInfo}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoDodge] 传送闪避失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 计算安全传送位置
    /// 策略: 
    ///   1. 首选: 敌人身后（敌人面朝方向的反方向）
    ///   2. 如果身后有墙: 尝试两侧（左/右垂直方向）
    ///   3. 如果全堵: 远离敌人方向
    /// </summary>
    private static Vector3 CalculateSafePosition(Transform playerT, Transform enemyT)
    {
        Vector3 enemyPos = enemyT.position;
        Vector3 playerPos = playerT.position;
        Vector3 enemyForward = enemyT.forward;

        // 候选位置
        Vector3[] candidates = new Vector3[]
        {
            // 敌人身后
            enemyPos - enemyForward * teleportDistance,
            // 敌人左侧
            enemyPos - enemyT.right * teleportDistance,
            // 敌人右侧
            enemyPos + enemyT.right * teleportDistance,
            // 远离敌人方向
            playerPos + (playerPos - enemyPos).normalized * teleportDistance,
            // 玩家身后
            playerPos - playerT.forward * teleportDistance,
        };

        // 选择第一个没有被阻挡的位置
        foreach (Vector3 candidate in candidates)
        {
            if (!Physics.Linecast(playerPos + Vector3.up * 0.5f, candidate + Vector3.up * 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                return candidate;
            }
        }

        // 全部被阻挡，使用远离方向（强制传送）
        return playerPos + (playerPos - enemyPos).normalized * teleportDistance;
    }

    /// <summary>
    /// 同步传送后的网络位置
    /// </summary>
    private static void SyncNetworkPosition(GameObject player, Vector3 newPosition)
    {
        try
        {
            if (!PhotonNetwork.IsConnected) return;

            PhotonView pv = player.GetComponent<PhotonView>();
            if ((UnityEngine.Object)(object)pv == (UnityEngine.Object)null)
            {
                var avatar = player.GetComponent<PlayerAvatar>();
                if (avatar != null) pv = avatar.photonView;
            }

            // 获取 PhotonTransformView 并触发立即同步
            if ((UnityEngine.Object)(object)pv != (UnityEngine.Object)null)
            {
                var transformView = player.GetComponent<PhotonTransformView>();
                if ((UnityEngine.Object)(object)transformView == (UnityEngine.Object)null)
                    transformView = player.GetComponentInChildren<PhotonTransformView>();

                // 强制设置网络位置（通过反射如有必要）
                if ((UnityEngine.Object)(object)transformView != (UnityEngine.Object)null)
                {
                    var storedField = typeof(PhotonTransformView).GetField("m_StoredPosition",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (storedField != null)
                        storedField.SetValue(transformView, newPosition);
                }
            }
        }
        catch { /* 网络同步是可选的，失败不影响本地传送 */ }
    }

    // ─── 缓存管理 ──────────────────────────────────────────

    private static Animator GetCachedAnimator(Enemy enemy)
    {
        int id = ((UnityEngine.Object)enemy).GetInstanceID();
        if (_animatorCache.TryGetValue(id, out Animator cached))
        {
            if (cached != null) return cached;
            _animatorCache.Remove(id); // 已销毁，移除
        }

        // 新查找并缓存
        Animator anim = ((Component)enemy).GetComponentInChildren<Animator>();
        if (anim != null)
            _animatorCache[id] = anim;
        return anim;
    }

    private static void CleanAnimatorCache()
    {
        var toRemove = new List<int>();
        foreach (var kvp in _animatorCache)
        {
            if (kvp.Value == null) toRemove.Add(kvp.Key);
        }
        foreach (int key in toRemove)
            _animatorCache.Remove(key);
    }

    /// <summary>
    /// 重置所有状态（场景切换时调用）
    /// </summary>
    public static void Reset()
    {
        _animatorCache.Clear();
        _lastDodgeTime = 0f;
        _cacheCleanTimer = 0f;
        dodgeCount = 0;
        lastDodgeInfo = "";
    }
}
