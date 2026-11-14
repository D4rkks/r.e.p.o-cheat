using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

internal static class MaiDebugCheats
{
	public class PlayerData
	{
		public object PlayerObject { get; }

		public PhotonView PhotonView { get; }

		public Transform Transform { get; }

		public bool IsAlive { get; set; }

		public string Name { get; set; }

		public PlayerData(object player)
		{
			PlayerObject = player;
			object obj = player.GetType().GetField("photonView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(player);
			PhotonView = obj as PhotonView;
			object obj2 = player.GetType().GetProperty("transform", BindingFlags.Instance | BindingFlags.Public)?.GetValue(player);
			Transform = obj2 as Transform;
			PlayerAvatar playerAvatar = player as PlayerAvatar;
			Name = ((playerAvatar != null) ? MidJoin.GetDisplayName(playerAvatar, SemiFunc.PlayerGetName(playerAvatar) ?? "Unknown Player") : "Unknown Player");
			IsAlive = true;
		}
	}

	public class ExtractionPointData
	{
		public ExtractionPoint ExtractionPoint { get; }

		public string CachedState { get; }

		public Vector3 CachedPosition { get; }

		public ExtractionPointData(ExtractionPoint ep, string state, Vector3 position)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			ExtractionPoint = ep;
			CachedState = state;
			CachedPosition = position;
		}
	}

	public static bool drawItemChamsBool;

	public static int minItemValue;

	public static int maxItemValue;

	public static float maxItemEspDistance;

	public static bool showEnemyBox;

	private static int frameCounter;

	public static List<Enemy> enemyList;

	public static List<object> valuableObjects;

	private static List<object> playerList;

	private static float scaleX;

	private static float scaleY;

	public static Texture2D texture2;

	private static float lastUpdateTime;

	private const float updateInterval = 5f;

	private static GameObject localPlayer;

	private static int lastPlayerDataCount;

	private static int lastEnemyCount;

	private static int lastItemCount;

	private static int lastPlayerCount;

	private static int lastExtractionPointCount;

	private static string lastLocalPlayerName;

	private static Vector3 lastExtractionPosition;

	private static List<ExtractionPointData> extractionPointList;

	public static bool drawEspBool;

	public static bool drawItemEspBool;

	public static bool draw3DItemEspBool;

	public static bool drawPlayerEspBool;

	public static bool draw2DPlayerEspBool;

	public static bool draw3DPlayerEspBool;

	public static bool drawExtractionPointEspBool;

	public static GUIStyle nameStyle;

	public static GUIStyle valueStyle;

	public static GUIStyle enemyStyle;

	public static GUIStyle healthStyle;

	public static GUIStyle distanceStyle;

	public static bool showEnemyNames;

	public static bool showEnemyDistance;

	public static bool showEnemyHP;

	public static bool showItemNames;

	public static bool showItemValue;

	public static bool showItemDistance;

	public static bool showPlayerDeathHeads;

	public static bool showExtractionNames;

	public static bool showExtractionDistance;

	public static bool showPlayerNames;

	public static bool showPlayerDistance;

	public static bool showPlayerHP;

	private static Camera cachedCamera;

	private static Material visibleMaterial;

	private static Material hiddenMaterial;

	private static Material itemVisibleMaterial;

	private static Material itemHiddenMaterial;

	private static Dictionary<Renderer, Material[]> enemyOriginalMaterials;

	private static List<Renderer> enemyMaterialsToRemove = new List<Renderer>();

	public static Color enemyVisibleColor;

	public static Color enemyHiddenColor;

	public static Color itemVisibleColor;

	public static Color itemHiddenColor;

	private static Dictionary<Renderer, Material[]> itemOriginalMaterials;

	private static bool enemyCachedOriginalCamera;

	private static float enemyOriginalFarClipPlane;

	private static DepthTextureMode enemyOriginalDepthTextureMode;

	private static bool enemyOriginalOcclusionCulling;

	private static bool itemCachedOriginalCamera;

	private static float itemOriginalFarClipPlane;

	private static DepthTextureMode itemOriginalDepthTextureMode;

	private static bool itemOriginalOcclusionCulling;

	private static List<PlayerData> playerDataList;

	private static float playerUpdateInterval;

	private static Dictionary<int, int> playerHealthCache;

	private static float lastPlayerUpdateTime;

	public static Dictionary<Enemy, int> enemyHealthCache;

	private const float maxEspDistance = 100f;

	private static FieldInfo _levelAnimationStartedField;

	public static bool drawChamsBool;

