using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

public static class MaiItemTeleport
{
	public class GameItem
	{
		public string Name { get; set; }

		public int Value { get; set; }

		public object ItemObject { get; set; }

		public GameItem(string name, int value, object itemObject = null)
		{
			Name = name;
			Value = value;
			ItemObject = itemObject;
		}
	}

	private static PhotonView punManagerPhotonView;

	public static void SetItemValue(GameItem selectedItem, int newValue)
	{
		if (selectedItem == null || selectedItem.ItemObject == null)
		{
			Debug.Log((object)"错误：所选物品或 ItemObject 为空！");
			return;
		}
		try
		{
			object itemObject = selectedItem.ItemObject;
			Object val = (Object)((itemObject is Object) ? itemObject : null);
			if (val == (Object)null)
			{
				Debug.Log((object)"错误：ItemObject 不是 UnityEngine.Object！");
				return;
			}
			object obj = ((val is GameObject) ? val : null);
			if (obj == null)
			{
				Object obj2 = ((val is Component) ? val : null);
				obj = ((obj2 != null) ? ((Component)obj2).gameObject : null);
			}
			GameObject val2 = (GameObject)obj;
			if ((Object)(object)val2 == (Object)null)
			{
				Debug.Log((object)"错误：无法从 ItemObject 获取 GameObject！");
				return;
			}
			PhotonView component = val2.GetComponent<PhotonView>();
			ValuableObject valuable = val2.GetComponent<ValuableObject>() ?? val2.GetComponentInParent<ValuableObject>();
			if ((Object)(object)valuable != (Object)null)
			{
				FieldInfo dollar = AccessTools.Field(typeof(ValuableObject), "dollarValueCurrent");
				dollar?.SetValue(valuable, (float)newValue);
			}
			if ((Object)(object)component != (Object)null && NativeGameApi.IsHost())
			{
				component.RPC("DollarValueSetRPC", RpcTarget.Others, (float)newValue);
				Debug.Log((object)$"已通过 RPC 将“{selectedItem.Name}”的价值设置为 ${newValue}");
			}
			else
			{
				Type type = selectedItem.ItemObject.GetType();
				FieldInfo fieldInfo = type.GetField("dollarValueCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? type.GetField("dollarValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (fieldInfo == null)
				{
					Debug.Log((object)("错误：在 “" + selectedItem.Name + "” 中未找到 'dollarValueCurrent' 字段"));
					return;
				}
				fieldInfo.SetValue(selectedItem.ItemObject, newValue);
				Debug.Log((object)$"已在本地将“{selectedItem.Name}”的价值设置为 ${newValue}（未找到 PhotonView）");
			}
			selectedItem.Value = newValue;
		}
		catch (Exception ex)
		{
			Debug.Log((object)("设置“" + selectedItem.Name + "”的价值时出错：" + ex.Message));
		}
	}

	private static void InitializePunManager()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		if ((Object)(object)punManagerPhotonView == (Object)null)
		{
			Type type = Type.GetType("PunManager, Assembly-CSharp");
			object obj = GameHelper.FindObjectOfType(type);
			if (obj != null)
			{
				punManagerPhotonView = (PhotonView)(type.GetField("photonView", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj));
				_ = (Object)(object)punManagerPhotonView == (Object)null;
			}
		}
	}

