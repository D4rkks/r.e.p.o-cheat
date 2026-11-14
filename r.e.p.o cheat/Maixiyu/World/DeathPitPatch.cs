using HarmonyLib;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// Death pits kill through HurtCollider.PlayerHurt (playerKill / deathPit),
/// not DisableDeathPitEffect (that timer only suppresses the save VFX).
/// Skip the local player's pit hurt so they can fall without dying.
/// </summary>
[HarmonyPatch(typeof(HurtCollider), "PlayerHurt")]
public static class DeathPitPatch
{
	[HarmonyPrefix]
	public static bool Prefix(HurtCollider __instance, PlayerAvatar _player)
	{
		if (!NativeGameApi.NoDeathPit || (Object)__instance == null || (Object)_player == null)
		{
			return true;
		}
		try
		{
			if (!__instance.deathPit)
			{
				return true;
			}
			if (_player.photonView != null && _player.photonView.IsMine)
			{
				return false;
			}
			PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
			if ((Object)local != null && local == _player)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}
}
