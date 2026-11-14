using System;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace r.e.p.o_cheat;

internal static class Troll
{
	private const byte RecoverLoadingEvent = 173;

	private static bool _photonEventHooked;

	private static readonly BindingFlags InstAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	public static void InfiniteLoadingSelectedPlayer()
	{
		try
		{
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
			Debug.Log((object)("正在尝试让 " + Hax2.playerNames[Hax2.selectedPlayerIndex] + " 进入无限加载界面……"));
			FieldInfo field = obj.GetType().GetField("photonView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
			{
				Debug.Log((object)"未找到 PhotonView 字段！");
				return;
			}
			object value = field.GetValue(obj);
			PhotonView val = (PhotonView)((value is PhotonView) ? value : null);
			if ((Object)(object)val == (Object)null)
			{
				Debug.Log((object)"PhotonView 为 null！");
				return;
			}
			if (val.Owner == null)
			{
				Debug.Log((object)"无法从 PhotonView 获取 Photon 玩家信息！");
				return;
			}
			int actorNumber = PhotonNetwork.MasterClient.ActorNumber;
			FieldInfo field2 = typeof(Player).GetField("actorNumber", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field2 != null)
			{
				// 保存原始值在修改前，避免恢复时读到篡改后的值
				int savedActor = (int)field2.GetValue(PhotonNetwork.LocalPlayer);
				try
				{
					field2.SetValue(PhotonNetwork.LocalPlayer, actorNumber);
					val.RPC("OutroStartRPC", (RpcTarget)0, Array.Empty<object>());
				}
				finally
				{
					field2.SetValue(PhotonNetwork.LocalPlayer, savedActor);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.Log((object)("让 " + Hax2.playerNames[Hax2.selectedPlayerIndex] + " 进入无限加载时出错：" + ex.Message));
		}
	}

	public static void SceneRecovery()
	{
		RecoverLocalFromLoading();
		TryNotifySelectedPlayerToRecover();
	}

	internal static void UnhookPhotonEvents()
	{
		if (!_photonEventHooked)
		{
			return;
		}
		try
		{
			if (PhotonNetwork.NetworkingClient != null)
			{
				PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
			}
		}
		catch
		{
		}
		_photonEventHooked = false;
	}

	internal static void EnsurePhotonEventHook()
	{
		if (_photonEventHooked)
		{
			return;
		}
		try
		{
			if (PhotonNetwork.NetworkingClient == null)
			{
				return;
			}
			PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
			_photonEventHooked = true;
		}
		catch
		{
		}
	}

	internal static void RecoverLocalFromLoading()
	{
		try
		{
			GameDirector gd = GameDirector.instance;
			if (gd != null)
			{
				FieldInfo outroField = typeof(GameDirector).GetField("outroStart", InstAll);
				bool outroFlag = outroField != null && outroField.GetValue(gd) is bool flag && flag;
				if (outroFlag || gd.currentState == GameDirector.gameState.Outro || gd.currentState == GameDirector.gameState.Death)
				{
					gd.Revive();
				}
			}
			LoadingUI ui = LoadingUI.instance;
			if (ui != null)
			{
				ui.StopLoading();
				typeof(LoadingUI).GetField("levelAnimationCompleted", InstAll)?.SetValue(ui, true);
			}
			if (HUD.instance != null)
			{
				HUD.instance.Show();
			}
			PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
			if (local != null)
			{
				typeof(PlayerAvatar).GetField("levelAnimationCompleted", InstAll)?.SetValue(local, true);
			}
			Debug.Log((object)"[恢复] 已本地解除加载界面，未重载场景、未断开连接。");
		}
		catch (Exception ex)
		{
			Debug.Log((object)("[恢复] 本地解除失败: " + ex.Message));
		}
	}

	private static void TryNotifySelectedPlayerToRecover()
	{
		EnsurePhotonEventHook();
		if (!PhotonNetwork.InRoom)
		{
			return;
		}
		PlayerAvatar avatar = GetSelectedAvatar();
		if (avatar == null || avatar.photonView == null || avatar.photonView.Owner == null)
		{
			return;
		}
		int actor = avatar.photonView.Owner.ActorNumber;
		Player localPlayer = PhotonNetwork.LocalPlayer;
		if (localPlayer != null && actor == localPlayer.ActorNumber)
		{
			return;
		}
		try
		{
			RaiseEventOptions options = new RaiseEventOptions
			{
				TargetActors = new[] { actor }
			};
			PhotonNetwork.RaiseEvent(RecoverLoadingEvent, true, options, SendOptions.SendReliable);
		}
		catch
		{
		}
	}

	private static void OnPhotonEvent(EventData photonEvent)
	{
		if (photonEvent == null || photonEvent.Code != RecoverLoadingEvent || !PhotonNetwork.InRoom)
		{
			return;
		}
		if (photonEvent.Sender <= 0)
		{
			return;
		}
		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			if (room == null || room.GetPlayer(photonEvent.Sender) == null)
			{
				return;
			}
		}
		catch
		{
			return;
		}
		RecoverLocalFromLoading();
	}

	private static PlayerAvatar GetSelectedAvatar()
	{
		if (Hax2.selectedPlayerIndex < 0 || Hax2.selectedPlayerIndex >= Hax2.playerList.Count)
		{
			return null;
		}
		return Hax2.playerList[Hax2.selectedPlayerIndex] as PlayerAvatar;
	}

	public static void ForcePlayerGlitch()
	{
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
			Debug.Log((object)("正在强制 " + Hax2.playerNames[Hax2.selectedPlayerIndex] + " 触发 Glitch。"));
			FieldInfo field = obj.GetType().GetField("photonView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
			{
				Debug.Log((object)"未找到 PhotonView 字段！");
				return;
			}
			object value = field.GetValue(obj);
			PhotonView val = (PhotonView)((value is PhotonView) ? value : null);
			if ((Object)(object)val == (Object)null)
			{
				Debug.Log((object)"PhotonView 无效！");
				return;
			}
			val.RPC("PlayerGlitchShortRPC", (RpcTarget)0, Array.Empty<object>());
			Debug.Log((object)("已强制 " + Hax2.playerNames[Hax2.selectedPlayerIndex] + " 触发 Glitch。"));
		}
		catch (Exception ex)
		{
			Debug.Log((object)("强制 " + Hax2.playerNames[Hax2.selectedPlayerIndex] + " 触发 Glitch 时出错：" + ex.Message));
		}
	}
}
