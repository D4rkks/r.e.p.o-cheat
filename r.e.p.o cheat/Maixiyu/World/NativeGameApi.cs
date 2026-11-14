using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// v0.4.4.3 native game API wrappers. Replaces removed types (EnemySpawner) and
/// stale RPC forgeries with the same paths the in-game DebugCommandHandler uses.
/// </summary>
public static class NativeGameApi
{
	public static bool HideGrabber;
	public static bool EasyGrabEnemies;
	public static bool NoEnemySpawnPause;
	public static bool SpawnClose;
	public static bool UnlimitedFps;
	public static bool HideItemLabels;
	public static bool NoCameraShake;
	public static bool InstantGunBuildup;
	public static bool FeatherFall;
	public static bool SlowWalk;
	public static bool LowHaul;
	public static bool CheapShop;
	public static bool FillValuables;
	public static bool SuperSpeed;
	public static bool NoDeathPit;
	public static bool InfiniteHeadEnergy;
	public static string LastStatus = "";

	private static readonly BindingFlags InstAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	public static bool IsSolo()
	{
		try { return !SemiFunc.IsMultiplayer(); }
		catch { return true; }
	}

	public static bool IsHost()
	{
		try
		{
			if (!PhotonNetwork.InRoom)
			{
				return true;
			}
			return ShadowHostMode.IsTrueMasterClient();
		}
		catch
		{
			try { return !SemiFunc.IsMultiplayer() || SemiFunc.IsMasterClient(); }
			catch { return true; }
		}
	}

	public static bool IsGuest()
	{
		return !IsHost();
	}

	public static string RoleKey()
	{
		if (IsSolo()) return "role.solo";
		return IsHost() ? "role.host" : "role.guest";
	}

	public static List<EnemySetup> GetEnemySetups()
	{
		var result = new List<EnemySetup>();
		try
		{
			EnemyDirector director = EnemyDirector.instance;
			if (director == null)
			{
				return result;
			}
			if (director.enemiesDifficulty1 != null) result.AddRange(director.enemiesDifficulty1);
			if (director.enemiesDifficulty2 != null) result.AddRange(director.enemiesDifficulty2);
			if (director.enemiesDifficulty3 != null) result.AddRange(director.enemiesDifficulty3);
		}
		catch (Exception ex)
		{
			LastStatus = "enemy list: " + ex.Message;
		}
		return result.Where(s => (UnityEngine.Object)s != null && !((UnityEngine.Object)s).name.Contains("Enemy Group")).ToList();
	}

	public static int SpawnEnemy(EnemySetup setup, Vector3 position, int count)
	{
		if ((UnityEngine.Object)setup == null || setup.spawnObjects == null)
		{
			LastStatus = "invalid EnemySetup";
			return 0;
		}
		if (!IsHost())
		{
			LastStatus = "host only";
			return 0;
		}
		int spawned = 0;
		count = Mathf.Clamp(count, 1, 10);
		try
		{
			bool multi = SemiFunc.IsMultiplayer();
			for (int n = 0; n < count; n++)
			{
				Vector3 pos = position + UnityEngine.Random.insideUnitSphere * (n * 0.4f);
				pos.y = position.y;
				foreach (PrefabRef spawnObject in setup.spawnObjects)
				{
					if (spawnObject == null || !spawnObject.IsValid())
					{
						continue;
					}
					GameObject go;
					if (multi)
					{
						go = PhotonNetwork.InstantiateRoomObject(spawnObject.ResourcePath, pos, Quaternion.identity, 0);
					}
					else
					{
						go = UnityEngine.Object.Instantiate(spawnObject.Prefab, pos, Quaternion.identity);
					}
					if (go == null)
					{
						continue;
					}
					EnemyParent parent = go.GetComponent<EnemyParent>();
					if (parent != null)
					{
						SetMember(parent, "SetupDone", true);
						SetMember(parent, "firstSpawnPointUsed", true);
					}
					Enemy enemy = go.GetComponentInChildren<Enemy>();
					if (enemy != null)
					{
						enemy.EnemyTeleported(pos);
					}
					spawned++;
				}
			}
			LastStatus = "spawned " + spawned;
		}
		catch (Exception ex)
		{
			LastStatus = "spawn: " + ex.Message;
		}
		return spawned;
	}

	public static int DestroyAllEnemies()
	{
		int count = 0;
		try
		{
			if (!IsHost())
			{
				LastStatus = "host only";
				return 0;
			}
			List<EnemyParent> list = GetSpawnedParents();
			foreach (EnemyParent parent in list)
			{
				if ((UnityEngine.Object)parent == null)
				{
					continue;
				}
				count++;
				RemoveFromSpawned(parent);
				if (SemiFunc.IsMultiplayer())
				{
					PhotonNetwork.Destroy(parent.gameObject);
				}
				else
				{
					UnityEngine.Object.Destroy(parent.gameObject);
				}
			}
			LastStatus = "destroyed " + count;
		}
		catch (Exception ex)
		{
			LastStatus = "destroy: " + ex.Message;
		}
		return count;
	}

	public static int DespawnAllEnemies()
	{
		int count = 0;
		try
		{
			if (!IsHost())
			{
				LastStatus = "host only";
				return 0;
			}
			foreach (EnemyParent parent in GetSpawnedParents())
			{
				if ((UnityEngine.Object)parent == null)
				{
					continue;
				}
				parent.SpawnedTimerSet(0f);
				parent.DespawnedTimerSet(10f);
				count++;
			}
			LastStatus = "despawned " + count;
		}
		catch (Exception ex)
		{
			LastStatus = "despawn: " + ex.Message;
		}
		return count;
	}

