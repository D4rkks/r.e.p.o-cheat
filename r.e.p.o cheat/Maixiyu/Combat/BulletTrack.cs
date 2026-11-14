using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// Silent bullet track: shoot while the target is in front of the camera.
/// The world object stays put; ShootBullet's hit point is redirected so the
/// tracer and HurtCollider land on that target (FPS magic-bullet style).
/// Host/solo only — the game raycasts guns on the master client.
/// </summary>
[HarmonyPatch(typeof(ItemGun), "ShootBullet")]
public static class BulletTrack
{
	public static bool Enabled;
	public static bool TrackEnemies = true;
	public static bool TrackPlayers;
	public static bool TrackItems;
	public static float MaxDistance = 80f;
	public static float Fov = 12f;

	private static readonly FieldInfo PhysGrabObjectField = AccessTools.Field(typeof(ItemGun), "physGrabObject");

	[HarmonyPrefix]
	public static void Prefix(ItemGun __instance, ref Vector3 _endPosition, ref bool _hit)
	{
		if (!Enabled || (Object)__instance == null)
		{
			return;
		}
		try
		{
			if (!IsLocalShot(__instance))
			{
				return;
			}
			if (!TryGetTargetPoint(out Vector3 target))
			{
				return;
			}
			_endPosition = target;
			_hit = true;
		}
		catch
		{
		}
	}

	/// <summary>
	/// Room guns are master-owned, so photonView.IsMine is true for every gun
	/// on the host and false for every gun on a guest. Use the local grab instead.
	/// </summary>
	public static bool IsLocalShot(ItemGun gun)
	{
		if ((Object)gun == null)
		{
			return false;
		}
		PhysGrabObject body = PhysGrabObjectField?.GetValue(gun) as PhysGrabObject;
		if ((Object)body == null)
		{
			body = gun.GetComponent<PhysGrabObject>();
		}
		if ((Object)body == null)
		{
			return false;
		}
		if (body.grabbedLocal)
		{
			return true;
		}
		if (body.playerGrabbing == null)
		{
			return false;
		}
		foreach (PhysGrabber grabber in body.playerGrabbing)
		{
			if (grabber != null && grabber.isLocal)
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGetTargetPoint(out Vector3 point)
	{
		point = Vector3.zero;
		Camera cam = GameHelper.GetActiveCamera();
		if ((Object)cam == null)
		{
			cam = Camera.main;
		}
		if ((Object)cam == null)
		{
			return false;
		}
		Vector3 origin = cam.transform.position;
		Vector3 forward = cam.transform.forward;
		float maxDist = Mathf.Max(5f, MaxDistance);
		float maxAngle = Mathf.Clamp(Fov, 2f, 60f);
		float bestAngle = maxAngle;
		bool found = false;

		if (TrackEnemies)
		{
			List<Enemy> enemies = DebugCheats.enemyList;
			if (enemies != null)
			{
				for (int i = 0; i < enemies.Count; i++)
				{
					Enemy enemy = enemies[i];
					if ((Object)enemy == null || !((Component)enemy).gameObject.activeInHierarchy)
					{
						continue;
					}
					try
					{
						if (Enemies.GetEnemyHealth(enemy) <= 0)
						{
							continue;
						}
					}
					catch
					{
					}
					Vector3 pos = enemy.CenterTransform != null ? enemy.CenterTransform.position : ((Component)enemy).transform.position;
					if (IsBetter(origin, forward, pos, maxDist, ref bestAngle))
					{
						point = pos;
						found = true;
					}
				}
			}
		}

		if (TrackPlayers)
		{
			PlayerAvatar local = null;
			try { local = SemiFunc.PlayerAvatarLocal(); } catch { }
			List<PlayerAvatar> players = null;
			try { players = SemiFunc.PlayerGetList(); } catch { }
			if (players != null)
			{
				for (int i = 0; i < players.Count; i++)
				{
					PlayerAvatar player = players[i];
					if ((Object)player == null || player == local)
					{
						continue;
					}
					if (player.photonView != null && player.photonView.IsMine)
					{
						continue;
					}
					Vector3 pos = player.transform.position + Vector3.up * 0.9f;
					try
					{
						if (player.PlayerVisionTarget != null && player.PlayerVisionTarget.VisionTransform != null)
						{
							pos = player.PlayerVisionTarget.VisionTransform.position;
						}
					}
					catch
					{
					}
					if (IsBetter(origin, forward, pos, maxDist, ref bestAngle))
					{
						point = pos;
						found = true;
					}
				}
			}
		}

		if (TrackItems)
		{
			List<object> items = DebugCheats.valuableObjects;
			if (items != null)
			{
				for (int i = 0; i < items.Count; i++)
				{
					object raw = items[i];
					if (!(raw is Component component) || (Object)component == null)
					{
						continue;
					}
					if (!component.gameObject.activeInHierarchy)
					{
						continue;
					}
					if (component.GetComponent<PlayerDeathHead>() != null)
					{
						continue;
					}
					Vector3 pos = component.transform.position;
					if (IsBetter(origin, forward, pos, maxDist, ref bestAngle))
					{
						point = pos;
						found = true;
					}
				}
			}
		}

		return found;
	}

	private static bool IsBetter(Vector3 origin, Vector3 forward, Vector3 target, float maxDist, ref float bestAngle)
	{
		Vector3 delta = target - origin;
		float dist = delta.magnitude;
		if (dist < 0.35f || dist > maxDist)
		{
			return false;
		}
		float angle = Vector3.Angle(forward, delta);
		if (angle > bestAngle)
		{
			return false;
		}
		bestAngle = angle;
		return true;
	}
}
