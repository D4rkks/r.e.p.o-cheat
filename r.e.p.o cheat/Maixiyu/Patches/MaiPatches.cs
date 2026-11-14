using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

public static class Patches
{
	[HarmonyPatch(typeof(SpectateCamera), "PlayerSwitch")]
	public static class SpectateCamera_PlayerSwitch_Patch
	{
		private static bool Prefix(bool _next)
		{
			return !Hax2.showMenu;
		}
	}

	// UnityEngine.Input mouse methods are native extern calls and cannot be
	// prefix-patched safely. Patch the game's managed Input System facade instead.
	[HarmonyPatch]
	public static class InputManagerMenuButtonPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			string[] names = { "KeyDown", "KeyUp", "KeyHold" };
			for (int i = 0; i < names.Length; i++)
			{
				MethodInfo method = AccessTools.Method(typeof(InputManager), names[i], new[] { typeof(InputKey) });
				if (method != null)
				{
					yield return method;
				}
			}
		}

		private static bool Prefix(ref bool __result)
		{
			if (Hax2.showMenu)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PhysGrabber), "RayCheck")]
	public class Patch_PhysGrabber_RayCheck
	{
		private static bool Prefix(bool _grab)
		{
			if (Hax2.showMenu)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PlayerAvatar), "AddToStatsManager")]
	public class Patch_AddToStatsManager
	{
		private static bool Prefix(PlayerAvatar __instance)
		{
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			if (Hax2.spoofNameActive)
			{
				string persistentNameText = Hax2.persistentNameText;
				string steamId = SemiFunc.PlayerGetSteamID(__instance);
				PhotonNetwork.NickName = persistentNameText;
				if (PhotonNetwork.LocalPlayer != null)
				{
					PhotonNetwork.LocalPlayer.NickName = persistentNameText;
				}
				if (!GameManager.Multiplayer())
				{
					__instance.AddToStatsManagerRPC(persistentNameText, steamId, default(PhotonMessageInfo));
					return false;
				}
				if (__instance.photonView.IsMine)
				{
					__instance.photonView.RPC("AddToStatsManagerRPC", RpcTarget.AllBuffered, persistentNameText, steamId);
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InputManager), "KeyDown")]
	public class BlockChatKey
	{
		private static bool Prefix(InputKey key, ref bool __result)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Invalid comparison between Unknown and I4
			if ((int)key == 7 && Hax2.showMenu)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(EnemyVision), "VisionTrigger")]
	public static class EnemyVision_BlindEnemies_Patch
	{
		[HarmonyPatch(typeof(EnemyThinMan), "SetTarget", new Type[] { typeof(PlayerAvatar) })]
		public static class EnemyThinMan_SetTarget_Patch
		{
			private static bool Prefix(EnemyThinMan __instance, PlayerAvatar _player)
			{
				PhotonView val = ((__instance != null) ? ((Component)__instance).GetComponent<PhotonView>() : null);
				if (!PhotonNetwork.IsMasterClient && ((Object)(object)val == (Object)null || !val.IsMine))
				{
					return true;
				}
				if ((Object)(object)_player != (Object)null && (Object)(object)_player.photonView != (Object)null && _player.photonView.Owner != null && ((Dictionary<object, object>)(object)_player.photonView.Owner.CustomProperties).TryGetValue((object)"isBlindEnabled", out object value) && value is bool && (bool)value)
				{
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(EnemyStateInvestigate), "SetRPC")]
		public static class EnemyStateInvestigate_BlindAudio_Patch
		{
			private const float LOCAL_PLAYER_SOUND_THRESHOLD = 1.5f;

			private static bool Prefix(Vector3 position)
			{
				//IL_008f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0094: Unknown result type (might be due to invalid IL or missing references)
				//IL_0095: Unknown result type (might be due to invalid IL or missing references)
				//IL_0096: Unknown result type (might be due to invalid IL or missing references)
				if (!Hax2.blindEnemies)
				{
					return true;
				}
				PlayerAvatar val = null;
				GameObject val2 = null;
				try
				{
					val2 = DebugCheats.GetLocalPlayer();
					if ((Object)(object)val2 != (Object)null)
					{
						val = val2.GetComponent<PlayerAvatar>();
					}
				}
				catch (Exception)
				{
				}
				if ((Object)(object)val == (Object)null && (Object)(object)GameDirector.instance != (Object)null && GameDirector.instance.PlayerList != null)
				{
					try
					{
						val = GameDirector.instance.PlayerList.FirstOrDefault((PlayerAvatar p) => (Object)(object)p != (Object)null && (Object)(object)p.photonView != (Object)null && p.photonView.IsMine);
					}
					catch (Exception)
					{
					}
				}
				if ((Object)(object)val == (Object)null)
				{
					return true;
				}
				Vector3 position2 = ((Component)val).transform.position;
				if (Vector3.Distance(position, position2) < 1.5f)
				{
					return false;
				}
				return true;
			}
		}

		private static bool Prefix(PlayerAvatar player)
		{
			if (!PhotonNetwork.IsMasterClient)
			{
				return true;
			}
			if ((Object)(object)player != (Object)null && (Object)(object)player.photonView != (Object)null && player.photonView.Owner != null && ((Dictionary<object, object>)(object)player.photonView.Owner.CustomProperties).TryGetValue((object)"isBlindEnabled", out object value) && value is bool && (bool)value)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ItemGun), "ShootRPC")]
	public class NoWeaponRecoil
	{
		public static bool _isEnabledForConfig = false;

		private static FieldInfo _physGrabObjectField = AccessTools.Field(typeof(ItemGun), "physGrabObject");

		private static FieldInfo _grabStrengthMultiplierField = AccessTools.Field(typeof(ItemGun), "grabStrengthMultiplier");

		private static FieldInfo _torqueMultiplierField = AccessTools.Field(typeof(ItemGun), "torqueMultiplier");

		[HarmonyPrefix]
		public static bool Prefix(ItemGun __instance, ref float[] __state)
		{
			__state = null;
			if (!_isEnabledForConfig || !BulletTrack.IsLocalShot(__instance))
			{
				return true;
			}
			try
			{
				float origGrab = -1f;
				float origTorque = -1f;
				if (_grabStrengthMultiplierField != null)
				{
					origGrab = (float)_grabStrengthMultiplierField.GetValue(__instance);
				}
				if (_torqueMultiplierField != null)
				{
					origTorque = (float)_torqueMultiplierField.GetValue(__instance);
				}
				// 保存原始值到 __state，供 Postfix 恢复
				__state = new float[] { origGrab, origTorque };

				float num = 0.01f;
				if (_grabStrengthMultiplierField != null)
				{
					_grabStrengthMultiplierField.SetValue(__instance, num);
				}
				if (_torqueMultiplierField != null)
				{
					_torqueMultiplierField.SetValue(__instance, num);
				}
				__instance.gunRecoilForce = 0f;
				__instance.cameraShakeMultiplier = 0f;
				if (_physGrabObjectField != null)
				{
					object value2 = _physGrabObjectField.GetValue(__instance);
					PhysGrabObject val2 = (PhysGrabObject)((value2 is PhysGrabObject) ? value2 : null);
					if ((Object)(object)val2 != (Object)null)
					{
						float num2 = 0.05f;
						float num3 = 0.1f;
						val2.OverrideGrabStrength(num3, num2);
						val2.OverrideTorqueStrength(num3, num2);
					}
				}
			}
			catch (Exception)
			{
			}
			return true;
		}

		[HarmonyPostfix]
		public static void Postfix(ItemGun __instance, float[] __state)
		{
			if (__state == null)
			{
				return;
			}
			if (!BulletTrack.IsLocalShot(__instance))
			{
				return;
			}
			try
			{
				if (__state[0] >= 0f && _grabStrengthMultiplierField != null)
				{
					_grabStrengthMultiplierField.SetValue(__instance, __state[0]);
				}
				if (__state[1] >= 0f && _torqueMultiplierField != null)
				{
					_torqueMultiplierField.SetValue(__instance, __state[1]);
				}
			}
			catch (Exception)
			{
			}
		}
	}

	private static class ReflectionCache
	{
		public static FieldInfo maskField;

		public static FieldInfo playerCameraField;

		public static FieldInfo grabbedObjectField;

		public static FieldInfo grabbedObjectTransformField;

		public static FieldInfo localGrabPositionField;

		public static FieldInfo physGrabPointField;

		public static FieldInfo physGrabPointPullerField;

		public static FieldInfo physGrabPointPlaneField;

		public static FieldInfo grabDisableTimerField;

		public static FieldInfo cameraRelativeGrabbedForwardField;

		public static FieldInfo cameraRelativeGrabbedUpField;

		public static FieldInfo cameraRelativeGrabbedRightField;

		public static FieldInfo initialPressTimerField;

		public static FieldInfo physRotatingTimerField;

		public static FieldInfo prevGrabbedField;

		public static FieldInfo grabbedField;

		public static MethodInfo physGrabPointActivateMethod;

		public static bool initialized;

		public static void Initialize()
		{
			if (!initialized)
			{
				Type typeFromHandle = typeof(PhysGrabber);
				maskField = AccessTools.Field(typeFromHandle, "mask");
				playerCameraField = AccessTools.Field(typeFromHandle, "playerCamera");
				grabbedObjectField = AccessTools.Field(typeFromHandle, "grabbedObject");
				grabbedObjectTransformField = AccessTools.Field(typeFromHandle, "grabbedObjectTransform");
				localGrabPositionField = AccessTools.Field(typeFromHandle, "localGrabPosition");
				physGrabPointField = AccessTools.Field(typeFromHandle, "physGrabPoint");
				physGrabPointPullerField = AccessTools.Field(typeFromHandle, "physGrabPointPuller");
				physGrabPointPlaneField = AccessTools.Field(typeFromHandle, "physGrabPointPlane");
				grabDisableTimerField = AccessTools.Field(typeFromHandle, "grabDisableTimer");
				cameraRelativeGrabbedForwardField = AccessTools.Field(typeFromHandle, "cameraRelativeGrabbedForward");
				cameraRelativeGrabbedUpField = AccessTools.Field(typeFromHandle, "cameraRelativeGrabbedUp");
				cameraRelativeGrabbedRightField = AccessTools.Field(typeFromHandle, "cameraRelativeGrabbedRight");
				initialPressTimerField = AccessTools.Field(typeFromHandle, "initialPressTimer");
				physRotatingTimerField = AccessTools.Field(typeFromHandle, "physRotatingTimer");
				prevGrabbedField = AccessTools.Field(typeFromHandle, "prevGrabbed");
				grabbedField = AccessTools.Field(typeFromHandle, "grabbed");
				physGrabPointActivateMethod = AccessTools.Method(typeFromHandle, "PhysGrabPointActivate", (Type[])null, (Type[])null);
				initialized = true;
				Debug.Log((object)"[GrabThroughWallsPatch] Reflection cache initialized successfully");
			}
		}
	}

	[HarmonyPatch(typeof(PhysGrabber), "RayCheck")]
	public class GrabThroughWallsPatch
	{
		public static bool enableGrabThroughWalls;

		private static FieldInfo maskField;

		private static LayerMask originalMask;

		private static bool hasStoredOriginalMask;

		static GrabThroughWallsPatch()
		{
			maskField = AccessTools.Field(typeof(PhysGrabber), "mask");
		}

		[HarmonyPrefix]
		public static void Prefix(PhysGrabber __instance, bool _grab)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			if (!enableGrabThroughWalls || !__instance.isLocal)
			{
				return;
			}
			try
			{
				LayerMask val = (LayerMask)maskField.GetValue(__instance);
				if (!hasStoredOriginalMask)
				{
					originalMask = val;
					hasStoredOriginalMask = true;
				}
				if (_grab)
				{
					int val2 = LayerMask.GetMask(new string[1] { "Default" });
					int val3 = (int)val & ~val2;
					int val4 = LayerMask.GetMask(new string[4] { "PhysGrabObject", "PhysGrabObjectCart", "PhysGrabObjectHinge", "StaticGrabObject" });
					val3 = val3 | val4;
					maskField.SetValue(__instance, (LayerMask)val3);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError((object)("[GrabThroughWallsPatch] Error in Prefix: " + ex.Message));
			}
		}

		[HarmonyPostfix]
		public static void Postfix(PhysGrabber __instance)
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			if (!enableGrabThroughWalls || !__instance.isLocal)
			{
				return;
			}
			try
			{
				if (hasStoredOriginalMask)
				{
					maskField.SetValue(__instance, originalMask);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError((object)("[GrabThroughWallsPatch] Error in Postfix: " + ex.Message));
			}
		}
	}

	public static void ToggleGrabThroughWalls(bool enabled)
	{
		GrabThroughWallsPatch.enableGrabThroughWalls = enabled;
	}

	public static bool IsGrabThroughWallsEnabled()
	{
		return GrabThroughWallsPatch.enableGrabThroughWalls;
	}

	[HarmonyPatch(typeof(PlayerHealth), "Hurt")]
	public static class PlayerHealth_Hurt_AuraPatch
	{
		private static bool Prefix(PlayerHealth __instance)
		{
			return !PlayerAura.BlocksDamage(__instance);
		}
	}

	[HarmonyPatch(typeof(PlayerHealth), "HurtOther")]
	public static class PlayerHealth_HurtOther_AuraPatch
	{
		private static bool Prefix(PlayerHealth __instance)
		{
			return !PlayerAura.BlocksDamage(__instance);
		}
	}

	[HarmonyPatch(typeof(HurtCollider), "PlayerHurt")]
	public static class HurtCollider_PlayerHurt_AuraPatch
	{
		private static bool Prefix(PlayerAvatar _player)
		{
			return !PlayerAura.Covers(_player);
		}
	}
}
