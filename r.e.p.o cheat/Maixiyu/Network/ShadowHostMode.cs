using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// Authority diagnostics and identity watchdog.
/// The old Harmony override of PhotonNetwork.IsMasterClient made every game
/// system (battery, guns, drones, haul) simulate as host while MasterOnlyRPC
/// still rejected the packets — that desync is the Room-tab vs Self-tab bug.
/// The getter is no longer rewritten.
/// </summary>
public static class ShadowHostMode
{
	public static bool isEnabled;
	internal static bool _bypassPatch;
	public static string statusMessage = "";

	[HarmonyPatch(typeof(PhotonNetwork), "get_IsMasterClient")]
	public static class Patch_IsMasterClient
	{
		static void Postfix(ref bool __result)
		{
			_ = __result;
			// Intentionally do not override. Faking master here breaks other features.
		}
	}

	public static bool IsTrueMasterClient()
	{
		_bypassPatch = true;
		try
		{
			return PhotonNetwork.IsMasterClient;
		}
		finally
		{
			_bypassPatch = false;
		}
	}

	public static void Toggle()
	{
		isEnabled = !isEnabled;
		ApplyToggleSideEffects();
	}

	public static void ApplyToggleSideEffects()
	{
		if (isEnabled)
		{
			ForceHost.RestoreLocalIdentity();
			statusMessage = L.T("room.shadow_protect_on");
		}
		else
		{
			statusMessage = L.T("room.shadow_protect_off");
		}
	}

	public static string GetConnectionLabel()
	{
		try
		{
			if (PhotonNetwork.InRoom)
			{
				return L.T("room.state_in_room");
			}
			ClientState state = PhotonNetwork.NetworkClientState;
			return state switch
			{
				ClientState.JoinedLobby => L.T("room.state_lobby"),
				ClientState.ConnectingToNameServer => L.T("room.state_connecting"),
				ClientState.ConnectingToMasterServer => L.T("room.state_connecting"),
				ClientState.ConnectingToGameServer => L.T("room.state_connecting"),
				ClientState.Authenticating => L.T("room.state_connecting"),
				ClientState.Authenticated => L.T("room.state_connecting"),
				ClientState.PeerCreated => L.T("room.state_offline"),
				ClientState.Disconnected => L.T("room.state_offline"),
				_ => state.ToString()
			};
		}
		catch
		{
			return L.T("room.not_in_room");
		}
	}

	public static string GetDiagnostics()
	{
		try
		{
			if (!PhotonNetwork.InRoom)
			{
				return L.T("room.conn_fmt", GetConnectionLabel());
			}
			bool trueMaster = IsTrueMasterClient();
			Player master = PhotonNetwork.MasterClient;
			string masterName = master != null ? master.NickName : "?";
			string fake = ForceHost.LocalFakeActive ? L.T("room.fake_on") : L.T("room.fake_off");
			return L.T("room.auth_diag_fmt", trueMaster ? L.T("room.yes") : L.T("room.no"), masterName, fake);
		}
		catch
		{
			return L.T("room.not_in_room");
		}
	}
}