	public static void KillEnemy(Enemy enemy)
	{
		if ((UnityEngine.Object)enemy == null)
		{
			return;
		}
		try
		{
			EnemyHealth health = enemy.GetComponent<EnemyHealth>();
			if (health != null)
			{
				health.Hurt(9999, Vector3.zero);
				return;
			}
			object healthObj = typeof(Enemy).GetField("Health", InstAll)?.GetValue(enemy);
			if (healthObj != null)
			{
				MethodInfo hurt = healthObj.GetType().GetMethod("Hurt", InstAll);
				hurt?.Invoke(healthObj, new object[] { 9999, Vector3.zero });
			}
		}
		catch (Exception ex)
		{
			LastStatus = "kill: " + ex.Message;
		}
	}

	public static void FreezeEnemy(Enemy enemy, float seconds)
	{
		if ((UnityEngine.Object)enemy == null)
		{
			return;
		}
		try
		{
			enemy.Freeze(seconds);
			EnemyNavMeshAgent agent = enemy.GetComponent<EnemyNavMeshAgent>();
			if (agent != null)
			{
				agent.Disable(seconds);
			}
		}
		catch
		{
		}
	}

	public static void SetNoAggro(bool enabled)
	{
		try
		{
			string steamId = GetLocalSteamId();
			if (string.IsNullOrEmpty(steamId))
			{
				LastStatus = "no steam id";
				return;
			}
			ToggleStringList(EnemyDirector.instance, "debugNoVision", steamId, enabled);
			if (DebugCommandHandler.instance != null)
			{
				ToggleStringList(DebugCommandHandler.instance, "enemyNoVision", steamId, enabled);
			}
			PhotonView punView = GetMember<PhotonView>(PunManager.instance, "photonView");
			if (SemiFunc.IsMultiplayer() && PunManager.instance != null && punView != null)
			{
				punView.RPC("TesterNoAggroCommandRPC", RpcTarget.MasterClient, steamId, enabled);
			}
			LastStatus = enabled ? (IsGuest() ? "no-aggro local" : "no-aggro on") : "no-aggro off";
		}
		catch (Exception ex)
		{
			LastStatus = "no-aggro: " + ex.Message;
		}
	}

	public static void ApplyDirectorFlags()
	{
		try
		{
			EnemyDirector director = EnemyDirector.instance;
			if (director == null)
			{
				return;
			}
			SetMember(director, "debugEasyGrab", EasyGrabEnemies);
			SetMember(director, "debugNoSpawnIdlePause", NoEnemySpawnPause);
			SetMember(director, "debugNoSpawnedPause", NoEnemySpawnPause);
			SetMember(director, "debugSpawnClose", SpawnClose);
		}
		catch
		{
		}
	}

	public static int DisableAllTraps()
	{
		int count = 0;
		try
		{
			foreach (Trap trap in UnityEngine.Object.FindObjectsOfType<Trap>())
			{
				if (trap == null)
				{
					continue;
				}
				trap.trapTriggered = true;
				trap.enabled = false;
				count++;
			}
			LastStatus = "traps " + count;
		}
		catch (Exception ex)
		{
			LastStatus = "traps: " + ex.Message;
		}
		return count;
	}

	public static int RevealMap()
	{
		int count = 0;
		try
		{
			foreach (MapModule module in UnityEngine.Object.FindObjectsOfType<MapModule>(true))
			{
				if (module != null && !module.gameObject.activeSelf)
				{
					module.gameObject.SetActive(true);
					count++;
				}
			}
			Type[] mapTypes =
			{
				typeof(DirtFinderMapFloor),
				typeof(DirtFinderMapWall),
				typeof(DirtFinderMapDoor),
				typeof(DirtFinderMapEnemy)
			};
			foreach (Type type in mapTypes)
			{
				foreach (UnityEngine.Object obj in UnityEngine.Object.FindObjectsOfType(type, true))
				{
					Component comp = obj as Component;
					if (comp != null && !comp.gameObject.activeSelf)
					{
						comp.gameObject.SetActive(true);
						count++;
					}
				}
			}
			if (Map.Instance != null)
			{
				foreach (object valuable in DebugCheats.valuableObjects)
				{
					ValuableObject vo = valuable as ValuableObject;
					if ((UnityEngine.Object)vo != null)
					{
						Map.Instance.AddValuable(vo);
					}
				}
			}
			LastStatus = "map +" + count;
		}
		catch (Exception ex)
		{
			LastStatus = "map: " + ex.Message;
		}
		return count;
	}

	public static void ApplyHideGrabber()
	{
		try
		{
			PhysGrabber grabber = PhysGrabber.instance;
			if (grabber == null)
			{
				return;
			}
			GameObject visual = GetMember<GameObject>(grabber, "physGrabPointVisual1");
			object beam = GetMember<object>(grabber, "physGrabBeamComponent");
			LineRenderer line = beam != null ? GetMember<LineRenderer>(beam, "lineRenderer") : null;
			if (HideGrabber)
			{
				if (visual != null)
				{
					visual.SetActive(false);
				}
				if (line != null)
				{
					line.enabled = false;
				}
			}
			else if (visual != null)
			{
				visual.SetActive(true);
			}
		}
		catch
		{
		}
	}

