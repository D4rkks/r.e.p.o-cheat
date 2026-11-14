using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// Screen-space ESP overlay. Drawn only while the playable scene is in Main
/// and level generation is complete. Suppressed during loading / scene fades.
/// </summary>
public static class EspOverlay
{
	private static readonly FieldInfo DollarCurrent = AccessTools.Field(typeof(ValuableObject), "dollarValueCurrent");
	private static readonly FieldInfo ExtractionState = AccessTools.Field(typeof(ExtractionPoint), "currentState");
	private static readonly FieldInfo PlayerHealthValue = AccessTools.Field(typeof(PlayerHealth), "health");
	private static readonly FieldInfo PlayerMaxHealth = AccessTools.Field(typeof(PlayerHealth), "maxHealth");
	private static readonly FieldInfo PlayerDead = AccessTools.Field(typeof(PlayerAvatar), "deadSet");
	private static readonly FieldInfo PlayerDisabled = AccessTools.Field(typeof(PlayerAvatar), "isDisabled");
	private static readonly FieldInfo EnemyParentEnemy = AccessTools.Field(typeof(EnemyParent), "Enemy");
	private static readonly FieldInfo DeathHeadTriggered = AccessTools.Field(typeof(PlayerDeathHead), "triggered");
	private static readonly FieldInfo VisualsAnimator = AccessTools.Field(typeof(PlayerAvatarVisuals), "animator");

	private static Texture2D _pixel;
	private static GUIStyle _label;
	private static GUIStyle _shadow;
	private static float _nextRefresh;
	private static Camera _cam;
	private static Rect _guiViewport;
	private static int _lastCameraId;
	private static readonly Vector3[] _corners = new Vector3[8];

	/// <summary>
	/// True only after the local client has completed generation and entered the
	/// normal playable state. This deliberately excludes Start/Load/Outro/End,
	/// which is where the old implementation accidentally rendered the ESP.
	/// </summary>
	public static bool IsGameplayReady()
	{
		GameDirector gd = GameDirector.instance;
		if (gd == null || gd.currentState != GameDirector.gameState.Main)
		{
			return false;
		}

		LevelGenerator lg = LevelGenerator.Instance;
		return lg != null && lg.Generated;
	}

	public static void Draw()
	{
		if (RunManager.instance == null || !IsGameplayReady())
		{
			RestoreChams();
			return;
		}
		if (!AnyEnabled())
		{
			RestoreChams();
			return;
		}
		_cam = ResolveProjectionCamera();
		if (_cam == null)
		{
			RestoreChams();
			return;
		}

		_guiViewport = GetGuiViewport(_cam);
		if (_guiViewport.width <= 1f || _guiViewport.height <= 1f)
		{
			RestoreChams();
			return;
		}

		LogProjectionCameraOnce(_cam);
		EnsureGui();
		RefreshLists();
		if (Event.current.type != EventType.Repaint)
		{
			return;
		}

		// Hax2 shares one IMGUI pass with windows/other overlays.  Always project
		// the ESP in unscaled screen pixels so a foreign GUI.matrix cannot move it.
		Matrix4x4 previousGuiMatrix = GUI.matrix;
		try
		{
			GUI.matrix = Matrix4x4.identity;
			DrawProjected(_cam.transform.position);
		}
		finally
		{
			GUI.matrix = previousGuiMatrix;
		}
	}

	private static void DrawProjected(Vector3 origin)
	{
		if (MaiDebugCheats.drawEspBool)
		{
			DrawEnemies(origin);
		}
		if (MaiDebugCheats.drawItemEspBool)
		{
			DrawValuables(origin);
			DrawCubes(origin);
			if (MaiDebugCheats.showPlayerDeathHeads)
			{
				DrawDeathHeads(origin);
			}
		}
		if (MaiDebugCheats.drawExtractionPointEspBool)
		{
			DrawExtraction(origin);
		}
		if (MaiDebugCheats.drawPlayerEspBool)
		{
			DrawPlayers(origin);
		}
		ApplyChams();
	}

