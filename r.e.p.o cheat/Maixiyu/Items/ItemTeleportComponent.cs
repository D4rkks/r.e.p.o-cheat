using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace r.e.p.o_cheat;

public class ItemTeleportComponent : MonoBehaviour, IPunOwnershipCallbacks
{
	private PhotonView photonView;
	private Vector3? pendingPosition;
	private Quaternion? pendingRotation;
	private float ownershipRequestTime;
	private const float OWNERSHIP_TIMEOUT = 2f;

	private void Awake()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		if ((Object)(object)photonView == (Object)null)
		{
			photonView = ((Component)this).gameObject.AddComponent<PhotonView>();
		}
		PhotonNetwork.AddCallbackTarget((object)this);
	}

	private void OnDestroy()
	{
		PhotonNetwork.RemoveCallbackTarget((object)this);
	}

	private void Update()
	{
		if (!pendingPosition.HasValue)
		{
			return;
		}
		if (photonView.IsMine)
		{
			ExecuteTeleport(pendingPosition.Value, pendingRotation.Value);
			pendingPosition = null;
			pendingRotation = null;
		}
		else if (Time.time - ownershipRequestTime > OWNERSHIP_TIMEOUT)
		{
			Debug.Log((object)("所有权请求超时，执行本地传送: " + ((Object)((Component)this).gameObject).name));
			ExecuteLocalTeleport(pendingPosition.Value, pendingRotation.Value);
			pendingPosition = null;
			pendingRotation = null;
		}
	}

	public void RequestTeleport(Vector3 targetPos, Quaternion targetRot)
	{
		if (photonView.IsMine)
		{
			ExecuteTeleport(targetPos, targetRot);
		}
		else
		{
			pendingPosition = targetPos;
			pendingRotation = targetRot;
			ownershipRequestTime = Time.time;
			photonView.RequestOwnership();
			Debug.Log((object)$"已请求所有权并排队传送: {((Object)((Component)this).gameObject).name} (ViewID: {photonView.ViewID})");
		}
	}

	private void ExecuteTeleport(Vector3 pos, Quaternion rot)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		PhotonTransformView component = ((Component)this).GetComponent<PhotonTransformView>();
		bool hadPTV = false;
		if ((Object)(object)component != (Object)null && ((Behaviour)component).enabled)
		{
			hadPTV = true;
			((Behaviour)component).enabled = false;
		}
		Rigidbody rb = ((Component)this).GetComponent<Rigidbody>();
		bool hadRB = false;
		if ((Object)(object)rb != (Object)null)
		{
			hadRB = !rb.isKinematic;
			rb.isKinematic = true;
		}
		((Component)this).transform.position = pos;
		((Component)this).transform.rotation = rot;
		if (PhotonNetwork.IsConnected && (Object)(object)photonView != (Object)null && photonView.IsMine)
		{
			photonView.RPC("TeleportItemRPC", (RpcTarget)1, new object[1] { pos });
			Debug.Log((object)("已发送 TeleportItemRPC: " + ((Object)((Component)this).gameObject).name));
		}
		if (hadPTV || hadRB)
		{
			((MonoBehaviour)this).StartCoroutine(ReEnablePhysics(rb, component, hadRB, hadPTV));
		}
		// 强制重新激活渲染
		GameObject go = ((Component)this).gameObject;
		go.SetActive(false);
		go.SetActive(true);
		Debug.Log((object)("传送完成: " + ((Object)((Component)this).gameObject).name));
	}

	private void ExecuteLocalTeleport(Vector3 pos, Quaternion rot)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		Rigidbody rb = ((Component)this).GetComponent<Rigidbody>();
		if ((Object)(object)rb != (Object)null)
		{
			rb.isKinematic = true;
		}
		((Component)this).transform.position = pos;
		((Component)this).transform.rotation = rot;
		GameObject go = ((Component)this).gameObject;
		go.SetActive(false);
		go.SetActive(true);
		Debug.Log((object)("本地传送完成（无所有权）: " + ((Object)((Component)this).gameObject).name));
	}

	private IEnumerator ReEnablePhysics(Rigidbody rb, PhotonTransformView ptv, bool restoreRB, bool restorePTV)
	{
		yield return (object)new WaitForSeconds(0.5f);
		if (restoreRB && (Object)(object)rb != (Object)null)
		{
			rb.isKinematic = false;
		}
		if (restorePTV && (Object)(object)ptv != (Object)null)
		{
			((Behaviour)ptv).enabled = true;
		}
	}

	[PunRPC]
	private void TeleportItemRPC(Vector3 targetPosition)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).transform.position = targetPosition;
	}

	public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
	{
	}

	public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
	{
		if ((Object)(object)targetView == (Object)(object)photonView)
		{
			Debug.Log((object)("所有权已转移: " + ((Object)((Component)this).gameObject).name));
		}
	}

	public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
	{
		if ((Object)(object)targetView == (Object)(object)photonView)
		{
			Debug.Log((object)("所有权转移失败: " + ((Object)((Component)this).gameObject).name));
		}
	}
}
