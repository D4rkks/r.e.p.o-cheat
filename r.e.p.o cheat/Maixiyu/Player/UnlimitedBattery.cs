using UnityEngine;

namespace r.e.p.o_cheat;

public class UnlimitedBattery : MonoBehaviour
{
	public bool unlimitedBatteryEnabled;

	private void Awake()
	{
		Object.DontDestroyOnLoad(gameObject);
	}

	private void Update()
	{
		if (!unlimitedBatteryEnabled && !Hax2.unlimitedBatteryActive)
		{
			BatteryKeepAlive.ApplyDirectorFlag();
			return;
		}
		unlimitedBatteryEnabled = Hax2.unlimitedBatteryActive;
		BatteryKeepAlive.ApplyDirectorFlag();
	}

	private void OnDisable()
	{
		BatteryKeepAlive.ApplyDirectorFlag();
	}
}
