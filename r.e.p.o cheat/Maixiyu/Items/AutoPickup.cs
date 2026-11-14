using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// Auto pickup teleports nearby valuables to the player.
/// Auto sell teleports only the grabbed / grab-ray hovered valuable into an
/// unlocked extraction (Active, or Idle READY). Locked and completed points are skipped.
/// </summary>
public static class AutoPickup
{
	public static bool isAutoPickupEnabled = false;
	public static bool isAutoSellEnabled = false;
	public static string SellStatus = "";

	public static float pickupRadius = 30f;
	public static int minPickupValue = 100;
	public static float pickupInterval = 1.5f;

	private static float lastPickupTime = 0f;
	private static float lastSellTime = 0f;
	private static float sellInterval = 0.2f;

	private static readonly FieldInfo GrabbedPhysField = AccessTools.Field(typeof(PhysGrabber), "grabbedPhysGrabObject");
	private static readonly FieldInfo LookingAtField = AccessTools.Field(typeof(PhysGrabber), "currentlyLookingAtPhysGrabObject");
	private static readonly FieldInfo RoomVolumeCheckField = AccessTools.Field(typeof(PhysGrabObject), "roomVolumeCheck");
	private static readonly FieldInfo InExtractionField = AccessTools.Field(typeof(RoomVolumeCheck), "inExtractionPoint");

	public static void UpdateAutoPickup()
	{
		if (!isAutoPickupEnabled && !isAutoSellEnabled)
		{
			return;
		}

		try
		{
			if (isAutoPickupEnabled && Time.time - lastPickupTime >= pickupInterval)
			{
				lastPickupTime = Time.time;
				PickupNearbyItems();
			}

			if (isAutoSellEnabled && Time.time - lastSellTime >= sellInterval)
			{
				lastSellTime = Time.time;
				SellItemsToExtraction();
			}
		}
		catch (Exception ex)
		{
			SellStatus = ex.Message;
			Debug.LogError((object)("[自动拾取] 错误: " + ex.Message));
		}
	}