	static MaiDebugCheats()
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		drawItemChamsBool = false;
		minItemValue = 0;
		maxItemValue = 50000;
		maxItemEspDistance = 1000f;
		showEnemyBox = true;
		frameCounter = 0;
		enemyList = new List<Enemy>();
		valuableObjects = new List<object>();
		playerList = new List<object>();
		lastUpdateTime = 0f;
		lastPlayerDataCount = 0;
		lastEnemyCount = 0;
		lastItemCount = 0;
		lastPlayerCount = 0;
		lastExtractionPointCount = 0;
		lastLocalPlayerName = "";
		lastExtractionPosition = Vector3.zero;
		extractionPointList = new List<ExtractionPointData>();
		drawEspBool = false;
		drawItemEspBool = false;
		draw3DItemEspBool = false;
		drawPlayerEspBool = false;
		draw2DPlayerEspBool = false;
		draw3DPlayerEspBool = true;
		drawExtractionPointEspBool = false;
		showEnemyNames = true;
		showEnemyDistance = true;
		showEnemyHP = true;
		showItemNames = true;
		showItemValue = true;
		showItemDistance = false;
		showPlayerDeathHeads = true;
		showExtractionNames = true;
		showExtractionDistance = true;
		showPlayerNames = true;
		showPlayerDistance = true;
		showPlayerHP = true;
		enemyOriginalMaterials = new Dictionary<Renderer, Material[]>();
		enemyVisibleColor = new Color(0f, 0.5f, 0.1f, 1f);
		enemyHiddenColor = new Color(0.4f, 0.04f, 0.2f, 0.5f);
		itemVisibleColor = new Color(0.6f, 0.6f, 0f, 0.85f);
		itemHiddenColor = new Color(0.6f, 0.3f, 0f, 0.4f);
		itemOriginalMaterials = new Dictionary<Renderer, Material[]>();
		enemyCachedOriginalCamera = false;
		enemyOriginalFarClipPlane = 0f;
		enemyOriginalDepthTextureMode = (DepthTextureMode)0;
		enemyOriginalOcclusionCulling = false;
		itemCachedOriginalCamera = false;
		itemOriginalFarClipPlane = 0f;
		itemOriginalDepthTextureMode = (DepthTextureMode)0;
		itemOriginalOcclusionCulling = false;
		playerDataList = new List<PlayerData>();
		playerUpdateInterval = 1f;
		playerHealthCache = new Dictionary<int, int>();
		lastPlayerUpdateTime = 0f;
		enemyHealthCache = new Dictionary<Enemy, int>();
		_levelAnimationStartedField = typeof(LoadingUI).GetField("levelAnimationStarted", BindingFlags.Instance | BindingFlags.NonPublic);
		drawChamsBool = false;
		cachedCamera = null; // 延迟到DrawESP时获取，避免静态构造时Camera.main为空
		UpdateLists();
		UpdateLocalPlayer();
		UpdateExtractionPointList();
		UpdatePlayerDataList();
	}

	private static void UpdatePlayerDataList()
	{
		playerDataList.Clear();
		playerHealthCache.Clear();
		List<PlayerAvatar> list = SemiFunc.PlayerGetList();
		if (list != null)
		{
			foreach (PlayerAvatar item in list)
			{
				if ((Object)(object)item != (Object)null)
				{
					PlayerData playerData = new PlayerData(item);
					if ((Object)(object)playerData.PhotonView != (Object)null && (Object)(object)playerData.Transform != (Object)null)
					{
						playerDataList.Add(playerData);
						int playerHealth = Players.GetPlayerHealth(item);
						playerHealthCache[playerData.PhotonView.ViewID] = playerHealth;
					}
				}
			}
		}
		if (playerDataList.Count != lastPlayerDataCount)
		{
			lastPlayerDataCount = playerDataList.Count;
		}
	}

	private static void UpdateExtractionPointList()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		extractionPointList.Clear();
		Object[] array = Object.FindObjectsOfType(Type.GetType("ExtractionPoint, Assembly-CSharp"));
		if (array == null)
		{
			return;
		}
		Object[] array2 = array;
		foreach (Object obj in array2)
		{
			ExtractionPoint val = (ExtractionPoint)(object)((obj is ExtractionPoint) ? obj : null);
			if ((Object)(object)val != (Object)null && ((Component)val).gameObject.activeInHierarchy)
			{
				FieldInfo field = ((object)val).GetType().GetField("currentState", BindingFlags.Instance | BindingFlags.NonPublic);
				string state = "Unknown";
				if (field != null)
				{
					state = field.GetValue(val)?.ToString() ?? "Unknown";
				}
				Vector3 position = ((Component)val).transform.position;
				extractionPointList.Add(new ExtractionPointData(val, state, position));
				if (Vector3.Distance(position, lastExtractionPosition) > 0.1f)
				{
					lastExtractionPosition = position;
				}
			}
		}
	}

	private static void UpdateLists()
	{
		UpdateExtractionPointList();
		if (extractionPointList.Count != lastExtractionPointCount)
		{
			lastExtractionPointCount = extractionPointList.Count;
		}
		enemyList.Clear();
		enemyHealthCache.Clear();
		Type type = Type.GetType("EnemyDirector, Assembly-CSharp");
		if (type != null)
		{
			object obj = type.GetField("instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
			if (obj != null)
			{
				FieldInfo field = type.GetField("enemiesSpawned", BindingFlags.Instance | BindingFlags.Public);
				if (field != null && field.GetValue(obj) is IEnumerable<object> enumerable)
				{
					foreach (object item in enumerable)
					{
						if (item == null)
						{
							continue;
						}
						FieldInfo fieldInfo = item.GetType().GetField("enemyInstance", BindingFlags.Instance | BindingFlags.NonPublic) ?? item.GetType().GetField("Enemy", BindingFlags.Instance | BindingFlags.NonPublic) ?? item.GetType().GetField("childEnemy", BindingFlags.Instance | BindingFlags.NonPublic);
						if (fieldInfo != null)
						{
							object value = fieldInfo.GetValue(item);
							Enemy val = (Enemy)((value is Enemy) ? value : null);
							if ((Object)(object)val != (Object)null && (Object)(object)((Component)val).gameObject != (Object)null && ((Component)val).gameObject.activeInHierarchy)
							{
								int enemyHealth = Enemies.GetEnemyHealth(val);
								enemyHealthCache[val] = enemyHealth;
								enemyList.Add(val);
							}
						}
					}
				}
			}
		}
		playerList.Clear();
		List<PlayerAvatar> list = SemiFunc.PlayerGetList();
		if (list != null)
		{
			foreach (PlayerAvatar item2 in list)
			{
				if ((Object)(object)item2 != (Object)null)
				{
					playerList.Add(item2);
				}
			}
		}
		lastUpdateTime = Time.time;
		if (enemyList.Count != lastEnemyCount || valuableObjects.Count != lastItemCount || playerList.Count != lastPlayerCount)
		{
			lastEnemyCount = enemyList.Count;
			lastItemCount = valuableObjects.Count;
			lastPlayerCount = playerList.Count;
		}
	}

	private static void UpdateLocalPlayer()
	{
		GameObject val = GetLocalPlayer();
		string text = (((Object)(object)val != (Object)null) ? ((Object)val).name : "null");
		if ((Object)(object)val != (Object)(object)localPlayer || text != lastLocalPlayerName)
		{
			_ = (Object)(object)val != (Object)null;
			lastLocalPlayerName = text;
		}
		localPlayer = val;
	}

	public static bool IsLocalPlayer(object player)
	{
		try
		{
			if ((Object)(object)localPlayer == (Object)null)
			{
				UpdateLocalPlayer();
				if ((Object)(object)localPlayer == (Object)null)
				{
					return false;
				}
			}
			GameObject val = (GameObject)((player is GameObject) ? player : null);
			if (val != null)
			{
				return (Object)(object)val == (Object)(object)localPlayer;
			}
			MonoBehaviour val2 = (MonoBehaviour)((player is MonoBehaviour) ? player : null);
			if (val2 != null)
			{
				return (Object)(object)((Component)val2).gameObject == (Object)(object)localPlayer;
			}
			PropertyInfo property = player.GetType().GetProperty("gameObject");
			if (property != null)
			{
				object value = property.GetValue(player);
				return (Object)((value is GameObject) ? value : null) == (Object)(object)localPlayer;
			}
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static GameObject GetLocalPlayer()
	{
		if (PhotonNetwork.IsConnected)
		{
			List<PlayerAvatar> list = SemiFunc.PlayerGetList();
			if (list != null)
			{
				foreach (PlayerAvatar item in list)
				{
					FieldInfo field = ((object)item).GetType().GetField("photonView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (!(field != null))
					{
						continue;
					}
					object value = field.GetValue(item);
					PhotonView val = (PhotonView)((value is PhotonView) ? value : null);
					if (!((Object)(object)val != (Object)null) || !val.IsMine)
					{
						continue;
					}
					PropertyInfo property = ((object)item).GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
					if (property != null)
					{
						object value2 = property.GetValue(item);
						object obj = ((value2 is GameObject) ? value2 : null);
						string name = ((Object)obj).name;
						if (name != lastLocalPlayerName)
						{
							lastLocalPlayerName = name;
						}
						return (GameObject)obj;
					}
					string name2 = ((Object)((Component)val).gameObject).name;
					if (name2 != lastLocalPlayerName)
					{
						lastLocalPlayerName = name2;
					}
					return ((Component)val).gameObject;
				}
			}
			if (PhotonNetwork.LocalPlayer != null)
			{
				PhotonView[] array = SceneCache.GetObjects<PhotonView>(1f);
				foreach (PhotonView val2 in array)
				{
					if (val2.Owner == PhotonNetwork.LocalPlayer && val2.IsMine)
					{
						string name3 = ((Object)((Component)val2).gameObject).name;
						if (name3 != lastLocalPlayerName)
						{
							lastLocalPlayerName = name3;
						}
						return ((Component)val2).gameObject;
					}
				}
			}
			return null;
		}
		List<PlayerAvatar> list2 = SemiFunc.PlayerGetList();
		if (list2 != null && list2.Count > 0)
		{
			PlayerAvatar val3 = list2[0];
			PropertyInfo property2 = ((object)val3).GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
			if (property2 != null)
			{
				object value3 = property2.GetValue(val3);
				object obj2 = ((value3 is GameObject) ? value3 : null);
				string name4 = ((Object)obj2).name;
				if (name4 != lastLocalPlayerName)
				{
					lastLocalPlayerName = name4;
				}
				return (GameObject)obj2;
			}
		}
		Type type = Type.GetType("PlayerAvatar, Assembly-CSharp");
		if (type != null)
		{
			PlayerAvatar[] cachedAvatars = SceneCache.GetObjects<PlayerAvatar>(0.5f);
			Object obj3 = (cachedAvatars != null && cachedAvatars.Length > 0) ? cachedAvatars[0] : null;
			MonoBehaviour val4 = (MonoBehaviour)(object)((obj3 is MonoBehaviour) ? obj3 : null);
			if ((Object)(object)val4 != (Object)null)
			{
				string name5 = ((Object)((Component)val4).gameObject).name;
				if (name5 != lastLocalPlayerName)
				{
					lastLocalPlayerName = name5;
				}
				return ((Component)val4).gameObject;
			}
		}
		GameObject val5 = GameObject.FindWithTag("Player");
		if ((Object)(object)val5 != (Object)null)
		{
			string name6 = ((Object)val5).name;
			if (name6 != lastLocalPlayerName)
			{
				lastLocalPlayerName = name6;
			}
			return val5;
		}
		GameObject[] array2 = SceneCache.GetObjects<GameObject>(1f);
		foreach (GameObject val6 in array2)
		{
			if (((Object)val6).name.Contains("Player") && val6.activeInHierarchy)
			{
				string name7 = ((Object)val6).name;
				if (name7 != lastLocalPlayerName)
				{
					lastLocalPlayerName = name7;
				}
				return val6;
			}
		}
		return null;
	}

	public static void UpdateEnemyList()
	{
		EspOverlay.RefreshLists();
	}

	public static void RectFilled(float x, float y, float width, float height, Texture2D text)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)text == (Object)null) return;
		GUI.DrawTexture(new Rect(x, y, width, height), (Texture)(object)text);
	}

	public static void RectOutlined(float x, float y, float width, float height, Texture2D text, float thickness = 1f)
	{
		RectFilled(x, y, thickness, height, text);
		RectFilled(x + width - thickness, y, thickness, height, text);
		RectFilled(x + thickness, y, width - thickness * 2f, thickness, text);
		RectFilled(x + thickness, y + height - thickness, width - thickness * 2f, thickness, text);
	}

	public static void Box(float x, float y, float width, float height, Texture2D text, float thickness = 2f)
	{
		RectOutlined(x - width / 2f, y - height, width, height, text, thickness);
	}

	public static void InitializeStyles()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0057: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected O, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Expected O, but got Unknown
		if (nameStyle == null)
		{
			GUIStyle val = new GUIStyle(GUI.skin.label);
			val.normal.textColor = Color.yellow;
			val.alignment = (TextAnchor)4;
			val.fontSize = 14;
			val.fontStyle = (FontStyle)1;
			val.wordWrap = true;
			val.border = new RectOffset(1, 1, 1, 1);
			nameStyle = val;
		}
		if (valueStyle == null)
		{
			GUIStyle val2 = new GUIStyle(GUI.skin.label);
			val2.normal.textColor = Color.green;
			val2.alignment = (TextAnchor)4;
			val2.fontSize = 12;
			val2.fontStyle = (FontStyle)1;
			valueStyle = val2;
		}
		if (enemyStyle == null)
		{
			enemyStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)4,
				wordWrap = true,
				fontSize = 12,
				fontStyle = (FontStyle)1
			};
		}
		if (healthStyle == null)
		{
			GUIStyle val3 = new GUIStyle(GUI.skin.label);
			val3.normal.textColor = Color.green;
			val3.alignment = (TextAnchor)4;
			val3.fontSize = 12;
			val3.fontStyle = (FontStyle)1;
			healthStyle = val3;
		}
		if (distanceStyle == null)
		{
			GUIStyle val4 = new GUIStyle(GUI.skin.label);
			val4.normal.textColor = Color.yellow;
			val4.alignment = (TextAnchor)4;
			val4.fontSize = 12;
			val4.fontStyle = (FontStyle)1;
			distanceStyle = val4;
		}
	}

	private static void CreateBoundsEdges(Bounds bounds, Color color)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		Vector3[] array = (Vector3[])(object)new Vector3[8];
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		array[0] = new Vector3(min.x, min.y, min.z);
		array[1] = new Vector3(max.x, min.y, min.z);
		array[2] = new Vector3(max.x, min.y, max.z);
		array[3] = new Vector3(min.x, min.y, max.z);
		array[4] = new Vector3(min.x, max.y, min.z);
		array[5] = new Vector3(max.x, max.y, min.z);
		array[6] = new Vector3(max.x, max.y, max.z);
		array[7] = new Vector3(min.x, max.y, max.z);
		Vector2[] array2 = (Vector2[])(object)new Vector2[8];
		bool flag = false;
		for (int i = 0; i < 8; i++)
		{
			Vector3 val = cachedCamera.WorldToScreenPoint(array[i]);
			if (val.z > 0f)
			{
				flag = true;
			}
			array2[i] = new Vector2(val.x * scaleX, (float)Screen.height - val.y * scaleY);
		}
		if (flag)
		{
			DrawLine(array2[0], array2[1], color);
			DrawLine(array2[1], array2[2], color);
			DrawLine(array2[2], array2[3], color);
			DrawLine(array2[3], array2[0], color);
			DrawLine(array2[4], array2[5], color);
			DrawLine(array2[5], array2[6], color);
			DrawLine(array2[6], array2[7], color);
			DrawLine(array2[7], array2[4], color);
			DrawLine(array2[0], array2[4], color);
			DrawLine(array2[1], array2[5], color);
			DrawLine(array2[2], array2[6], color);
			DrawLine(array2[3], array2[7], color);
		}
	}

	private static void DrawLine(Vector2 start, Vector2 end, Color color)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)texture2 == (Object)null))
		{
			float num = Vector2.Distance(start, end);
			float num2 = Mathf.Atan2(end.y - start.y, end.x - start.x) * 57.29578f;
			GUI.color = color;
			Matrix4x4 matrix = GUI.matrix;
			GUIUtility.RotateAroundPivot(num2, start);
			GUI.DrawTexture(new Rect(start.x, start.y, num, 1f), (Texture)(object)texture2);
			GUI.matrix = matrix;
			GUI.color = Color.white;
		}
	}

	private static Bounds GetActiveColliderBounds(GameObject obj)
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		Collider[] componentsInChildren = obj.GetComponentsInChildren<Collider>(true);
		Bounds bounds2 = default(Bounds);
		int activeColliders = 0;
		Collider[] array = componentsInChildren;
		foreach (Collider val in array)
		{
			if (val.enabled && ((Component)val).gameObject.activeInHierarchy)
			{
				if (activeColliders == 0)
				{
					bounds2 = val.bounds;
				}
				else
				{
					bounds2.Encapsulate(val.bounds);
				}
				activeColliders++;
			}
		}
		if (activeColliders == 0)
		{
			Renderer[] componentsInChildren2 = obj.GetComponentsInChildren<Renderer>(true);
			if (componentsInChildren2.Length != 0)
			{
				Bounds bounds = componentsInChildren2[0].bounds;
				for (int j = 1; j < componentsInChildren2.Length; j++)
				{
					if (componentsInChildren2[j].enabled && ((Component)componentsInChildren2[j]).gameObject.activeInHierarchy)
					{
						bounds.Encapsulate(componentsInChildren2[j].bounds);
					}
				}
				return bounds;
			}
			return new Bounds(obj.transform.position, Vector3.one * 0.5f);
		}
		bounds2.Expand(0.1f);
		return bounds2;
	}

	public static void DrawESP()
	{
		EspOverlay.Draw();
	}
}