	public static List<GameItem> GetItemList()
	{
		List<GameItem> list = new List<GameItem>();
		foreach (object valuableObject in DebugCheats.valuableObjects)
		{
			if (valuableObject == null)
			{
				continue;
			}
			if (valuableObject is CosmeticWorldObject cube)
			{
				list.Add(new GameItem(L.T("item.name.Cube") + " " + CosmeticFeatures.RarityLabel(cube.rarity), 0, cube));
				continue;
			}
			PropertyInfo property = valuableObject.GetType().GetProperty("transform", BindingFlags.Instance | BindingFlags.Public);
			if (property == null)
			{
				continue;
			}
			object value = property.GetValue(valuableObject);
			Transform val = (Transform)((value is Transform) ? value : null);
			if ((Object)(object)val == (Object)null || !((Component)val).gameObject.activeInHierarchy)
			{
				continue;
			}
			if (valuableObject is PlayerDeathHead head && !IsDeathHeadReady(head))
			{
				continue;
			}
			string text;
			try
			{
				text = valuableObject.GetType().GetProperty("name", BindingFlags.Instance | BindingFlags.Public)?.GetValue(valuableObject) as string;
				if (string.IsNullOrEmpty(text))
				{
					object obj = ((valuableObject is Object) ? valuableObject : null);
					text = ((obj != null) ? ((Object)obj).name : null) ?? "Unknown";
				}
			}
			catch (Exception)
			{
				object obj2 = ((valuableObject is Object) ? valuableObject : null);
				text = ((obj2 != null) ? ((Object)obj2).name : null) ?? "Unknown";
			}
			if (text.StartsWith("Valuable", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring("Valuable".Length).Trim();
			}
			if (text.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(0, text.Length - "(Clone)".Length).Trim();
			}
			int value2 = 0;
			if (valuableObject.GetType().Name != "PlayerDeathHead")
			{
				FieldInfo fieldInfo = valuableObject.GetType().GetField("dollarValueCurrent", BindingFlags.Instance | BindingFlags.Public) ?? valuableObject.GetType().GetField("dollarValue", BindingFlags.Instance | BindingFlags.Public);
				if (fieldInfo != null)
				{
					try
					{
						value2 = Convert.ToInt32(fieldInfo.GetValue(valuableObject));
					}
					catch (Exception)
					{
					}
				}
			}
			list.Add(new GameItem(text, value2, valuableObject));
		}
		if (list.Count == 0)
		{
			list.Add(new GameItem(L.T("items.no_items"), 0));
		}
		return list;
	}

	public static void TeleportItemToMe(GameItem selectedItem)
	{
		if (selectedItem != null && selectedItem.ItemObject != null)
		{
			PerformTeleport(selectedItem);
		}
	}

	public static void TeleportAllItemsToMe()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			GameObject localPlayer = DebugCheats.GetLocalPlayer();
			if ((Object)(object)localPlayer == (Object)null)
			{
				return;
			}
			_ = localPlayer.transform.position + localPlayer.transform.forward * 1f + Vector3.up * 1.5f;
			List<GameItem> itemList = GetItemList();
			int num = 0;
			foreach (GameItem item in itemList)
			{
				if (item.ItemObject != null)
				{
					PerformTeleport(item);
					num++;
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void TeleportSelectedItemToMe(GameItem selectedItem)
	{
		if (selectedItem != null && selectedItem.ItemObject != null)
		{
			PerformTeleport(selectedItem);
		}
	}

	public static bool TeleportComponent(Component source, Vector3 position, Quaternion rotation)
	{
		if ((Object)(object)source == (Object)null)
		{
			return false;
		}
		PhysGrabObject body = ResolvePhysGrabObject(source);
		if ((Object)(object)body != (Object)null)
		{
			PrepareDeathHead(source);
			body.Teleport(SnapToGround(position), rotation);
			return true;
		}
		source.transform.SetPositionAndRotation(SnapToGround(position), rotation);
		return true;
	}

	public static Vector3 SnapToGround(Vector3 origin)
	{
		Vector3 start = origin + Vector3.up * 0.35f;
		int mask = ~LayerMask.GetMask("Ignore Raycast", "Player", "Enemy");
		if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 6f, mask, QueryTriggerInteraction.Ignore))
		{
			return hit.point + Vector3.up * 0.04f;
		}
		return origin;
	}

	public static bool TryGetOpenExtractionDrop(out Vector3 dropPos, out Quaternion dropRot, out string extractLabel)
	{
		dropPos = Vector3.zero;
		dropRot = Quaternion.identity;
		extractLabel = "";
		GameObject localPlayer = DebugCheats.GetLocalPlayer();
		Vector3 playerPos = (Object)(object)localPlayer != (Object)null ? localPlayer.transform.position : Vector3.zero;
		ExtractionPoint bestOpen = null;
		ExtractionPoint bestReady = null;
		float bestOpenDist = float.MaxValue;
		float bestReadyDist = float.MaxValue;
		FieldInfo stateField = AccessTools.Field(typeof(ExtractionPoint), "currentState");
		ExtractionPoint[] points = SceneCache.GetObjects<ExtractionPoint>(0.5f);
		if (points == null)
		{
			return false;
		}
		foreach (ExtractionPoint point in points)
		{
			if ((Object)(object)point == (Object)null || !((Component)point).gameObject.activeInHierarchy)
			{
				continue;
			}
			string state = stateField?.GetValue(point)?.ToString() ?? "";
			float dist = Vector3.Distance(playerPos, ((Component)point).transform.position);
			if (state == "Active" || state == "Success" || state == "Surplus" || state == "Warning" || state == "Extracting" || state == "TaxReturn")
			{
				if (dist < bestOpenDist)
				{
					bestOpenDist = dist;
					bestOpen = point;
				}
			}
			else if (state == "Idle" && !point.isLocked && dist < bestReadyDist)
			{
				bestReadyDist = dist;
				bestReady = point;
			}
		}
		ExtractionPoint chosen = (Object)(object)bestOpen != (Object)null ? bestOpen : bestReady;
		if ((Object)(object)chosen == (Object)null)
		{
			return false;
		}
		extractLabel = stateField?.GetValue(chosen)?.ToString() ?? "READY";
		Transform pose = chosen.safetySpawn != null ? chosen.safetySpawn : (chosen.platform != null ? chosen.platform : ((Component)chosen).transform);
		dropRot = pose.rotation;
		dropPos = SnapToGround(pose.position);
		return true;
	}

	public static PhysGrabObject ResolvePhysGrabObject(Component source)
	{
		if ((Object)(object)source == (Object)null)
		{
			return null;
		}
		PlayerDeathHead head = source as PlayerDeathHead ?? source.GetComponent<PlayerDeathHead>() ?? source.GetComponentInParent<PlayerDeathHead>();
		if ((Object)(object)head != (Object)null)
		{
			PhysGrabObject fieldBody = AccessTools.Field(typeof(PlayerDeathHead), "physGrabObject")?.GetValue(head) as PhysGrabObject;
			if ((Object)(object)fieldBody != (Object)null)
			{
				return fieldBody;
			}
		}
		return source.GetComponent<PhysGrabObject>()
			?? source.GetComponentInParent<PhysGrabObject>()
			?? source.GetComponentInChildren<PhysGrabObject>(true);
	}

	private static bool IsDeathHeadReady(PlayerDeathHead head)
	{
		if ((Object)(object)head == (Object)null)
		{
			return false;
		}
		FieldInfo triggered = AccessTools.Field(typeof(PlayerDeathHead), "triggered");
		return triggered != null && triggered.GetValue(head) is bool ready && ready;
	}

	private static void PrepareDeathHead(Component source)
	{
		PlayerDeathHead head = source as PlayerDeathHead ?? source.GetComponent<PlayerDeathHead>() ?? source.GetComponentInParent<PlayerDeathHead>();
		if ((Object)(object)head == (Object)null)
		{
			return;
		}
		try
		{
			head.Trigger();
			PhysGrabObject body = ResolvePhysGrabObject(head);
			body?.OverrideDeactivateReset();
			body?.DisableDeathPitEffect(8f);
		}
		catch
		{
		}
	}

	private static void PerformTeleport(GameItem item)
	{
		try
		{
			GameObject localPlayer = DebugCheats.GetLocalPlayer();
			if ((Object)(object)localPlayer == (Object)null || item?.ItemObject == null)
			{
				return;
			}
			Vector3 dest = SnapToGround(localPlayer.transform.position + localPlayer.transform.forward * 1f);
			Quaternion rotation = localPlayer.transform.rotation;
			Component source = item.ItemObject as Component;
			if ((Object)(object)source == (Object)null && item.ItemObject is GameObject go)
			{
				source = go.transform;
			}
			if ((Object)(object)source == (Object)null)
			{
				return;
			}
			TeleportComponent(source, dest, rotation);
		}
		catch (Exception ex)
		{
			Debug.Log((object)("Error teleporting item '" + item.Name + "': " + ex.Message));
		}
	}
}