	private static Camera ResolveProjectionCamera()
	{
		// Prefer CameraZoom (gameplay).  GameHelper historically returned Camera.main,
		// which can be the compositor and does not see the world.
		try
		{
			CameraZoom zoom = CameraZoom.Instance;
			if (zoom != null)
			{
				Camera zoomCamera = zoom.GetComponent<Camera>();
				if (zoomCamera == null) zoomCamera = zoom.GetComponentInParent<Camera>();
				if (zoomCamera == null) zoomCamera = zoom.GetComponentInChildren<Camera>();
				if (IsProjectionCamera(zoomCamera))
				{
					return zoomCamera;
				}
			}
		}
		catch
		{
		}

		Camera[] cameras = Camera.allCameras;
		Camera best = null;
		float bestScore = float.MinValue;
		PlayerAvatar local = null;
		try
		{
			local = SemiFunc.PlayerAvatarLocal();
		}
		catch
		{
		}

		for (int i = 0; i < cameras.Length; i++)
		{
			Camera candidate = cameras[i];
			if (!IsProjectionCamera(candidate))
			{
				continue;
			}

			float score = candidate.depth * 0.01f;
			if (candidate.targetTexture != null)
			{
				score += 1000f;
			}
			else
			{
				Rect pr = candidate.pixelRect;
				score += (pr.width * pr.height) / Mathf.Max(1f, Screen.width * Screen.height);
			}

			if (local != null)
			{
				Transform playerTransform = local.playerTransform != null ? local.playerTransform : local.transform;
				if (playerTransform != null)
				{
					float distance = Vector3.Distance(candidate.transform.position, playerTransform.position);
					score += Mathf.Clamp(20f - distance, -20f, 20f);
				}
			}

			if (score > bestScore)
			{
				bestScore = score;
				best = candidate;
			}
		}

		if (best != null)
		{
			return best;
		}

		try
		{
			Camera helper = GameHelper.GetActiveCamera();
			if (IsProjectionCamera(helper))
			{
				return helper;
			}
		}
		catch
		{
		}

		Camera main = Camera.main;
		return IsProjectionCamera(main) ? main : null;
	}

	private static bool IsProjectionCamera(Camera camera)
	{
		if (camera == null || !camera.enabled || camera.cullingMask == 0)
		{
			return false;
		}

		if (camera.targetTexture != null)
		{
			return camera.targetTexture.width > 16 && camera.targetTexture.height > 16;
		}

		Rect pr = camera.pixelRect;
		return pr.width > 16f && pr.height > 16f;
	}

	private static Rect GetGuiViewport(Camera camera)
	{
		// For an off-screen gameplay camera, viewport coordinates are normalized
		// to the render target.  The game's compositor presents that image to the
		// final display, so map normalized coordinates to the final screen instead
		// of reusing RenderTexture pixel dimensions.
		if (camera != null && camera.targetTexture != null)
		{
			return new Rect(0f, 0f, Screen.width, Screen.height);
		}

		Rect pixel = camera != null ? camera.pixelRect : default;
		if (pixel.width <= 1f || pixel.height <= 1f)
		{
			return new Rect(0f, 0f, Screen.width, Screen.height);
		}

		// Camera coordinates use a bottom-left origin; IMGUI uses top-left.
		return new Rect(pixel.xMin, Screen.height - pixel.yMax, pixel.width, pixel.height);
	}

	private static void LogProjectionCameraOnce(Camera camera)
	{
		if (camera == null)
		{
			return;
		}

		int id = camera.GetInstanceID();
		if (id == _lastCameraId)
		{
			return;
		}
		_lastCameraId = id;

		RenderTexture rt = camera.targetTexture;
		string target = rt != null ? (rt.width + "x" + rt.height) : "screen";
		Rect pr = camera.pixelRect;
		Debug.Log("[ESP] projection camera=" + camera.name
			+ " id=" + id
			+ " target=" + target
			+ " pixelRect=" + pr
			+ " guiViewport=" + _guiViewport
			+ " screen=" + Screen.width + "x" + Screen.height);
	}

	private static bool AnyEnabled()
	{
		return MaiDebugCheats.drawEspBool
			|| MaiDebugCheats.drawItemEspBool
			|| MaiDebugCheats.drawExtractionPointEspBool
			|| MaiDebugCheats.drawPlayerEspBool
			|| MaiDebugCheats.drawChamsBool
			|| MaiDebugCheats.drawItemChamsBool
			|| SkeletonESP.enabled;
	}