	public static void ToggleCinematicHud()
	{
		try
		{
			if (GameDirector.instance != null)
			{
				GameDirector.instance.CommandRecordingDirectorToggle();
				LastStatus = "cinematic toggled";
			}
		}
		catch (Exception ex)
		{
			LastStatus = "cinematic: " + ex.Message;
		}
	}

	public static void ApplyUnlimitedFps()
	{
		try
		{
			if (GameDirector.instance == null)
			{
				return;
			}
			GameDirector.instance.CommandSetFPS(UnlimitedFps ? 0 : 60);
		}
		catch
		{
		}
	}

	public static List<string> GetLevelDisplayNames()
	{
		var names = new List<string>();
		try
		{
			RunManager run = RunManager.instance;
			if (run == null)
			{
				return names;
			}
			void Add(IEnumerable<Level> levels)
			{
				if (levels == null)
				{
					return;
				}
				foreach (Level level in levels)
				{
					if ((UnityEngine.Object)level != null)
					{
						string n = ((UnityEngine.Object)level).name;
						if (!string.IsNullOrEmpty(n) && !names.Contains(n))
						{
							names.Add(n);
						}
					}
				}
			}
			Add(run.levels);
			Add(run.levelArena);
			Add(run.levelShop);
			names.AddRange(new[] { "next", "shop", "lobby", "lobby menu", "recording", "refresh", "random" });
		}
		catch
		{
		}
		return names;
	}

	public static bool GoToLevel(string levelName)
	{
		try
		{
			RunManager run = RunManager.instance;
			if (run == null)
			{
				LastStatus = "RunManager missing";
				return false;
			}
			if (!IsHost())
			{
				LastStatus = "host only";
				return false;
			}
			string key = (levelName ?? "").ToLower().Replace("level - ", "").Trim();
			switch (key)
			{
				case "lobby menu":
					run.ChangeLevel(false, false, RunManager.ChangeLevelType.LobbyMenu);
					break;
				case "next":
					run.ChangeLevel(true, SemiFunc.RunIsArena());
					break;
				case "random":
					run.ChangeLevel(false, false, RunManager.ChangeLevelType.RunLevel);
					break;
				case "recording":
					run.ChangeLevel(false, false, RunManager.ChangeLevelType.Recording);
					break;
				case "refresh":
					run.RestartScene();
					break;
				case "shop":
					run.ChangeLevel(false, false, RunManager.ChangeLevelType.Shop);
					break;
				case "lobby":
					SetMember(run, "debugLevel", run.levelLobby);
					run.ChangeLevel(false, false);
					SetMember(run, "debugLevel", null);
					break;
				default:
				{
					Level match = null;
					IEnumerable<Level> all = Enumerable.Empty<Level>();
					if (run.levels != null) all = all.Concat(run.levels);
					if (run.levelArena != null) all = all.Concat(run.levelArena);
					if (run.levelShop != null) all = all.Concat(run.levelShop);
					foreach (Level level in all)
					{
						if ((UnityEngine.Object)level == null)
						{
							continue;
						}
						string n = ((UnityEngine.Object)level).name;
						if (n.Equals(levelName, StringComparison.OrdinalIgnoreCase) ||
							n.Replace("Level - ", "").Equals(key, StringComparison.OrdinalIgnoreCase))
						{
							match = level;
							break;
						}
					}
					if (match == null)
					{
						LastStatus = "unknown level";
						return false;
					}
					SetMember(run, "debugLevel", match);
					run.ChangeLevel(false, false);
					SetMember(run, "debugLevel", null);
					break;
				}
			}
			LastStatus = "level " + levelName;
			return true;
		}
		catch (Exception ex)
		{
			LastStatus = "level: " + ex.Message;
			return false;
		}
	}

