using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

internal static class Players
{
	public static object playerHealthInstance;

	public static object playerMaxHealthInstance;

	public static void HealPlayer(object targetPlayer, int healAmount, string playerName)
	{
		try
		{
			// 游戏自己的治疗+同步路径（HealOther → HealOtherRPC）；旧的 3 参数 UpdateHealthRPC 是伪造签名，已被游戏丢弃
			if (targetPlayer is PlayerAvatar avatar && avatar.playerHealth != null)
			{
				avatar.playerHealth.HealOther(healAmount, effect: false);
			}
		}
		catch (Exception)
		{
		}
	}

	public static void DamagePlayer(object targetPlayer, int damageAmount, string playerName)
	{
		if (targetPlayer == null)
		{
			return;
		}
		try
		{
			if (targetPlayer is PlayerAvatar avatar && avatar.playerHealth != null)
			{
				avatar.playerHealth.HurtOther(damageAmount, ((Component)avatar).transform.position, true);
				return;
			}
		}
		catch
		{
		}
	}

	internal static void ReviveSelectedPlayer(int selectedPlayerIndex, List<object> playerList, List<string> playerNames)
	{
		if (selectedPlayerIndex >= 0 && selectedPlayerIndex < playerList.Count && selectedPlayerIndex < playerNames.Count)
		{
			object obj = playerList[selectedPlayerIndex];
			string playerName = playerNames[selectedPlayerIndex];
			if (obj != null)
			{
				ReviveSelectedPlayer(obj, playerList, playerName);
			}
		}
	}

	public static void ReviveSelectedPlayer(object selectedPlayer, List<object> playerList, string playerName)
	{
		try
		{
			// 游戏自己的复活+同步路径（Revive → ReviveRPC）
			if (selectedPlayer is PlayerAvatar avatar)
			{
				ReviveAvatar(avatar);
			}
		}
		catch (Exception)
		{
		}
	}

	internal static void KillSelectedPlayer(int selectedPlayerIndex, List<object> playerList, List<string> playerNames)
	{
		if (selectedPlayerIndex >= 0 && selectedPlayerIndex < playerList.Count && selectedPlayerIndex < playerNames.Count)
		{
			object obj = playerList[selectedPlayerIndex];
			string playerName = playerNames[selectedPlayerIndex];
			if (obj != null)
			{
				KillSelectedPlayer(obj, playerList, playerName);
			}
		}
	}

	public static void KillSelectedPlayer(object selectedPlayer, List<object> playerList, string playerName)
	{
		try
		{
			if (!(selectedPlayer is PlayerAvatar avatar))
			{
				return;
			}
			// PlayerHealth.Death() is owner-only (photonView.IsMine). Calling it on the
			// host for a guest avatar is a no-op, so same-cheat guests with godMode
			// never die. PlayerDeath → PlayerDeathRPC is master-authoritative and
			// does not check godMode.
			avatar.PlayerDeath(-1);
		}
		catch (Exception)
		{
		}
	}

