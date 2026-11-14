using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

public static class Enemies
{
	private static Dictionary<Enemy, int> enemyMaxHealthCache = new Dictionary<Enemy, int>();

	public static List<Enemy> enemyList = new List<Enemy>();

	public static void KillSelectedEnemy(int selectedEnemyIndex, List<Enemy> enemyList, List<string> enemyNames)
	{
		if (selectedEnemyIndex < 0 || selectedEnemyIndex >= enemyList.Count)
		{
			return;
		}
		Enemy val = enemyList[selectedEnemyIndex];
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		try
		{
			VoidAndHaul(val);
			DebugCheats.UpdateEnemyList();
		}
		catch (Exception)
		{
		}
	}

	public static void KillAllEnemies()
	{
		List<Enemy> list = DebugCheats.enemyList;
		if (list == null || list.Count == 0)
		{
			if (NativeGameApi.IsHost())
			{
				NativeGameApi.DestroyAllEnemies();
				DebugCheats.UpdateEnemyList();
			}
			return;
		}
		foreach (Enemy item in new List<Enemy>(list))
		{
			if ((Object)(object)item != (Object)null)
			{
				VoidAndHaul(item);
			}
		}
		DebugCheats.UpdateEnemyList();
	}

	public static void TeleportEnemyToMe(int selectedEnemyIndex, List<Enemy> enemyList, List<string> enemyNames)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		if (selectedEnemyIndex < 0 || selectedEnemyIndex >= enemyList.Count)
		{
			return;
		}
		Enemy val = enemyList[selectedEnemyIndex];
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		try
		{
			GameObject localPlayer = DebugCheats.GetLocalPlayer();
			if ((Object)(object)localPlayer == (Object)null)
			{
				return;
			}
			Vector3 forward = localPlayer.transform.forward;
			Vector3 val2 = localPlayer.transform.position + forward * 1f + Vector3.up * 1.5f;
			PhotonView component = ((Component)val).GetComponent<PhotonView>();
			if (PhotonNetwork.IsConnected && (Object)(object)component != (Object)null && !component.IsMine)
			{
				component.RequestOwnership();
			}
			NativeGameApi.TeleportEnemy(val, val2);
		}
		catch (Exception)
		{
		}
	}

	public static void TeleportEnemyToPlayer(int selectedEnemyIndex, List<Enemy> enemyList, List<string> enemyNames, int targetPlayerIndex, List<object> playerList, List<string> playerNames)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		if (selectedEnemyIndex < 0 || selectedEnemyIndex >= enemyList.Count)
		{
			return;
		}
		Enemy val = enemyList[selectedEnemyIndex];
		if ((Object)(object)val == (Object)null || targetPlayerIndex < 0 || targetPlayerIndex >= playerList.Count)
		{
			return;
		}
		object obj = playerList[targetPlayerIndex];
		if (obj == null)
		{
			return;
		}
		try
		{
			GameObject val2 = (GameObject)((obj is GameObject) ? obj : null);
			Vector3 val3;
			if (val2 != null)
			{
				val3 = val2.transform.position + Vector3.up * 1.5f;
			}
			else
			{
				MonoBehaviour val4 = (MonoBehaviour)((obj is MonoBehaviour) ? obj : null);
				if (val4 != null)
				{
					val3 = ((Component)val4).transform.position + Vector3.up * 1.5f;
				}
				else
				{
					FieldInfo field = obj.GetType().GetField("transform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (!(field != null))
					{
						return;
					}
					object value = field.GetValue(obj);
					Transform val5 = (Transform)((value is Transform) ? value : null);
					if (!((Object)(object)val5 != (Object)null))
					{
						return;
					}
					val3 = val5.position + Vector3.up * 1.5f;
				}
			}
			PhotonView component = ((Component)val).GetComponent<PhotonView>();
			if (PhotonNetwork.IsConnected && (Object)(object)component != (Object)null && !component.IsMine)
			{
				component.RequestOwnership();
			}
			NativeGameApi.TeleportEnemy(val, val3);
		}
		catch (Exception)
		{
		}
	}

	public static void TeleportEnemyToVoid(int selectedEnemyIndex, List<Enemy> enemyList, List<string> enemyNames)
	{
		if (selectedEnemyIndex < 0 || selectedEnemyIndex >= enemyList.Count)
		{
			return;
		}
		Enemy enemy = enemyList[selectedEnemyIndex];
		if ((Object)(object)enemy == (Object)null)
		{
			return;
		}
		try
		{
			VoidAndHaul(enemy);
		}
		catch (Exception)
		{
		}
	}

	private static readonly Vector3 VoidPos = new Vector3(0f, -500f, 0f);

	private static void VoidAndHaul(Enemy enemy)
	{
		if ((Object)(object)enemy == (Object)null)
		{
			return;
		}
		HashSet<int> before = SnapshotValuableIds();
		if (NativeGameApi.IsHost())
		{
			NativeGameApi.KillEnemy(enemy);
		}
		else
		{
			NativeGameApi.TeleportEnemy(enemy, VoidPos);
		}
		EnemyNavMeshAgent agent = ((Component)enemy).GetComponent<EnemyNavMeshAgent>();
		if ((Object)(object)agent != (Object)null)
		{
			agent.Disable(60f);
		}
		MxLoader.RunCoroutine(HaulNewValuables(before, enemy));
	}

	private static HashSet<int> SnapshotValuableIds()
	{
		HashSet<int> ids = new HashSet<int>();
		try
		{
			ValuableObject[] valuables = UnityEngine.Object.FindObjectsOfType<ValuableObject>();
			for (int i = 0; i < valuables.Length; i++)
			{
				if ((Object)(object)valuables[i] != (Object)null)
				{
					ids.Add(valuables[i].GetInstanceID());
				}
			}
		}
		catch
		{
		}
		return ids;
	}

	private static IEnumerator HaulNewValuables(HashSet<int> before, Enemy enemy)
	{
		for (int i = 0; i < 12; i++)
		{
			yield return new WaitForSeconds(0.25f);
			if (!MaiItemTeleport.TryGetOpenExtractionDrop(out Vector3 dropPos, out Quaternion dropRot, out _))
			{
				continue;
			}
			bool moved = false;
			ValuableObject[] valuables = UnityEngine.Object.FindObjectsOfType<ValuableObject>();
			for (int v = 0; v < valuables.Length; v++)
			{
				ValuableObject valuable = valuables[v];
				if ((Object)(object)valuable == (Object)null || before.Contains(valuable.GetInstanceID()))
				{
					continue;
				}
				if (valuable.GetComponent<PlayerDeathHead>() != null || valuable.GetComponentInParent<PlayerDeathHead>() != null)
				{
					continue;
				}
				MaiItemTeleport.TeleportComponent(valuable, dropPos, dropRot);
				before.Add(valuable.GetInstanceID());
				moved = true;
			}
			if (moved)
			{
				break;
			}
		}
		if (NativeGameApi.IsHost() && (Object)(object)enemy != (Object)null)
		{
			NativeGameApi.TeleportEnemy(enemy, VoidPos);
		}
	}

	// cached reflection for the per-frame ESP/Aimbot health lookups (resolved once per runtime type)
	private static readonly Dictionary<Type, FieldInfo> HealthFieldCache = new Dictionary<Type, FieldInfo>();
	private static readonly Dictionary<Type, FieldInfo> HealthValueFieldCache = new Dictionary<Type, FieldInfo>();

	public static int GetEnemyHealth(Enemy enemy)
	{
		try
		{
			EnemyHealth health = ((Component)enemy).GetComponent<EnemyHealth>();
			object healthObj = health;
			if (healthObj == null)
			{
				FieldInfo field = GetCachedField(HealthFieldCache, ((object)enemy).GetType(), "Health");
				if (field == null)
				{
					return -1;
				}
				healthObj = field.GetValue(enemy);
			}
			if (healthObj == null)
			{
				return -1;
			}
			FieldInfo field2 = GetCachedField(HealthValueFieldCache, healthObj.GetType(), "healthCurrent");
			if (field2 == null)
			{
				return -1;
			}
			return (int)field2.GetValue(healthObj);
		}
		catch (Exception)
		{
			return -1;
		}
	}

	/// <summary>
	/// 通用方法：从 Enemy.Health 对象中读取指定字段（如 healthMax、healthCurrent）
	/// </summary>
	private static int GetEnemyHealthFieldValue(Enemy enemy, string fieldName)
	{
		try
		{
			FieldInfo field = GetCachedField(HealthFieldCache, ((object)enemy).GetType(), "Health");
			if (field == null) return -1;
			object healthObj = field.GetValue(enemy);
			if (healthObj == null) return -1;
			FieldInfo targetField = GetCachedField(HealthValueFieldCache, healthObj.GetType(), fieldName);
			if (targetField == null) return -1;
			return (int)targetField.GetValue(healthObj);
		}
		catch (Exception)
		{
			return -1;
		}
	}

	private static FieldInfo GetCachedField(Dictionary<Type, FieldInfo> cache, Type type, string fieldName)
	{
		if (type == null)
		{
			return null;
		}
		lock (cache)
		{
			if (cache.TryGetValue(type, out FieldInfo cached))
			{
				return cached;
			}
		}
		FieldInfo resolved = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		lock (cache)
		{
			cache[type] = resolved;
		}
		return resolved;
	}

	public static int GetEnemyMaxHealth(Enemy enemy)
	{
		if (enemyMaxHealthCache.TryGetValue(enemy, out var value))
		{
			// 如果观察到更高的血量，更新缓存
			int currentHealth = GetEnemyHealth(enemy);
			if (currentHealth > value)
			{
				enemyMaxHealthCache[enemy] = currentHealth;
				return currentHealth;
			}
			return value;
		}
		// 首次观察：尝试读取 healthMax 字段，否则用当前血量
		int maxHealth = GetEnemyHealthFieldValue(enemy, "health");
		if (maxHealth <= 0)
		{
			maxHealth = GetEnemyHealth(enemy);
		}
		int num = (maxHealth > 0) ? maxHealth : 100;
		enemyMaxHealthCache[enemy] = num;
		return num;
	}

	public static float GetEnemyHealthPercentage(Enemy enemy)
	{
		int enemyHealth = GetEnemyHealth(enemy);
		int enemyMaxHealth = GetEnemyMaxHealth(enemy);
		if (enemyHealth < 0 || enemyMaxHealth <= 0)
		{
			return -1f;
		}
		return (float)enemyHealth / (float)enemyMaxHealth;
	}

	// === 冻结所有敌人 ===
	public static bool freezeAllEnemies = false;

	public static void FreezeAllEnemies()
	{
		freezeAllEnemies = true;
		try
		{
			List<Enemy> list = DebugCheats.enemyList ?? new List<Enemy>();
			foreach (Enemy enemy in list)
			{
				NativeGameApi.FreezeEnemy(enemy, 9999f);
			}
		}
		catch (Exception ex) { Debug.LogWarning("[Enemies] FreezeAll: " + ex.Message); }
	}

	public static void UnfreezeAllEnemies()
	{
		freezeAllEnemies = false;
		try
		{
			List<Enemy> list = DebugCheats.enemyList ?? new List<Enemy>();
			foreach (Enemy enemy in list)
			{
				NativeGameApi.FreezeEnemy(enemy, 0f);
				EnemyNavMeshAgent agent = ((Component)enemy).GetComponent<EnemyNavMeshAgent>();
				if (agent != null)
				{
					agent.Enable();
				}
			}
		}
		catch (Exception ex) { Debug.LogWarning("[Enemies] UnfreezeAll: " + ex.Message); }
	}

	/// <summary>
	/// 每帧调用: 用游戏自己的 Enemy.Freeze 持续冻结新刷出的敌人
	/// </summary>
	public static void UpdateFreeze()
	{
		if (!freezeAllEnemies) return;
		try
		{
			List<Enemy> list = DebugCheats.enemyList;
			if (list == null) return;
			foreach (Enemy enemy in list)
			{
				if ((Object)(object)enemy == (Object)null) continue;
				NativeGameApi.FreezeEnemy(enemy, 30f);
			}
		}
		catch { }
	}

	// === 敌人速度修改 ===
	public static float enemySpeedMultiplier = 1.0f;
	private static bool speedModifyActive = false;

	public static void SetSpeedMultiplier(float multiplier)
	{
		enemySpeedMultiplier = multiplier;
		speedModifyActive = (multiplier != 1.0f);
		if (!speedModifyActive)
		{
			RestoreOriginalSpeeds();
		}
	}

	public static void RestoreOriginalSpeeds()
	{
		try
		{
			List<Enemy> list = DebugCheats.enemyList ?? new List<Enemy>();
			foreach (Enemy enemy in list)
			{
				NativeGameApi.SetEnemySpeed(enemy, 1f);
			}
		}
		catch { }
	}

	/// <summary>
	/// 每帧调用: 用游戏自己的 EnemyNavMeshAgent.OverrideAgent 改速度
	/// </summary>
	public static void UpdateSpeedModify()
	{
		if (!speedModifyActive) return;
		try
		{
			List<Enemy> list = DebugCheats.enemyList;
			if (list == null) return;
			foreach (Enemy enemy in list)
			{
				if ((Object)(object)enemy == (Object)null) continue;
				NativeGameApi.SetEnemySpeed(enemy, enemySpeedMultiplier);
			}
		}
		catch { }
	}
}