	public static List<string> GetItemNames()
	{
		var names = new List<string>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			AddItemNames(names, seen, StatsManager.instance != null ? StatsManager.instance.itemDictionary : null);
			Item[] fromResources = Resources.LoadAll<Item>("ScriptableObjects");
			AddItemAssets(names, seen, fromResources);
			Item[] allAssets = Resources.FindObjectsOfTypeAll<Item>();
			AddItemAssets(names, seen, allAssets);
			ShopManager shop = ShopManager.instance;
			if (shop != null)
			{
				AddItemAssets(names, seen, shop.potentialItems);
				AddItemAssets(names, seen, shop.potentialItemConsumables);
				AddItemAssets(names, seen, shop.potentialItemUpgrades);
				AddItemAssets(names, seen, shop.potentialItemHealthPacks);
			}
			names.Sort(StringComparer.OrdinalIgnoreCase);
			AddItemName(names, seen, "ValuableCubeBall");
			AddItemName(names, seen, "Cube");
		}
		catch
		{
		}
		return names;
	}

	private static void AddItemNames(List<string> names, HashSet<string> seen, Dictionary<string, Item> dictionary)
	{
		if (dictionary == null)
		{
			return;
		}
		foreach (KeyValuePair<string, Item> pair in dictionary)
		{
			string n = pair.Key;
			if (string.IsNullOrEmpty(n) && pair.Value != null)
			{
				n = ((UnityEngine.Object)pair.Value).name;
			}
			AddItemName(names, seen, n);
		}
	}

	private static void AddItemAssets(List<string> names, HashSet<string> seen, IEnumerable<Item> items)
	{
		if (items == null)
		{
			return;
		}
		foreach (Item item in items)
		{
			if ((UnityEngine.Object)item == null)
			{
				continue;
			}
			AddItemName(names, seen, ((UnityEngine.Object)item).name);
		}
	}

	private static void AddItemName(List<string> names, HashSet<string> seen, string n)
	{
		if (string.IsNullOrEmpty(n) || !seen.Add(n))
		{
			return;
		}
		names.Add(n);
	}

	public static bool SpawnItemNative(string itemName, Vector3 position)
	{
		if (IsCubeItemName(itemName))
		{
			return SpawnCosmeticCube(CosmeticFeatures.TokenRarity, position);
		}
		try
		{
			Item item = FindItem(itemName);
			if ((UnityEngine.Object)item == null || item.prefab == null || !item.prefab.IsValid())
			{
				LastStatus = "item not found";
				return false;
			}
			if (SemiFunc.IsMultiplayer())
			{
				if (IsHost())
				{
					PhotonNetwork.InstantiateRoomObject(item.prefab.ResourcePath, position, Quaternion.identity, 0);
				}
				else
				{
					PhotonNetwork.Instantiate(item.prefab.ResourcePath, position, Quaternion.identity, 0);
				}
			}
			else
			{
				UnityEngine.Object.Instantiate(item.prefab.Prefab, position, Quaternion.identity);
			}
			LastStatus = IsGuest() ? "spawned (owned) " + itemName : "spawned " + itemName;
			return true;
		}
		catch (Exception ex)
		{
			LastStatus = "item spawn: " + ex.Message;
			return false;
		}
	}

	public static bool IsCubeItemName(string itemName)
	{
		if (string.IsNullOrEmpty(itemName))
		{
			return false;
		}
		string n = itemName.Replace(" ", "");
		return n.IndexOf("Cube", StringComparison.OrdinalIgnoreCase) >= 0
			|| n.IndexOf("魔方", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static bool SpawnCosmeticCube(SemiFunc.Rarity rarity, Vector3 position)
	{
		try
		{
			if (SemiFunc.IsMultiplayer() && !IsHost())
			{
				LastStatus = L.T("role.host_only");
				return false;
			}
			ValuableDirector vd = ValuableDirector.instance;
			if (vd == null || vd.cosmeticWorldObjectSetups == null)
			{
				LastStatus = "no cube setups";
				return false;
			}
			int index = (int)rarity;
			if (index < 0 || index >= vd.cosmeticWorldObjectSetups.Count)
			{
				index = 0;
			}
			ValuableDirector.CosmeticWorldObjectSetup setup = vd.cosmeticWorldObjectSetups[index];
			if (setup == null || setup.prefab == null)
			{
				LastStatus = "cube prefab missing";
				return false;
			}
			Quaternion rot = Quaternion.identity;
			if (SemiFunc.IsMultiplayer())
			{
				PhotonNetwork.InstantiateRoomObject(setup.prefab.ResourcePath, position, rot, 0);
			}
			else
			{
				UnityEngine.Object.Instantiate(setup.prefab.Prefab, position, rot);
			}
			LastStatus = "cube " + rarity;
			return true;
		}
		catch (Exception ex)
		{
			LastStatus = "cube: " + ex.Message;
			return false;
		}
	}

	public static void FillBattery(ItemBattery battery)
	{
		if ((UnityEngine.Object)battery == null)
		{
			return;
		}
		try
		{
			if (battery.batteryLife <= 0f)
			{
				battery.batteryLife = 100f;
			}
			BatteryKeepAlive.ForceFill(battery, IsHost());
			battery.ChargeBattery(battery.gameObject, 9999f);
		}
		catch
		{
			try
			{
				battery.batteryLife = 100f;
				battery.SetBatteryLife(100);
			}
			catch
			{
			}
		}
	}

	public static void TeleportPlayer(PlayerAvatar avatar, Vector3 position, Quaternion rotation)
	{
		if ((UnityEngine.Object)avatar == null)
		{
			return;
		}
		try
		{
			PlayerTumble tumble = GetMember<PlayerTumble>(avatar, "tumble");
			PhysGrabObject body = GetMember<PhysGrabObject>(tumble, "physGrabObject");
			if (body != null)
			{
				body.Teleport(position, rotation);
			}
			else
			{
				avatar.Spawn(position, rotation);
			}
			PhotonView view = avatar.photonView;
			if (view != null && view.IsMine && global::PlayerController.instance != null)
			{
				global::PlayerController.instance.transform.position = position;
				global::PlayerController.instance.transform.rotation = rotation;
			}
			LastStatus = "teleported";
		}
		catch (Exception ex)
		{
			LastStatus = "tp: " + ex.Message;
		}
	}

	public static void TeleportEnemy(Enemy enemy, Vector3 position)
	{
		if ((UnityEngine.Object)enemy == null)
		{
			return;
		}
		try
		{
			PhysGrabObject body = MaiItemTeleport.ResolvePhysGrabObject(enemy);
			if (body == null)
			{
				body = enemy.GetComponentInChildren<PhysGrabObject>(true);
			}
			if (IsGuest() && body != null)
			{
				body.Teleport(position, Quaternion.identity);
				LastStatus = "enemy tp requested";
				return;
			}
			enemy.EnemyTeleported(position);
			EnemyNavMeshAgent agent = enemy.GetComponent<EnemyNavMeshAgent>();
			if (agent != null)
			{
				agent.Disable(0.4f);
				agent.Warp(position, true);
			}
			LastStatus = "enemy tp";
		}
		catch (Exception ex)
		{
			LastStatus = "enemy tp: " + ex.Message;
		}
	}

	public static void SetEnemySpeed(Enemy enemy, float multiplier)
	{
		if ((UnityEngine.Object)enemy == null)
		{
			return;
		}
		try
		{
			EnemyNavMeshAgent agent = enemy.GetComponent<EnemyNavMeshAgent>();
			if (agent == null)
			{
				return;
			}
			float speed = GetMember<float>(agent, "DefaultSpeed");
			float accel = GetMember<float>(agent, "DefaultAcceleration");
			if (speed <= 0f)
			{
				speed = 1f;
			}
			if (accel <= 0f)
			{
				accel = 8f;
			}
			agent.OverrideAgent(speed * Mathf.Max(0.01f, multiplier), accel, 2f);
		}
		catch
		{
		}
	}

	public static void SetPlayerMaxHealth(PlayerHealth health, int newMax)
	{
		if ((UnityEngine.Object)health == null)
		{
			return;
		}
		newMax = Mathf.Max(1, newMax);
		SetMember(health, "maxHealth", newMax);
		health.Heal(newMax, false);
	}

	public static bool ChargeHeldItem()
	{
		try
		{
			PhysGrabber grabber = PhysGrabber.instance;
			if (grabber == null)
			{
				LastStatus = "no grabber";
				return false;
			}
			PhysGrabObject grabbed = GetMember<PhysGrabObject>(grabber, "grabbedPhysGrabObject");
			if (grabbed == null)
			{
				LastStatus = "not holding item";
				return false;
			}
			ItemBattery battery = grabbed.GetComponent<ItemBattery>() ?? grabbed.GetComponentInChildren<ItemBattery>();
			if (battery == null)
			{
				LastStatus = "item has no battery";
				return false;
			}
			FillBattery(battery);
			battery.ChargeBattery(grabber.gameObject, 9999f);
			LastStatus = "charged held item";
			return true;
		}
		catch (Exception ex)
		{
			LastStatus = "charge: " + ex.Message;
			return false;
		}
	}

	public static void GiveCrown()
	{
		try
		{
			string steamId = GetLocalSteamId();
			if (string.IsNullOrEmpty(steamId) || PunManager.instance == null)
			{
				LastStatus = "crown unavailable";
				return;
			}
			if (!IsHost())
			{
				LastStatus = "host only";
				return;
			}
			PunManager.instance.CrownPlayerSync(steamId);
			LastStatus = "crown requested";
		}
		catch (Exception ex)
		{
			LastStatus = "crown: " + ex.Message;
		}
	}

	public static void UnlockExtractionPoints()
	{
		try
		{
			if (!IsHost())
			{
				LastStatus = "host only";
				return;
			}
			if (RoundDirector.instance == null)
			{
				LastStatus = "no round";
				return;
			}
			RoundDirector.instance.ExtractionPointsUnlock();
			LastStatus = "extraction unlocked";
		}
		catch (Exception ex)
		{
			LastStatus = "extract: " + ex.Message;
		}
	}

	public static void RequestActivateExtraction()
	{
		try
		{
			RoundDirector director = RoundDirector.instance;
			if (director == null)
			{
				LastStatus = "no round";
				return;
			}
			int requested = 0;
			List<GameObject> list = GetMember<List<GameObject>>(director, "extractionPointList");
			if (list != null)
			{
				foreach (GameObject go in list)
				{
					if (go == null || !go.activeInHierarchy)
					{
						continue;
					}
					PhotonView view = go.GetComponent<PhotonView>();
					if (view != null)
					{
						director.RequestExtractionPointActivation(view.ViewID);
						requested++;
					}
				}
			}
			if (requested == 0)
			{
				foreach (ExtractionPoint point in UnityEngine.Object.FindObjectsOfType<ExtractionPoint>())
				{
					PhotonView view = point.GetComponent<PhotonView>();
					if (view != null)
					{
						director.RequestExtractionPointActivation(view.ViewID);
						requested++;
					}
				}
			}
			LastStatus = requested > 0 ? "extraction requested " + requested : "no extraction points";
		}
		catch (Exception ex)
		{
			LastStatus = "extract request: " + ex.Message;
		}
	}

	public static void SetRunLives(int lives)
	{
		try
		{
			if (!IsHost())
			{
				LastStatus = "host only";
				return;
			}
			SemiFunc.StatSetRunLives(Mathf.Clamp(lives, 0, 99));
			LastStatus = "lives " + lives;
		}
		catch (Exception ex)
		{
			LastStatus = "lives: " + ex.Message;
		}
	}

	public static void AddCosmeticTokens(int count)
	{
		CosmeticFeatures.AddTokens(count);
	}

	public static void ApplyHideItemLabels()
	{
		SetMember(DebugCommandHandler.instance, "hideItemLabels", HideItemLabels);
	}

	public static void ApplyNoCameraShake()
	{
		try
		{
			if (GameplayManager.instance == null)
			{
				return;
			}
			float value = NoCameraShake ? 0f : 1f;
			GameplayManager.instance.OverrideCameraShake(value, 2f);
			GameplayManager.instance.OverrideCameraNoise(value, 2f);
		}
		catch
		{
		}
	}

	public static void ApplyInstantGunBuildup()
	{
		if (!InstantGunBuildup)
		{
			return;
		}
		try
		{
			foreach (ItemGun gun in UnityEngine.Object.FindObjectsOfType<ItemGun>())
			{
				if (gun == null)
				{
					continue;
				}
				PhysGrabObject phys = gun.GetComponent<PhysGrabObject>();
				if (phys == null || phys.playerGrabbing == null)
				{
					continue;
				}
				bool local = false;
				foreach (PhysGrabber grabber in phys.playerGrabbing)
				{
					if (grabber != null && grabber.isLocal)
					{
						local = true;
						break;
					}
				}
				if (local)
				{
					gun.buildUpTime = 0.01f;
				}
			}
		}
		catch
		{
		}
	}

	private static bool _hadFeather;
	private static bool _hadSuperSpeed;

	public static void ApplyFeather()
	{
		try
		{
			global::PlayerController pc = global::PlayerController.instance;
			if (pc == null)
			{
				return;
			}
			if (FeatherFall)
			{
				pc.Feather(1.1f);
				_hadFeather = true;
				return;
			}
			if (_hadFeather)
			{
				SetMember(pc, "featherTimer", 0f);
				if (pc.rb != null)
				{
					pc.rb.useGravity = true;
				}
				_hadFeather = false;
			}
		}
		catch
		{
		}
	}

	public static void ApplySlowWalk()
	{
		try
		{
			// debugSlow scales walk to 0.1x. That lands under the game's 0.1
			// idle-force gate, so velocity stays ~0 and sprint never starts
			// (needs rb.velocity > 0.01). Clear it while sprinting / Super Speed.
			SetMember(global::PlayerController.instance, "debugSlow", WantDebugSlow());
		}
		catch
		{
		}
	}

	public static void ApplyLowHaul()
	{
		try
		{
			RoundDirector round = RoundDirector.instance;
			if (round == null)
			{
				return;
			}
			SetMember(round, "debugLowHaul", LowHaul);
			if (LowHaul)
			{
				SetMember(round, "haulGoal", 0);
			}
		}
		catch
		{
		}
	}

	public static void ApplyCheapShop()
	{
		try
		{
			ShopManager shop = ShopManager.instance;
			if (shop == null)
			{
				return;
			}
			SetMember(shop, "itemValueMultiplier", CheapShop ? 0.01f : 4f);
			SetMember(shop, "upgradeValueIncrease", CheapShop ? 0f : 0.5f);
		}
		catch
		{
		}
	}

	public static void ApplyFillValuables()
	{
		try
		{
			ValuableDirector director = ValuableDirector.instance;
			if (director == null)
			{
				return;
			}
			SetMember(director, "valuableDebug", FillValuables ? ValuableDirector.ValuableDebug.All : ValuableDirector.ValuableDebug.Normal);
		}
		catch
		{
		}
	}

	public static void ReviveLocal(PlayerAvatar avatar)
	{
		if ((UnityEngine.Object)avatar == null)
		{
			return;
		}
		try
		{
			SetMember(avatar, "deadSet", false);
			SetMember(avatar, "isDisabled", false);
			avatar.gameObject.SetActive(true);
			if (avatar.playerAvatarVisuals != null)
			{
				avatar.playerAvatarVisuals.gameObject.SetActive(true);
			}
			if (avatar.photonView != null && avatar.photonView.IsMine && global::PlayerController.instance != null)
			{
				Collider col = GetMember<Collider>(global::PlayerController.instance, "col");
				if (col != null)
				{
					col.enabled = true;
				}
				if (avatar.playerTransform != null)
				{
					if (avatar.playerTransform.parent != null)
					{
						avatar.playerTransform.parent.gameObject.SetActive(true);
					}
					avatar.playerTransform.gameObject.SetActive(true);
				}
			}
			if (avatar.playerHealth != null)
			{
				avatar.playerHealth.HealOther(100, false);
			}
			LastStatus = IsGuest() ? "local revive (not synced)" : "revived";
		}
		catch (Exception ex)
		{
			LastStatus = "revive: " + ex.Message;
		}
	}

	public static void Bounce()
	{
		try
		{
			PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
			if (local != null)
			{
				local.ForceImpulse(Vector3.up * 18f);
			}
			else if (global::PlayerController.instance != null)
			{
				global::PlayerController.instance.ForceImpulse(Vector3.up * 18f);
			}
			LastStatus = "bounce";
		}
		catch (Exception ex)
		{
			LastStatus = "bounce: " + ex.Message;
		}
	}

	public static void AntiGravBurst()
	{
		try
		{
			if (global::PlayerController.instance != null)
			{
				global::PlayerController.instance.AntiGravity(8f);
			}
			LastStatus = "antigrav";
		}
		catch (Exception ex)
		{
			LastStatus = "antigrav: " + ex.Message;
		}
	}

	public static void SelfTumble()
	{
		try
		{
			PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
			PlayerTumble tumble = GetMember<PlayerTumble>(local, "tumble");
			if (tumble != null)
			{
				tumble.TumbleRequest(true, true);
				LastStatus = "tumble";
			}
		}
		catch (Exception ex)
		{
			LastStatus = "tumble: " + ex.Message;
		}
	}

	public static void ToggleGreenScreen()
	{
		try
		{
			if (GameDirector.instance != null)
			{
				GameDirector.instance.CommandGreenScreenToggle();
				LastStatus = "greenscreen";
			}
		}
		catch (Exception ex)
		{
			LastStatus = "greenscreen: " + ex.Message;
		}
	}

	public static void LightHeldItem()
	{
		try
		{
			PhysGrabber grabber = PhysGrabber.instance;
			PhysGrabObject body = GetMember<PhysGrabObject>(grabber, "grabbedPhysGrabObject");
			if (body == null)
			{
				LastStatus = "not holding item";
				return;
			}
			body.OverrideMass(0.05f, 60f);
			LastStatus = "held item light";
		}
		catch (Exception ex)
		{
			LastStatus = "light item: " + ex.Message;
		}
	}

	public static void ApplySuperSpeed()
	{
		try
		{
			global::PlayerController pc = global::PlayerController.instance;
			if (pc == null)
			{
				return;
			}
			if (SuperSpeed)
			{
				pc.OverrideSpeed(3.5f, 1.1f);
				_hadSuperSpeed = true;
				return;
			}
			if (_hadSuperSpeed)
			{
				ClearSpeedOverride(pc);
				_hadSuperSpeed = false;
			}
		}
		catch
		{
		}
	}

	private static bool WantDebugSlow()
	{
		if (!SlowWalk || SuperSpeed)
		{
			return false;
		}
		return !SprintHeld();
	}

	private static bool SprintHeld()
	{
		try
		{
			if (SemiFunc.InputHold(InputKey.Sprint))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		}
		catch
		{
			return false;
		}
	}

	private static void ClearSpeedOverride(global::PlayerController pc)
	{
		SetMember(pc, "overrideSpeedTimer", 0f);
		SetMember(pc, "overrideSpeedMultiplier", 1f);
		float move = GetMember<float>(pc, "playerOriginalMoveSpeed");
		float sprint = GetMember<float>(pc, "playerOriginalSprintSpeed");
		float crouch = GetMember<float>(pc, "playerOriginalCrouchSpeed");
		if (move > 0.01f)
		{
			pc.MoveSpeed = move;
		}
		if (sprint > 0.01f)
		{
			pc.SprintSpeed = sprint;
		}
		if (crouch > 0.01f)
		{
			pc.CrouchSpeed = crouch;
		}
	}

	public static void ApplyNoDeathPit()
	{
		if (!NoDeathPit)
		{
			return;
		}
		try
		{
			PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
			PlayerTumble tumble = GetMember<PlayerTumble>(local, "tumble");
			PhysGrabObject body = GetMember<PhysGrabObject>(tumble, "physGrabObject");
			body?.DisableDeathPitEffect(2f);
			SuppressDeathPitOn(local);
			if (global::PlayerController.instance != null)
			{
				SuppressDeathPitOn(global::PlayerController.instance);
			}
		}
		catch
		{
		}
	}

	private static void SuppressDeathPitOn(Component root)
	{
		if ((Object)root == null)
		{
			return;
		}
		PhysGrabObject[] bodies = root.GetComponentsInChildren<PhysGrabObject>(true);
		for (int i = 0; i < bodies.Length; i++)
		{
			bodies[i]?.DisableDeathPitEffect(2f);
		}
	}

	public static void ApplyHeadEnergy()
	{
		if (!InfiniteHeadEnergy)
		{
			return;
		}
		try
		{
			if (SpectateCamera.instance != null)
			{
				SetMember(SpectateCamera.instance, "headEnergy", 1f);
				SetMember(SpectateCamera.instance, "headEnergyEnough", true);
			}
		}
		catch
		{
		}
	}

	public static void Tick()
	{
		MidJoin.Tick();
		Troll.EnsurePhotonEventHook();
		CosmeticFeatures.EnsurePhotonEventHook();
		ApplyDirectorFlags();
		if (HideGrabber)
		{
			ApplyHideGrabber();
		}
		if (HideItemLabels)
		{
			ApplyHideItemLabels();
		}
		if (NoCameraShake)
		{
			ApplyNoCameraShake();
		}
		if (InstantGunBuildup)
		{
			ApplyInstantGunBuildup();
		}
		ApplyFeather();
		ApplySlowWalk();
		ApplySuperSpeed();
		if (LowHaul)
		{
			ApplyLowHaul();
		}
		if (CheapShop)
		{
			ApplyCheapShop();
		}
		if (FillValuables)
		{
			ApplyFillValuables();
		}
		if (NoDeathPit)
		{
			ApplyNoDeathPit();
		}
		if (InfiniteHeadEnergy)
		{
			ApplyHeadEnergy();
		}
		if (Hax2.blindEnemies)
		{
			KeepNoAggro();
		}
		if (Hax2.godModeActive)
		{
			ReapplyGodMode();
		}
		if (Hax2.stamineState)
		{
			ReapplyDebugEnergy();
		}
		if (Hax2.infiniteHealthActive)
		{
			ReapplyHeal();
		}
	}

	private static void KeepNoAggro()
	{
		try
		{
			string steamId = GetLocalSteamId();
			if (string.IsNullOrEmpty(steamId) || EnemyDirector.instance == null)
			{
				return;
			}
			ToggleStringList(EnemyDirector.instance, "debugNoVision", steamId, true);
		}
		catch
		{
		}
	}

	private static void ReapplyGodMode()
	{
		try
		{
			PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
			if (local != null && local.playerHealth != null)
			{
				SetMember(local.playerHealth, "godMode", true);
			}
		}
		catch
		{
		}
	}

	private static void ReapplyDebugEnergy()
	{
		try
		{
			if (global::PlayerController.instance != null)
			{
				global::PlayerController pc = global::PlayerController.instance;
				pc.DebugEnergy = true;
				if (pc.EnergyStart < 1f)
				{
					pc.EnergyStart = 100f;
				}
				pc.EnergyCurrent = pc.EnergyStart;
			}
		}
		catch
		{
		}
	}

	private static void ReapplyHeal()
	{
		try
		{
			PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
			if (local == null || local.playerHealth == null)
			{
				return;
			}
			if (GetMember<bool>(local, "deadSet") || GetMember<bool>(local, "isDisabled"))
			{
				return;
			}
			PlayerHealth health = local.playerHealth;
			int current = GetMember<int>(health, "health");
			int max = GetMember<int>(health, "maxHealth");
			if (max <= 0)
			{
				max = 100;
			}
			if (current < max)
			{
				health.Heal(max, false);
			}
		}
		catch
		{
		}
	}

	public static string GetItemPlainName(string itemName)
	{
		Item item = FindItem(itemName);
		if ((UnityEngine.Object)item == null || string.IsNullOrEmpty(item.itemName))
		{
			return null;
		}
		if (string.Equals(item.itemName, "N/A", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return item.itemName;
	}

	private static Item FindItem(string itemName)
	{
		string want = (itemName ?? "").ToLower();
		StatsManager stats = StatsManager.instance;
		if (stats != null && stats.itemDictionary != null)
		{
			if (stats.itemDictionary.TryGetValue(itemName, out Item exact))
			{
				return exact;
			}
			foreach (Item item in stats.itemDictionary.Values)
			{
				if (ItemNameMatches(item, itemName, want))
				{
					return item;
				}
			}
		}
		Item[] all = Resources.FindObjectsOfTypeAll<Item>();
		if (all != null)
		{
			for (int i = 0; i < all.Length; i++)
			{
				if (ItemNameMatches(all[i], itemName, want))
				{
					return all[i];
				}
			}
		}
		return null;
	}

	private static bool ItemNameMatches(Item item, string itemName, string want)
	{
		if ((UnityEngine.Object)item == null)
		{
			return false;
		}
		string n = ((UnityEngine.Object)item).name;
		return n.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
			n.ToLower().Replace("item ", "") == want.Replace("item ", "");
	}

	private static List<EnemyParent> GetSpawnedParents()
	{
		var result = new List<EnemyParent>();
		try
		{
			EnemyDirector director = EnemyDirector.instance;
			if (director == null)
			{
				return result;
			}
			object raw = typeof(EnemyDirector).GetField("enemiesSpawned", InstAll)?.GetValue(director);
			if (raw is List<EnemyParent> list)
			{
				result.AddRange(list);
			}
		}
		catch
		{
		}
		return result;
	}

	private static void RemoveFromSpawned(EnemyParent parent)
	{
		try
		{
			object raw = typeof(EnemyDirector).GetField("enemiesSpawned", InstAll)?.GetValue(EnemyDirector.instance);
			(raw as List<EnemyParent>)?.Remove(parent);
		}
		catch
		{
		}
	}

	private static string GetLocalSteamId()
	{
		try
		{
			if (global::PlayerController.instance != null)
			{
				string id = GetMember<string>(global::PlayerController.instance, "playerSteamID");
				if (!string.IsNullOrEmpty(id))
				{
					return id;
				}
			}
		}
		catch
		{
		}
		try
		{
			PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
			return local != null ? SemiFunc.PlayerGetSteamID(local) : null;
		}
		catch
		{
			return null;
		}
	}

	private static void ToggleStringList(object instance, string fieldName, string value, bool add)
	{
		if (instance == null || string.IsNullOrEmpty(value))
		{
			return;
		}
		FieldInfo field = instance.GetType().GetField(fieldName, InstAll);
		if (field == null)
		{
			return;
		}
		List<string> list = field.GetValue(instance) as List<string>;
		if (list == null)
		{
			return;
		}
		if (add)
		{
			if (!list.Contains(value))
			{
				list.Add(value);
			}
		}
		else
		{
			list.Remove(value);
		}
	}

	private static void SetMember(object instance, string name, object value)
	{
		if (instance == null)
		{
			return;
		}
		Type type = instance.GetType();
		FieldInfo field = type.GetField(name, InstAll);
		if (field != null)
		{
			field.SetValue(instance, value);
			return;
		}
		PropertyInfo prop = type.GetProperty(name, InstAll);
		prop?.SetValue(instance, value);
	}

	private static T GetMember<T>(object instance, string name)
	{
		if (instance == null)
		{
			return default;
		}
		Type type = instance.GetType();
		FieldInfo field = type.GetField(name, InstAll);
		if (field != null)
		{
			object raw = field.GetValue(instance);
			if (raw is T typed)
			{
				return typed;
			}
			if (raw != null)
			{
				try { return (T)raw; } catch { }
			}
		}
		PropertyInfo prop = type.GetProperty(name, InstAll);
		if (prop != null)
		{
			object raw = prop.GetValue(instance);
			if (raw is T typed)
			{
				return typed;
			}
		}
		return default;
	}
}
