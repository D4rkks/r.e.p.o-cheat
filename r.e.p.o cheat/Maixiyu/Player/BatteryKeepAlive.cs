using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// Item battery charge/drain/bar-sync runs on the master client only.
/// These patches keep the local copy full, skip host-side drain, and let
/// guests still fire staves (PhotonNetwork.Instantiate is not MasterOnly).
/// Guns/drones still need the host's battery — their shoot RPCs are MasterOnly.
/// </summary>
public static class BatteryKeepAlive
{
	private static readonly FieldInfo BatteryLifeIntField = AccessTools.Field(typeof(ItemBattery), "batteryLifeInt");
	private static readonly FieldInfo BatteryLifePrevField = AccessTools.Field(typeof(ItemBattery), "batteryLifePrev");
	private static readonly FieldInfo CurrentBarsField = AccessTools.Field(typeof(ItemBattery), "currentBars");
	private static readonly FieldInfo DebugInfiniteBatteryField = AccessTools.Field(typeof(RoundDirector), "debugInfiniteBattery");
	private static readonly FieldInfo GunBatteryField = AccessTools.Field(typeof(ItemGun), "itemBattery");
	private static readonly ConditionalWeakTable<ItemBattery, HostBarState> HostBars = new ConditionalWeakTable<ItemBattery, HostBarState>();

	private static int guestStaffCastDepth;

	private sealed class HostBarState
	{
		public int Bars = -1;
	}

	public static bool IsActive
	{
		get
		{
			return Hax2.unlimitedBatteryActive
				|| ((Object)Hax2.unlimitedBatteryComponent != null && Hax2.unlimitedBatteryComponent.unlimitedBatteryEnabled);
		}
	}

	public static void ApplyDirectorFlag()
	{
		try
		{
			if (RoundDirector.instance == null || DebugInfiniteBatteryField == null)
			{
				return;
			}
			DebugInfiniteBatteryField.SetValue(RoundDirector.instance, IsActive);
		}
		catch
		{
		}
	}

