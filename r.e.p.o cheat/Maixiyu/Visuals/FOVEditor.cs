using UnityEngine;

namespace r.e.p.o_cheat;

public class FOVEditor : MonoBehaviour
{
	public float fovValue = 70f;

	public bool fovEnabled = true;

	public static FOVEditor Instance { get; private set; }

	private void Awake()
	{
		if ((Object)(object)Instance == (Object)null)
		{
			Instance = this;
			Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
		}
		else
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	private void Update()
	{
		if (fovEnabled)
		{
			CameraZoom instance = CameraZoom.Instance;
			if ((Object)(object)instance != (Object)null)
			{
				instance.Reflect<CameraZoom>().SetValue("zoomPrev", fovValue);
				instance.Reflect<CameraZoom>().SetValue("zoomNew", fovValue);
				instance.Reflect<CameraZoom>().SetValue("zoomCurrent", fovValue);
				instance.playerZoomDefault = fovValue;
			}
		}
	}

	public void SetFOV(float value)
	{
		fovValue = value;
	}

	public float GetFOV()
	{
		return fovValue;
	}

	public void EnableFOV(bool state)
	{
		fovEnabled = state;
	}
}