	public static void ForcePlayerTumble(float duration = 10f)
	{
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		if (Hax2.selectedPlayerIndex < 0 || Hax2.selectedPlayerIndex >= Hax2.playerList.Count)
		{
			Debug.Log((object)"玩家索引无效！");
			return;
		}
		object obj = Hax2.playerList[Hax2.selectedPlayerIndex];
		if (obj == null)
		{
			Debug.Log((object)"所选玩家为空！");
			return;
		}
		try
		{
			Debug.Log((object)$"正在让 {Hax2.playerNames[Hax2.selectedPlayerIndex]} 翻滚 {duration} 秒。");
			FieldInfo field = obj.GetType().GetField("tumble", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
			{
				Debug.Log((object)"未找到 PlayerTumble 字段！");
				return;
			}
			object value = field.GetValue(obj);
			PlayerTumble val = (PlayerTumble)((value is PlayerTumble) ? value : null);
			if ((Object)(object)val == (Object)null)
			{
				Debug.Log((object)"PlayerTumble 实例为 null！");
				return;
			}
			FieldInfo field2 = ((object)val).GetType().GetField("photonView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field2 == null)
			{
				Debug.Log((object)"在 PlayerTumble 上未找到 PhotonView 字段！");
				return;
			}
			object value2 = field2.GetValue(val);
			PhotonView val2 = (PhotonView)((value2 is PhotonView) ? value2 : null);
			if ((Object)(object)val2 == (Object)null)
			{
				Debug.Log((object)"PhotonView 无效！");
				return;
			}
			val2.RPC("TumbleSetRPC", (RpcTarget)0, new object[2] { true, false });
			val2.RPC("TumbleOverrideTimeRPC", (RpcTarget)0, new object[1] { duration });
			val2.RPC("TumbleForceRPC", (RpcTarget)0, new object[1] { (object)new Vector3(10f, 50f, 0f) });
			val2.RPC("TumbleTorqueRPC", (RpcTarget)0, new object[1] { (object)new Vector3(0f, 0f, 2000f) });
			Debug.Log((object)$"已让 {Hax2.playerNames[Hax2.selectedPlayerIndex]} 翻滚 {duration} 秒。");
		}
		catch (Exception ex)
		{
			Debug.Log((object)("让 " + Hax2.playerNames[Hax2.selectedPlayerIndex] + " 翻滚时出错：" + ex.Message));
		}
	}

	public static int GetPlayerHealth(object player)
	{
		try
		{
			FieldInfo field = player.GetType().GetField("playerHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
			{
				return 100;
			}
			object value = field.GetValue(player);
			if (value == null)
			{
				return 100;
			}
			FieldInfo field2 = value.GetType().GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field2 == null)
			{
				return 100;
			}
			return (int)field2.GetValue(value);
		}
		catch (Exception)
		{
			return 100;
		}
	}

	public static int GetPlayerMaxHealth(object playerHealthInstance)
	{
		if (playerHealthInstance == null)
		{
			return 100;
		}
		FieldInfo field = playerHealthInstance.GetType().GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (!(field != null))
		{
			return 100;
		}
		return (int)field.GetValue(playerHealthInstance);
	}

	/// <summary>
	/// 复活自己 - 无需主机权限
	/// 通过RPC同步确保所有客户端看到复活效果
	/// </summary>
	public static void ReviveSelf()
	{
		try
		{
			// 游戏自己的复活+同步路径（Revive → ReviveRPC All），替代旧的死亡头反射 + 伪造 RPC
			PlayerAvatar local = null;
			List<PlayerAvatar> players = SemiFunc.PlayerGetList();
			if (players != null)
			{
				foreach (PlayerAvatar avatar in players)
				{
					if (avatar != null && avatar.photonView != null && avatar.photonView.IsMine)
					{
						local = avatar;
						break;
					}
				}
			}
			if (local == null)
			{
				Debug.Log((object)"[ReviveSelf] 无法找到本地玩家");
				return;
			}
			ReviveAvatar(local);
			Debug.Log((object)"[ReviveSelf] 已复活自己");
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[ReviveSelf] 出错: " + ex.Message));
		}
	}

	/// <summary>
	/// 治疗复活队友 - 通过RPC同步，不需要主机权限
	/// 先恢复血量（通过HealPlayer），然后尝试复活死亡头/玩家
	/// </summary>
	public static void HealRevivePlayer(object targetPlayer, string playerName)
	{
		if (targetPlayer == null) return;

		try
		{
			// 游戏自己的复活+治疗+同步路径
			if (targetPlayer is PlayerAvatar avatar)
			{
				ReviveAvatar(avatar);
				Debug.Log((object)("[HealRevive] 已治疗复活: " + playerName));
			}
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[HealRevive] 出错: " + ex.Message));
		}
	}

	private static void ReviveAvatar(PlayerAvatar avatar)
	{
		if (avatar == null)
		{
			return;
		}
		try
		{
			object head = typeof(PlayerAvatar).GetField("playerDeathHead", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(avatar);
			if (head != null)
			{
				head.GetType().GetField("inExtractionPoint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(head, true);
				head.GetType().GetMethod("Revive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(head, null);
			}
			if (NativeGameApi.IsHost())
			{
				avatar.Revive(false);
			}
			else
			{
				NativeGameApi.ReviveLocal(avatar);
			}
			if (avatar.playerHealth != null)
			{
				avatar.playerHealth.HealOther(GetPlayerMaxHealth(avatar.playerHealth), effect: false);
			}
		}
		catch
		{
		}
	}
}