	public static void RefreshLists(bool force = false)
	{
		if (!force && Time.unscaledTime < _nextRefresh)
		{
			return;
		}
		_nextRefresh = Time.unscaledTime + 0.25f;
		List<Enemy> enemies = MaiDebugCheats.enemyList;
		if (enemies == null)
		{
			MaiDebugCheats.enemyList = enemies = new List<Enemy>();
		}
		enemies.Clear();
		EnemyDirector director = EnemyDirector.instance;
		if (director != null && director.enemiesSpawned != null)
		{
			List<EnemyParent> spawned = director.enemiesSpawned;
			for (int i = 0; i < spawned.Count; i++)
			{
				EnemyParent parent = spawned[i];
				if (parent == null)
				{
					continue;
				}
				Enemy enemy = EnemyParentEnemy?.GetValue(parent) as Enemy;
				if (enemy == null)
				{
					enemy = parent.GetComponentInChildren<Enemy>(true);
				}
				if (enemy != null && enemy.gameObject.activeInHierarchy)
				{
					enemies.Add(enemy);
				}
			}
		}
		List<object> valuables = MaiDebugCheats.valuableObjects;
		if (valuables == null)
		{
			return;
		}
		valuables.Clear();
		ValuableDirector vd = ValuableDirector.instance;
		if (vd != null && vd.valuableList != null)
		{
			List<ValuableObject> list = vd.valuableList;
			for (int i = 0; i < list.Count; i++)
			{
				ValuableObject item = list[i];
				if (item != null)
				{
					valuables.Add(item);
				}
			}
		}
		PlayerDeathHead[] heads = SceneCache.GetObjects<PlayerDeathHead>(0.5f);
		if (heads != null)
		{
			for (int i = 0; i < heads.Length; i++)
			{
				if (heads[i] != null)
				{
					valuables.Add(heads[i]);
				}
			}
		}
		RoundDirector rd = RoundDirector.instance;
		if (rd != null && rd.cosmeticWorldObjects != null)
		{
			List<CosmeticWorldObject> cubes = rd.cosmeticWorldObjects;
			for (int i = 0; i < cubes.Count; i++)
			{
				CosmeticWorldObject cube = cubes[i];
				if (cube != null && !valuables.Contains(cube))
				{
					valuables.Add(cube);
				}
			}
		}
	}

	private static void DrawEnemies(Vector3 origin)
	{
		List<Enemy> enemies = MaiDebugCheats.enemyList;
		if (enemies == null)
		{
			return;
		}
		for (int i = 0; i < enemies.Count; i++)
		{
			Enemy enemy = enemies[i];
			if (enemy == null || !enemy.gameObject.activeInHierarchy)
			{
				continue;
			}
			Transform marker = enemy.CenterTransform != null ? enemy.CenterTransform : enemy.transform;
			float dist = Vector3.Distance(origin, marker.position);
			if (dist > 120f)
			{
				continue;
			}
			if (!TryWorldBounds(enemy.gameObject, out Bounds bounds))
			{
				bounds = new Bounds(marker.position, new Vector3(0.8f, 1.6f, 0.8f));
			}
			if (!ProjectBounds(bounds, out Rect box, out Vector2 foot))
			{
				continue;
			}
			Color color = new Color(1f, 0.25f, 0.2f, 0.95f);
			if (MaiDebugCheats.showEnemyBox)
			{
				RectOutline(box, color, 1.5f);
			}
			if (ESPEnhancements.showTraceLinesEnemy)
			{
				TraceTo(foot, color);
			}
			string name = EnemyName(enemy);
			int hp = Enemies.GetEnemyHealth(enemy);
			int max = Enemies.GetEnemyMaxHealth(enemy);
			List<string> lines = new List<string>(3);
			if (MaiDebugCheats.showEnemyNames)
			{
				lines.Add(name);
			}
			if (MaiDebugCheats.showEnemyHP && hp >= 0)
			{
				lines.Add(L.T("esp.hp_fmt", hp, max > 0 ? max : hp).Trim());
			}
			if (MaiDebugCheats.showEnemyDistance)
			{
				lines.Add($"{dist:F0}m");
			}
			DrawStack(box, lines, color);
		}
	}