	public static bool IsLocallyUsing(ItemBattery battery)
	{
		if ((Object)battery == null)
		{
			return false;
		}
		try
		{
			PhysGrabObject grab = battery.GetComponent<PhysGrabObject>() ?? battery.GetComponentInChildren<PhysGrabObject>();
			if ((Object)grab != null)
			{
				if (grab.grabbedLocal)
				{
					return true;
				}
				if (grab.playerGrabbing != null)
				{
					foreach (PhysGrabber grabber in grab.playerGrabbing)
					{
						if ((Object)grabber != null && grabber.isLocal)
						{
							return true;
						}
					}
				}
			}
			ItemEquippable equippable = battery.GetComponent<ItemEquippable>();
			if ((Object)equippable != null && equippable.IsEquipped() && Inventory.instance != null && Inventory.instance.IsItemEquipped(equippable))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public static bool ShouldKeep(ItemBattery battery)
	{
		if (!IsActive || (Object)battery == null)
		{
			return false;
		}
		if (NativeGameApi.IsHost())
		{
			return true;
		}
		return IsLocallyUsing(battery);
	}

	public static int GetHostBars(ItemBattery battery)
	{
		if ((Object)battery == null)
		{
			return -1;
		}
		if (HostBars.TryGetValue(battery, out HostBarState state) && state != null && state.Bars >= 0)
		{
			return state.Bars;
		}
		return battery.batteryBars;
	}

	public static void ForceFill(ItemBattery battery, bool syncIfHost)
	{
		if ((Object)battery == null)
		{
			return;
		}
		try
		{
			int bars = Mathf.Max(1, battery.batteryBars);
			int previousInt = BatteryLifeIntField != null ? (int)BatteryLifeIntField.GetValue(battery) : 0;
			if (battery.batteryLife <= 0f)
			{
				battery.batteryLife = 100f;
			}
			battery.batteryLife = 100f;
			if (BatteryLifeIntField != null)
			{
				BatteryLifeIntField.SetValue(battery, bars);
			}
			if (BatteryLifePrevField != null)
			{
				BatteryLifePrevField.SetValue(battery, 100f);
			}
			if (CurrentBarsField != null)
			{
				CurrentBarsField.SetValue(battery, bars);
			}
			if (syncIfHost && NativeGameApi.IsHost() && previousInt < bars)
			{
				battery.SetBatteryLife(100);
			}
			else if (previousInt <= 0)
			{
				battery.SetBatteryLife(100);
			}
		}
		catch
		{
		}
	}

	private static bool ShouldGuestCast(object staff)
	{
		if (!IsActive || NativeGameApi.IsHost() || staff == null)
		{
			return false;
		}
		try
		{
			FieldInfo grabField = AccessTools.Field(staff.GetType(), "physGrabObject");
			PhysGrabObject grab = grabField?.GetValue(staff) as PhysGrabObject;
			if ((Object)grab == null || !grab.grabbedLocal)
			{
				return false;
			}
			FieldInfo batteryField = AccessTools.Field(staff.GetType(), "itemBattery");
			ItemBattery battery = batteryField?.GetValue(staff) as ItemBattery;
			if ((Object)battery == null)
			{
				return false;
			}
			ForceFill(battery, false);
			return GetHostBars(battery) <= 0;
		}
		catch
		{
			return false;
		}
	}

	[HarmonyPatch(typeof(ItemBattery), "Update")]
	private static class PatchBatteryUpdate
	{
		[HarmonyPostfix]
		private static void Postfix(ItemBattery __instance)
		{
			if (!ShouldKeep(__instance))
			{
				return;
			}
			ApplyDirectorFlag();
			ForceFill(__instance, NativeGameApi.IsHost());
		}
	}

	[HarmonyPatch(typeof(ItemBattery), "Drain")]
	private static class PatchBatteryDrain
	{
		[HarmonyPrefix]
		private static bool Prefix(ItemBattery __instance)
		{
			return !ShouldKeep(__instance);
		}
	}

	[HarmonyPatch(typeof(ItemBattery), "RemoveFullBar")]
	private static class PatchRemoveFullBar
	{
		[HarmonyPrefix]
		private static bool Prefix(ItemBattery __instance)
		{
			return !ShouldKeep(__instance);
		}
	}

	[HarmonyPatch(typeof(ItemBattery), "SetBatteryLife")]
	private static class PatchSetBatteryLife
	{
		[HarmonyPrefix]
		private static void Prefix(ItemBattery __instance)
		{
			if (!ShouldKeep(__instance) || (Object)__instance == null)
			{
				return;
			}
			if (__instance.batteryLife <= 0f)
			{
				__instance.batteryLife = 100f;
			}
		}
	}

	[HarmonyPatch(typeof(ItemBattery), "BatteryFullPercentChangeLogic")]
	private static class PatchBarSync
	{
		[HarmonyPrefix]
		private static void Prefix(ItemBattery __instance, int batteryLevel)
		{
			if ((Object)__instance == null)
			{
				return;
			}
			if (!HostBars.TryGetValue(__instance, out HostBarState state) || state == null)
			{
				state = new HostBarState();
				try
				{
					HostBars.Add(__instance, state);
				}
				catch
				{
					if (!HostBars.TryGetValue(__instance, out state) || state == null)
					{
						return;
					}
				}
			}
			state.Bars = batteryLevel;
		}

		[HarmonyPostfix]
		private static void Postfix(ItemBattery __instance)
		{
			if (ShouldKeep(__instance))
			{
				ForceFill(__instance, false);
			}
		}
	}

	[HarmonyPatch(typeof(ItemGun), "Shoot")]
	private static class PatchGunShoot
	{
		[HarmonyPrefix]
		private static void Prefix(ItemGun __instance)
		{
			if (!IsActive || (Object)__instance == null)
			{
				return;
			}
			ItemBattery battery = GunBatteryField?.GetValue(__instance) as ItemBattery;
			if (ShouldKeep(battery))
			{
				ForceFill(battery, NativeGameApi.IsHost());
			}
		}
	}

	[HarmonyPatch]
	private static class PatchStaffCast
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			MethodBase torque = AccessTools.Method(typeof(ItemStaffTorque), "CastSpell");
			MethodBase voidStaff = AccessTools.Method(typeof(ItemStaffVoid), "CastSpell");
			MethodBase zeroG = AccessTools.Method(typeof(ItemStaffZeroGravity), "CastSpell");
			if (torque != null)
			{
				yield return torque;
			}
			if (voidStaff != null)
			{
				yield return voidStaff;
			}
			if (zeroG != null)
			{
				yield return zeroG;
			}
		}

		[HarmonyPrefix]
		private static void Prefix(object __instance, ref bool __state)
		{
			__state = ShouldGuestCast(__instance);
			if (__state)
			{
				guestStaffCastDepth++;
			}
		}

		[HarmonyPostfix]
		private static void Postfix(bool __state)
		{
			if (__state && guestStaffCastDepth > 0)
			{
				guestStaffCastDepth--;
			}
		}
	}

	[HarmonyPatch(typeof(SemiFunc), "IsMasterClientOrSingleplayer")]
	private static class PatchActAsMasterForGuestStaff
	{
		[HarmonyPostfix]
		private static void Postfix(ref bool __result)
		{
			if (guestStaffCastDepth > 0)
			{
				__result = true;
			}
		}
	}
}
