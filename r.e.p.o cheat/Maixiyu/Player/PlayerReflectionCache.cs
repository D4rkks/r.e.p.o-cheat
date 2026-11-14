using System;
using System.Reflection;

namespace r.e.p.o_cheat;

public static class PlayerReflectionCache
{
	public static Type PlayerControllerType { get; private set; }

	public static object PlayerControllerInstance { get; set; }

	public static FieldInfo PlayerAvatarScriptField { get; private set; }

	public static object PlayerAvatarScriptInstance { get; private set; }

	public static FieldInfo PlayerHealthField { get; private set; }

	public static object PlayerHealthInstance { get; private set; }

	public static FieldInfo MaxHealthField { get; private set; }

	public static FieldInfo EnergyStartField { get; private set; }

	public static FieldInfo EnergyCurrentField { get; private set; }

	public static FieldInfo FlashlightControllerField { get; private set; }

	public static object FlashlightControllerInstance { get; private set; }

	public static FieldInfo BaseIntensityField { get; private set; }

	public static FieldInfo CrouchTimeMinField { get; set; }

	public static FieldInfo PhotonViewField { get; set; }

	public static FieldInfo SprintRechargeTimeField { get; private set; }

	public static FieldInfo SprintRechargeAmountField { get; private set; }

	public static void CachePlayerControllerData()
	{
		PlayerControllerType = Type.GetType("PlayerController, Assembly-CSharp");
		if (PlayerControllerType == null)
		{
			return;
		}
		PlayerControllerInstance = GameHelper.FindObjectOfType(PlayerControllerType);
		if (PlayerControllerInstance == null)
		{
			return;
		}
		PlayerAvatarScriptField = PlayerControllerType.GetField("playerAvatarScript", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (PlayerAvatarScriptField == null)
		{
			return;
		}
		PlayerAvatarScriptInstance = PlayerAvatarScriptField.GetValue(PlayerControllerInstance);
		if (PlayerAvatarScriptInstance == null)
		{
			return;
		}
		PlayerHealthField = PlayerAvatarScriptInstance.GetType().GetField("playerHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (PlayerHealthField == null)
		{
			return;
		}
		PlayerHealthInstance = PlayerHealthField.GetValue(PlayerAvatarScriptInstance);
		if (PlayerHealthInstance == null)
		{
			return;
		}
		MaxHealthField = PlayerHealthInstance.GetType().GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		_ = MaxHealthField == null;
		EnergyStartField = PlayerControllerType.GetField("EnergyStart", BindingFlags.Instance | BindingFlags.Public);
		_ = EnergyStartField == null;
		EnergyCurrentField = PlayerControllerType.GetField("EnergyCurrent", BindingFlags.Instance | BindingFlags.Public);
		_ = EnergyCurrentField == null;
		CrouchTimeMinField = PlayerControllerType.GetField("CrouchTimeMin", BindingFlags.Instance | BindingFlags.Public);
		_ = CrouchTimeMinField == null;
		PhotonViewField = PlayerAvatarScriptInstance.GetType().GetField("photonView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		_ = PhotonViewField == null;
		FlashlightControllerField = PlayerAvatarScriptInstance.GetType().GetField("flashlightController", BindingFlags.Instance | BindingFlags.Public);
		if (FlashlightControllerField != null)
		{
			FlashlightControllerInstance = FlashlightControllerField.GetValue(PlayerAvatarScriptInstance);
			if (FlashlightControllerInstance != null)
			{
				BaseIntensityField = FlashlightControllerInstance.GetType().GetField("baseIntensity", BindingFlags.Instance | BindingFlags.NonPublic);
				_ = BaseIntensityField == null;
			}
		}
		SprintRechargeTimeField = PlayerControllerType.GetField("sprintRechargeTime", BindingFlags.Instance | BindingFlags.NonPublic);
		SprintRechargeAmountField = PlayerControllerType.GetField("sprintRechargeAmount", BindingFlags.Instance | BindingFlags.NonPublic);
	}
}