	private static void DrawValuables(Vector3 origin)
	{
		List<object> items = MaiDebugCheats.valuableObjects;
		if (items == null)
		{
			return;
		}
		for (int i = 0; i < items.Count; i++)
		{
			ValuableObject item = items[i] as ValuableObject;
			if (item == null)
			{
				continue;
			}
			if (item.GetComponent<PlayerDeathHead>() != null)
			{
				continue;
			}
			Transform t = item.transform;
			if (t == null || !t.gameObject.activeInHierarchy)
			{
				continue;
			}
			float dist = Vector3.Distance(origin, t.position);
			if (dist > MaiDebugCheats.maxItemEspDistance)
			{
				continue;
			}
			int value = ItemValue(item);
			if (MaiDebugCheats.showItemValue && value < MaiDebugCheats.minItemValue)
			{
				continue;
			}
			if (!TryWorldBounds(item.gameObject, out Bounds bounds))
			{
				bounds = new Bounds(t.position, Vector3.one * 0.4f);
			}
			if (!ProjectBounds(bounds, out Rect box, out Vector2 foot))
			{
				continue;
			}
			Color color = ValueColor(value);
			if (MaiDebugCheats.draw3DItemEspBool)
			{
				DrawBounds3D(bounds, color);
			}
			else
			{
				RectOutline(box, color, 1.2f);
			}
			if (ESPEnhancements.showTraceLinesItem)
			{
				TraceTo(foot, color);
			}
			List<string> lines = new List<string>(3);
			if (MaiDebugCheats.showItemNames)
			{
				lines.Add(CleanName(item.gameObject.name));
			}
			if (MaiDebugCheats.showItemValue)
			{
				lines.Add($"${value}");
			}
			if (MaiDebugCheats.showItemDistance)
			{
				lines.Add($"{dist:F0}m");
			}
			DrawStack(box, lines, color);
		}
	}

	private static void DrawCubes(Vector3 origin)
	{
		List<object> items = MaiDebugCheats.valuableObjects;
		if (items == null)
		{
			return;
		}
		Color cubeColor = new Color(0.85f, 0.45f, 1f);
		for (int i = 0; i < items.Count; i++)
		{
			CosmeticWorldObject cube = items[i] as CosmeticWorldObject;
			if (cube == null)
			{
				continue;
			}
			Transform t = cube.transform;
			if (t == null || !t.gameObject.activeInHierarchy)
			{
				continue;
			}
			float dist = Vector3.Distance(origin, t.position);
			if (dist > MaiDebugCheats.maxItemEspDistance)
			{
				continue;
			}
			if (!TryWorldBounds(cube.gameObject, out Bounds bounds))
			{
				bounds = new Bounds(t.position, Vector3.one * 0.6f);
			}
			if (!ProjectBounds(bounds, out Rect box, out Vector2 foot))
			{
				continue;
			}
			if (MaiDebugCheats.draw3DItemEspBool)
			{
				DrawBounds3D(bounds, cubeColor);
			}
			else
			{
				RectOutline(box, cubeColor, 1.4f);
			}
			if (ESPEnhancements.showTraceLinesItem)
			{
				TraceTo(foot, cubeColor);
			}
			List<string> lines = new List<string>(3);
			if (MaiDebugCheats.showItemNames)
			{
				lines.Add(L.T("item.name.Cube") + " " + CosmeticFeatures.RarityLabel(cube.rarity));
			}
			if (MaiDebugCheats.showItemDistance)
			{
				lines.Add($"{dist:F0}m");
			}
			DrawStack(box, lines, cubeColor);
		}
	}

	private static void DrawDeathHeads(Vector3 origin)
	{
		List<object> items = MaiDebugCheats.valuableObjects;
		if (items == null)
		{
			return;
		}
		for (int i = 0; i < items.Count; i++)
		{
			PlayerDeathHead head = items[i] as PlayerDeathHead;
			if (head == null)
			{
				continue;
			}
			if (DeathHeadTriggered != null && DeathHeadTriggered.GetValue(head) is bool triggered && !triggered)
			{
				continue;
			}
			Transform t = head.transform;
			if (t == null || !t.gameObject.activeInHierarchy)
			{
				continue;
			}
			float dist = Vector3.Distance(origin, t.position);
			if (!ToGui(t.position, out Vector2 gui))
			{
				continue;
			}
			Color color = new Color(1f, 0.35f, 0.85f, 0.95f);
			Rect box = new Rect(gui.x - 8f, gui.y - 8f, 16f, 16f);
			RectOutline(box, color, 1.5f);
			string who = head.playerAvatar != null ? MidJoin.GetDisplayName(head.playerAvatar, SemiFunc.PlayerGetName(head.playerAvatar) ?? L.T("esp.death_head")) : L.T("esp.death_head");
			DrawStack(box, new List<string> { who, $"{dist:F0}m" }, color);
		}
	}