	private static void PickupNearbyItems()
	{
		try
		{
			GameObject localPlayer = DebugCheats.GetLocalPlayer();
			if ((Object)(object)localPlayer == (Object)null)
			{
				return;
			}

			Vector3 playerPos = localPlayer.transform.position;
			Quaternion playerRot = localPlayer.transform.rotation;
			ValuableObject[] valuables = SceneCache.GetObjects<ValuableObject>(0.5f);
			if (valuables == null || valuables.Length == 0)
			{
				return;
			}

			foreach (ValuableObject valuable in valuables)
			{
				if (!IsSellableValuable(valuable))
				{
					continue;
				}

				Transform t = ((Component)valuable).transform;
				float dist = Vector3.Distance(playerPos, t.position);
				if (dist > pickupRadius || dist < 1.2f)
				{
					continue;
				}

				if (GetItemValue(valuable) < minPickupValue)
				{
					continue;
				}

				Vector3 targetPos = MaiItemTeleport.SnapToGround(playerPos + localPlayer.transform.forward * 1f);
				MaiItemTeleport.TeleportComponent((Component)valuable, targetPos, playerRot);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[自动拾取] 拾取失败: " + ex.Message));
		}
	}

	private static void SellItemsToExtraction()
	{
		try
		{
			PhysGrabObject phys = GetHoveredPhys();
			if ((Object)(object)phys == (Object)null)
			{
				SellStatus = L.T("items.auto_sell_aim");
				return;
			}

			ValuableObject valuable = phys.GetComponent<ValuableObject>()
				?? phys.GetComponentInParent<ValuableObject>()
				?? phys.GetComponentInChildren<ValuableObject>();
			if ((Object)(object)valuable == (Object)null || !IsSellableValuable(valuable))
			{
				SellStatus = L.T("items.auto_sell_not_valuable");
				return;
			}

			if (!MaiItemTeleport.TryGetOpenExtractionDrop(out Vector3 dropPos, out Quaternion dropRot, out string extractLabel))
			{
				SellStatus = L.T("items.auto_sell_no_extract");
				return;
			}

			if (IsAlreadyInExtraction(phys) || Vector3.Distance(dropPos, ((Component)valuable).transform.position) < 1.2f)
			{
				SellStatus = L.T("items.auto_sell_in_zone");
				return;
			}

			ReleaseIfGrabbed();
			if (!MaiItemTeleport.TeleportComponent((Component)valuable, dropPos, dropRot))
			{
				SellStatus = L.T("items.auto_sell_no_extract");
				return;
			}

			SellStatus = L.T("items.auto_sell_sent", extractLabel);
		}
		catch (Exception ex)
		{
			SellStatus = ex.Message;
			Debug.LogError((object)("[自动卖出] 失败: " + ex.Message));
		}
	}

	private static PhysGrabObject GetHoveredPhys()
	{
		PhysGrabber grabber = GetLocalGrabber();
		PhysGrabObject phys = null;

		if ((Object)(object)grabber != (Object)null)
		{
			if (grabber.grabbed)
			{
				phys = ComponentToPhys(grabber.grabbedObjectTransform);
				if ((Object)(object)phys == (Object)null)
				{
					phys = GrabbedPhysField?.GetValue(grabber) as PhysGrabObject;
				}
			}
			if ((Object)(object)phys == (Object)null)
			{
				phys = LookingAtField?.GetValue(grabber) as PhysGrabObject;
			}
		}

		if ((Object)(object)phys == (Object)null || phys.dead)
		{
			return null;
		}
		return phys;
	}

	private static PhysGrabObject ComponentToPhys(Component source)
	{
		if ((Object)(object)source == (Object)null)
		{
			return null;
		}
		return source.GetComponent<PhysGrabObject>()
			?? source.GetComponentInParent<PhysGrabObject>()
			?? source.GetComponentInChildren<PhysGrabObject>();
	}

	private static PhysGrabObject ComponentToPhys(GameObject source)
	{
		if ((Object)(object)source == (Object)null)
		{
			return null;
		}
		return source.GetComponent<PhysGrabObject>()
			?? source.GetComponentInParent<PhysGrabObject>()
			?? source.GetComponentInChildren<PhysGrabObject>();
	}

	private static void ReleaseIfGrabbed()
	{
		PhysGrabber grabber = GetLocalGrabber();
		if ((Object)(object)grabber == (Object)null || !grabber.grabbed)
		{
			return;
		}
		try
		{
			grabber.ReleaseObject(-1);
		}
		catch
		{
		}
	}

	private static PhysGrabber GetLocalGrabber()
	{
		if ((Object)(object)PhysGrabber.instance != (Object)null)
		{
			return PhysGrabber.instance;
		}
		PlayerAvatar avatar = PlayerAvatar.instance;
		if ((Object)(object)avatar != (Object)null)
		{
			return avatar.physGrabber;
		}
		return null;
	}

	private static bool IsAlreadyInExtraction(PhysGrabObject phys)
	{
		try
		{
			object check = RoomVolumeCheckField?.GetValue(phys);
			if (check == null)
			{
				return false;
			}
			object flag = InExtractionField?.GetValue(check);
			return flag is bool inside && inside;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSellableValuable(ValuableObject valuable)
	{
		if ((Object)(object)valuable == (Object)null)
		{
			return false;
		}
		GameObject go = ((Component)valuable).gameObject;
		if ((Object)(object)go == (Object)null || !go.activeInHierarchy)
		{
			return false;
		}
		if (go.GetComponent<PlayerDeathHead>() != null || go.GetComponentInParent<PlayerDeathHead>() != null)
		{
			return false;
		}
		return true;
	}

	private static int GetItemValue(ValuableObject valuable)
	{
		try
		{
			FieldInfo dollarValueField = typeof(ValuableObject).GetField("dollarValueCurrent",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (dollarValueField != null)
			{
				return Convert.ToInt32(dollarValueField.GetValue(valuable));
			}

			FieldInfo valueField = typeof(ValuableObject).GetField("dollarValueOriginal",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (valueField != null)
			{
				return Convert.ToInt32(valueField.GetValue(valuable));
			}
		}
		catch
		{
		}
		return 0;
	}
}
