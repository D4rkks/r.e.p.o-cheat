using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

public class MaiItemSync : MonoBehaviour, IPunInstantiateMagicCallback, IPunObservable
{
	private Rigidbody rb;

	private void Awake()
	{
		rb = ((Component)this).GetComponent<Rigidbody>();
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		object[] instantiationData = info.photonView.InstantiationData;
		if (instantiationData != null && instantiationData.Length == 3)
		{
			Vector3 position = new Vector3((float)instantiationData[0], (float)instantiationData[1], (float)instantiationData[2]);
			((Component)this).transform.position = position;
		}
		if (!info.photonView.IsMine && (Object)(object)rb != (Object)null)
		{
			rb.isKinematic = true;
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if (stream.IsWriting)
		{
			stream.SendNext((object)((Component)this).transform.position);
			stream.SendNext((object)((Component)this).transform.rotation);
			if ((Object)(object)rb != (Object)null)
			{
				stream.SendNext((object)rb.velocity);
				stream.SendNext((object)rb.angularVelocity);
			}
		}
		else
		{
			((Component)this).transform.position = (Vector3)stream.ReceiveNext();
			((Component)this).transform.rotation = (Quaternion)stream.ReceiveNext();
			if ((Object)(object)rb != (Object)null)
			{
				rb.velocity = (Vector3)stream.ReceiveNext();
				rb.angularVelocity = (Vector3)stream.ReceiveNext();
			}
		}
	}

	private void Update()
	{
		_ = PhotonNetwork.IsMasterClient;
	}
}