	private static void DrawExtraction(Vector3 origin)
	{
		ExtractionPoint[] points = SceneCache.GetObjects<ExtractionPoint>(0.5f);
		if (points == null)
		{
			return;
		}
		for (int i = 0; i < points.Length; i++)
		{
			ExtractionPoint point = points[i];
			if (point == null || !point.gameObject.activeInHierarchy)
			{
				continue;
			}
			Vector3 pos = point.transform.position;
			float dist = Vector3.Distance(origin, pos);
			if (!TryWorldBounds(point.gameObject, out Bounds bounds))
			{
				bounds = new Bounds(pos, new Vector3(2f, 3f, 2f));
			}
			if (!ProjectBounds(bounds, out Rect box, out _))
			{
				continue;
			}
			string state = ExtractionStateName(point);
			Color color = state == "Active" || state == L.T("esp.state_active")
				? new Color(0.25f, 1f, 0.45f)
				: new Color(0.3f, 0.85f, 1f);
			RectOutline(box, color, 1.5f);
			List<string> lines = new List<string>(2);
			if (MaiDebugCheats.showExtractionNames)
			{
				lines.Add($"{L.T("esp.extraction")} [{state}]");
			}
			if (MaiDebugCheats.showExtractionDistance)
			{
				lines.Add($"{dist:F0}m");
			}
			DrawStack(box, lines, color);
		}
	}

