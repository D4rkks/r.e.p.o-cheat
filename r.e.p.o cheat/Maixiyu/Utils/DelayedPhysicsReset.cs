using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

public class DelayedPhysicsReset : MonoBehaviour
{
	private Rigidbody rb;

	private PhotonTransformView transformView;

	private float delay = 1f;

	public void Setup(Rigidbody rigidbody, PhotonTransformView tView = null)
	{
		rb = rigidbody;
		transformView = tView;
		((MonoBehaviour)this).Invoke("ResetPhysics", delay);
	}

	private void ResetPhysics()
	{
		if ((Object)(object)rb != (Object)null)
		{
			rb.isKinematic = false;
		}
		if ((Object)(object)transformView != (Object)null)
		{
			((Behaviour)transformView).enabled = true;
		}
		Object.Destroy((Object)(object)this);
	}
}
