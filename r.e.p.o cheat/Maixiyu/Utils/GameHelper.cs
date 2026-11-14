using System;
using UnityEngine;

public class GameHelper : MonoBehaviour
{
	public static object FindObjectOfType(Type type)
	{
		return Object.FindObjectOfType(type, true);
	}

	// ═══════════════════════════════════════════════════════════
	// 可靠相机获取 — Camera.main fallback chain
	// R.E.P.O 游戏内的玩家相机可能没有 "MainCamera" tag，
	// 导致 Camera.main 返回 null。此方法提供多级回退。
	// ═══════════════════════════════════════════════════════════

	private static Camera _cachedActiveCam;
	private static int _cachedFrame = -1;

	/// <summary>
	/// 获取当前最佳可用相机。带 1 帧缓存避免重复开销。
	/// 回退链: CameraZoom → Camera.main → allCameras（含 RenderTexture 世界相机）
	/// </summary>
	public static Camera GetActiveCamera()
	{
		int frame = Time.frameCount;
		if (_cachedFrame == frame && (Object)(object)_cachedActiveCam != (Object)null)
			return _cachedActiveCam;

		_cachedFrame = frame;
		Camera cam = null;

		try
		{
			CameraZoom cz = CameraZoom.Instance;
			if ((Object)(object)cz != (Object)null)
			{
				cam = ((Component)cz).GetComponent<Camera>();
				if ((Object)(object)cam == (Object)null)
					cam = ((Component)cz).GetComponentInParent<Camera>();
				if ((Object)(object)cam == (Object)null)
					cam = ((Component)cz).GetComponentInChildren<Camera>();
				if ((Object)(object)cam != (Object)null && cam.enabled && cam.cullingMask != 0)
				{
					_cachedActiveCam = cam;
					return cam;
				}
			}
		}
		catch { }

		cam = Camera.main;
		if ((Object)(object)cam != (Object)null && cam.enabled && cam.cullingMask != 0)
		{
			_cachedActiveCam = cam;
			return cam;
		}

		try
		{
			Camera[] allCams = Camera.allCameras;
			Camera bestCam = null;
			float bestScore = float.MinValue;

			foreach (Camera c in allCams)
			{
				if ((Object)(object)c == (Object)null || !c.enabled) continue;
				if (c.cullingMask == 0) continue;

				float score = c.depth;
				if (c.targetTexture != null)
					score += 1000f;

				if (score > bestScore)
				{
					bestScore = score;
					bestCam = c;
				}
			}

			if ((Object)(object)bestCam != (Object)null)
			{
				_cachedActiveCam = bestCam;
				return bestCam;
			}
		}
		catch { }

		_cachedActiveCam = null;
		return null;
	}

	/// <summary>
	/// 强制清除相机缓存（场景切换时调用）
	/// </summary>
	public static void InvalidateCameraCache()
	{
		_cachedActiveCam = null;
		_cachedFrame = -1;
	}
}