	private static void DrawPlayers(Vector3 origin)
	{
		List<PlayerAvatar> players = GameDirector.instance != null ? GameDirector.instance.PlayerList : SemiFunc.PlayerGetList();
		if (players == null)
		{
			return;
		}
		PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
		for (int i = 0; i < players.Count; i++)
		{
			PlayerAvatar player = players[i];
			if (player == null || player == local)
			{
				continue;
			}
			if (PlayerDisabled != null && PlayerDisabled.GetValue(player) is bool disabled && disabled)
			{
				continue;
			}
			bool dead = PlayerDead != null && PlayerDead.GetValue(player) is bool d && d;
			Transform t = player.playerTransform != null ? player.playerTransform : player.transform;
			float dist = Vector3.Distance(origin, t.position);
			GameObject visual = player.playerAvatarVisuals != null && player.playerAvatarVisuals.meshParent != null
				? player.playerAvatarVisuals.meshParent
				: player.gameObject;
			if (!TryWorldBounds(visual, out Bounds bounds))
			{
				bounds = new Bounds(t.position + Vector3.up * 0.6f, new Vector3(0.6f, 1.2f, 0.6f));
			}
			if (!ProjectBounds(bounds, out Rect box, out Vector2 foot))
			{
				continue;
			}
			Color color = dead ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.25f, 1f, 0.4f);
			if (MaiDebugCheats.draw2DPlayerEspBool)
			{
				RectOutline(box, color, 1.5f);
			}
			if (MaiDebugCheats.draw3DPlayerEspBool)
			{
				DrawBounds3D(bounds, color);
			}
			if (ESPEnhancements.showTraceLinesPlayer)
			{
				TraceTo(foot, color);
			}
			if (SkeletonESP.enabled)
			{
				DrawPlayerRig(player, color);
			}
			string name = MidJoin.GetDisplayName(player, SemiFunc.PlayerGetName(player) ?? L.T("server.unknown"));
			List<string> lines = new List<string>(3);
			if (MaiDebugCheats.showPlayerNames)
			{
				lines.Add(name);
			}
			if (MaiDebugCheats.showPlayerHP && player.playerHealth != null)
			{
				int hp = PlayerHealthValue?.GetValue(player.playerHealth) is int h ? h : -1;
				int max = PlayerMaxHealth?.GetValue(player.playerHealth) is int m ? m : 100;
				if (hp >= 0)
				{
					lines.Add(L.T("esp.player_hp_fmt", hp) + $"/{max}");
				}
			}
			if (MaiDebugCheats.showPlayerDistance)
			{
				lines.Add($"{dist:F0}m");
			}
			DrawStack(box, lines, color);
		}
	}

	private static void DrawPlayerRig(PlayerAvatar player, Color color)
	{
		PlayerAvatarVisuals vis = player.playerAvatarVisuals;
		if (vis == null)
		{
			return;
		}
		Animator animator = VisualsAnimator?.GetValue(vis) as Animator;
		if (animator != null && animator.isHuman)
		{
			DrawHumanoid(animator, color);
			return;
		}
		DrawLine3(vis.meshParent != null ? vis.meshParent.transform : player.transform, vis.attachNeck, color);
		DrawLine3(vis.attachNeck, vis.headLookAtTransform, color);
		DrawLine3(vis.leanTransform, vis.legTwistTransform, color);
	}

	private static void DrawHumanoid(Animator animator, Color color)
	{
		HumanBodyBones[][] links =
		{
			new[] { HumanBodyBones.Hips, HumanBodyBones.Spine },
			new[] { HumanBodyBones.Spine, HumanBodyBones.Head },
			new[] { HumanBodyBones.Spine, HumanBodyBones.LeftUpperArm },
			new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftHand },
			new[] { HumanBodyBones.Spine, HumanBodyBones.RightUpperArm },
			new[] { HumanBodyBones.RightUpperArm, HumanBodyBones.RightHand },
			new[] { HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg },
			new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftFoot },
			new[] { HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg },
			new[] { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightFoot }
		};
		for (int i = 0; i < links.Length; i++)
		{
			Transform a = animator.GetBoneTransform(links[i][0]);
			Transform b = animator.GetBoneTransform(links[i][1]);
			DrawLine3(a, b, color);
		}
	}

	private static void DrawLine3(Transform a, Transform b, Color color)
	{
		if (a == null || b == null)
		{
			return;
		}
		if (!ToGui(a.position, out Vector2 p1) || !ToGui(b.position, out Vector2 p2))
		{
			return;
		}
		Line(p1, p2, color, 2f);
	}

	private static string EnemyName(Enemy enemy)
	{
		EnemyParent parent = enemy.GetComponentInParent<EnemyParent>();
		if (parent != null && !string.IsNullOrEmpty(parent.enemyName))
		{
			return LanguageManager.GetEnemyName(parent.enemyName);
		}
		return L.T("common.enemy");
	}

	private static int ItemValue(ValuableObject item)
	{
		if (DollarCurrent == null)
		{
			return 0;
		}
		object raw = DollarCurrent.GetValue(item);
		if (raw is float f)
		{
			return Mathf.RoundToInt(f);
		}
		if (raw is int n)
		{
			return n;
		}
		return 0;
	}

	private static Color ValueColor(int value)
	{
		if (value >= 10000)
		{
			return new Color(1f, 0.55f, 0.15f);
		}
		if (value >= 3000)
		{
			return new Color(0.35f, 1f, 0.45f);
		}
		return new Color(1f, 0.92f, 0.25f);
	}

	private static string ExtractionStateName(ExtractionPoint point)
	{
		object raw = ExtractionState?.GetValue(point);
		if (raw == null)
		{
			return L.T("esp.state_idle");
		}
		string name = raw.ToString();
		if (name == "Active" || name == "Extracting")
		{
			return L.T("esp.state_active");
		}
		if (name == "Idle")
		{
			return L.T("esp.state_idle");
		}
		return name;
	}

	private static string CleanName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "?";
		}
		return name.Replace("(Clone)", "").Replace("Valuable ", "").Trim();
	}

	private static bool TryWorldBounds(GameObject go, out Bounds bounds)
	{
		bounds = default;
		Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
		bool any = false;
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer r = renderers[i];
			if (r == null || !r.enabled || r is ParticleSystemRenderer)
			{
				continue;
			}
			if (!any)
			{
				bounds = r.bounds;
				any = true;
			}
			else
			{
				bounds.Encapsulate(r.bounds);
			}
		}
		return any && bounds.size.sqrMagnitude > 0.0001f;
	}

	private static bool ProjectBounds(Bounds bounds, out Rect box, out Vector2 foot)
	{
		box = default;
		foot = default;
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		_corners[0] = new Vector3(min.x, min.y, min.z);
		_corners[1] = new Vector3(max.x, min.y, min.z);
		_corners[2] = new Vector3(min.x, max.y, min.z);
		_corners[3] = new Vector3(max.x, max.y, min.z);
		_corners[4] = new Vector3(min.x, min.y, max.z);
		_corners[5] = new Vector3(max.x, min.y, max.z);
		_corners[6] = new Vector3(min.x, max.y, max.z);
		_corners[7] = new Vector3(max.x, max.y, max.z);
		float xMin = float.MaxValue;
		float yMin = float.MaxValue;
		float xMax = float.MinValue;
		float yMax = float.MinValue;
		int visible = 0;
		for (int i = 0; i < 8; i++)
		{
			if (!ToGui(_corners[i], out Vector2 p))
			{
				continue;
			}
			visible++;
			if (p.x < xMin) xMin = p.x;
			if (p.y < yMin) yMin = p.y;
			if (p.x > xMax) xMax = p.x;
			if (p.y > yMax) yMax = p.y;
		}
		if (visible < 2)
		{
			return false;
		}
		float left = _guiViewport.xMin - 50f;
		float top = _guiViewport.yMin - 50f;
		float right = _guiViewport.xMax + 50f;
		float bottom = _guiViewport.yMax + 50f;
		xMin = Mathf.Clamp(xMin, left, right);
		yMin = Mathf.Clamp(yMin, top, bottom);
		xMax = Mathf.Clamp(xMax, left, right);
		yMax = Mathf.Clamp(yMax, top, bottom);
		float w = Mathf.Clamp(xMax - xMin, 6f, _guiViewport.width + 100f);
		float h = Mathf.Clamp(yMax - yMin, 6f, _guiViewport.height + 100f);
		box = new Rect(xMin, yMin, w, h);
		foot = new Vector2(xMin + w * 0.5f, yMax);
		return true;
	}

	private static bool ToGui(Vector3 world, out Vector2 gui)
	{
		if (_cam == null)
		{
			gui = default;
			return false;
		}

		Vector3 viewport = _cam.WorldToViewportPoint(world);
		if (viewport.z <= 0.05f)
		{
			gui = default;
			return false;
		}

		// _guiViewport is already expressed in IMGUI coordinates (top-left
		// origin).  Mapping normalized viewport coordinates here works for both
		// direct-to-screen cameras and RenderTexture-backed gameplay cameras.
		float guiX = _guiViewport.xMin + viewport.x * _guiViewport.width;
		float guiY = _guiViewport.yMax - viewport.y * _guiViewport.height;
		gui = new Vector2(guiX, guiY);
		return true;
	}

	private static void DrawBounds3D(Bounds bounds, Color color)
	{
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		Vector3[] c =
		{
			new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z),
			new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z),
			new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z),
			new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z)
		};
		int[] edges = { 0,1, 1,2, 2,3, 3,0, 4,5, 5,6, 6,7, 7,4, 0,4, 1,5, 2,6, 3,7 };
		for (int i = 0; i < edges.Length; i += 2)
		{
			if (ToGui(c[edges[i]], out Vector2 a) && ToGui(c[edges[i + 1]], out Vector2 b))
			{
				Line(a, b, color, 1.2f);
			}
		}
	}

	private static void DrawStack(Rect box, List<string> lines, Color color)
	{
		if (lines == null || lines.Count == 0)
		{
			return;
		}
		_label.normal.textColor = color;
		float width = Mathf.Max(120f, box.width + 20f);
		width = Mathf.Min(width, Mathf.Max(120f, _guiViewport.width));
		float x = Mathf.Clamp(box.center.x - width * 0.5f, _guiViewport.xMin, Mathf.Max(_guiViewport.xMin, _guiViewport.xMax - width));
		float y = box.yMin - 4f;
		for (int i = lines.Count - 1; i >= 0; i--)
		{
			string text = lines[i];
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			float h = _label.CalcHeight(new GUIContent(text), width);
			y -= h;
			Rect rect = new Rect(x, y, width, h);
			_shadow.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
			GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _shadow);
			GUI.Label(rect, text, _label);
		}
	}

	private static void TraceTo(Vector2 foot, Color color)
	{
		Vector2 from = new Vector2(_guiViewport.center.x, _guiViewport.yMax - 8f);
		Line(from, foot, new Color(color.r, color.g, color.b, 0.45f), 1.2f);
	}

	private static void RectOutline(Rect r, Color color, float thickness)
	{
		Line(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), color, thickness);
		Line(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), color, thickness);
		Line(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), color, thickness);
		Line(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), color, thickness);
	}

	private static void Line(Vector2 a, Vector2 b, Color color, float thickness)
	{
		Vector2 d = b - a;
		float len = d.magnitude;
		if (len < 0.5f)
		{
			return;
		}
		float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
		Matrix4x4 prev = GUI.matrix;
		Color prevC = GUI.color;
		GUI.color = color;
		GUIUtility.RotateAroundPivot(angle, a);
		GUI.DrawTexture(new Rect(a.x, a.y - thickness * 0.5f, len, thickness), _pixel);
		GUI.matrix = prev;
		GUI.color = prevC;
	}

	private static void EnsureGui()
	{
		if (_pixel == null)
		{
			_pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			_pixel.SetPixel(0, 0, Color.white);
			_pixel.Apply();
			_pixel.hideFlags = HideFlags.HideAndDontSave;
		}
		if (_label == null)
		{
			_label = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.UpperCenter,
				fontSize = 12,
				fontStyle = FontStyle.Bold,
				wordWrap = false
			};
			_shadow = new GUIStyle(_label);
		}
	}

	private static void ApplyChams()
	{
		bool enemyOn = MaiDebugCheats.drawEspBool && MaiDebugCheats.drawChamsBool;
		bool itemOn = MaiDebugCheats.drawItemEspBool && MaiDebugCheats.drawItemChamsBool;
		if (!enemyOn && !itemOn)
		{
			RestoreChams();
			return;
		}
		if (enemyOn)
		{
			List<Enemy> enemies = MaiDebugCheats.enemyList;
			if (enemies != null)
			{
				for (int i = 0; i < enemies.Count; i++)
				{
					if (enemies[i] != null)
					{
						PaintChams(enemies[i].gameObject, _enemyMats, true);
					}
				}
			}
		}
		else
		{
			RestoreMap(_enemyMats);
		}
		if (itemOn)
		{
			List<object> items = MaiDebugCheats.valuableObjects;
			if (items != null)
			{
				for (int i = 0; i < items.Count; i++)
				{
					Component c = items[i] as Component;
					if (c != null)
					{
						PaintChams(c.gameObject, _itemMats, false);
					}
				}
			}
		}
		else
		{
			RestoreMap(_itemMats);
		}
	}

	private static readonly Dictionary<Renderer, Material[]> _enemyMats = new Dictionary<Renderer, Material[]>();
	private static readonly Dictionary<Renderer, Material[]> _itemMats = new Dictionary<Renderer, Material[]>();
	private static Material _enemyHidden;
	private static Material _enemyVisible;
	private static Material _itemHidden;
	private static Material _itemVisible;

	private static void PaintChams(GameObject go, Dictionary<Renderer, Material[]> store, bool enemy)
	{
		EnsureChamMats();
		Material hid = enemy ? _enemyHidden : _itemHidden;
		Material vis = enemy ? _enemyVisible : _itemVisible;
		if (hid == null || vis == null)
		{
			return;
		}
		Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer r = renderers[i];
			if (r == null || r is ParticleSystemRenderer)
			{
				continue;
			}
			if (!store.ContainsKey(r))
			{
				try
				{
					store[r] = r.sharedMaterials;
				}
				catch
				{
					continue;
				}
			}
			Material[] current = r.sharedMaterials;
			if (current != null && current.Length == 2 && current[0] == hid)
			{
				continue;
			}
			try
			{
				r.sharedMaterials = new Material[] { hid, vis };
			}
			catch
			{
			}
		}
	}

	private static void RestoreChams()
	{
		RestoreMap(_enemyMats);
		RestoreMap(_itemMats);
	}

	private static void RestoreMap(Dictionary<Renderer, Material[]> store)
	{
		if (store == null || store.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<Renderer, Material[]> pair in store)
		{
			if (pair.Key != null)
			{
				try
				{
					pair.Key.sharedMaterials = pair.Value;
				}
				catch
				{
				}
			}
		}
		store.Clear();
	}

	private static void EnsureChamMats()
	{
		Shader shader = Shader.Find("Hidden/Internal-Colored");
		if (shader == null)
		{
			return;
		}
		if (_enemyHidden == null)
		{
			_enemyHidden = MakeCham(shader, MaiDebugCheats.enemyHiddenColor, 8);
			_enemyVisible = MakeCham(shader, MaiDebugCheats.enemyVisibleColor, 4);
			_itemHidden = MakeCham(shader, MaiDebugCheats.itemHiddenColor, 8);
			_itemVisible = MakeCham(shader, MaiDebugCheats.itemVisibleColor, 4);
		}
		else
		{
			_enemyHidden.SetColor("_Color", MaiDebugCheats.enemyHiddenColor);
			_enemyVisible.SetColor("_Color", MaiDebugCheats.enemyVisibleColor);
			_itemHidden.SetColor("_Color", MaiDebugCheats.itemHiddenColor);
			_itemVisible.SetColor("_Color", MaiDebugCheats.itemVisibleColor);
		}
	}

	private static Material MakeCham(Shader shader, Color color, int zTest)
	{
		Material mat = new Material(shader);
		mat.hideFlags = HideFlags.HideAndDontSave;
		mat.SetInt("_SrcBlend", 5);
		mat.SetInt("_DstBlend", 10);
		mat.SetInt("_Cull", 0);
		mat.SetInt("_ZTest", zTest);
		mat.SetInt("_ZWrite", 0);
		mat.SetColor("_Color", color);
		mat.renderQueue = 4000;
		return mat;
	}
}
