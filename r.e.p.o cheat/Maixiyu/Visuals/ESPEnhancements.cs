using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// ESP presets. Trace lines are drawn by EspOverlay.
/// </summary>
public static class ESPEnhancements
{
	public static bool showTraceLinesEnemy;
	public static bool showTraceLinesItem;
	public static bool showTraceLinesPlayer;

	public enum ESPPreset
	{
		Custom,
		HighValueOnly,
		EnemyOnly,
		PlayerOnly,
		Everything,
		Stealth
	}

	public static ESPPreset currentPreset = ESPPreset.Custom;

	public static string[] GetPresetNames()
	{
		return new string[]
		{
			L.T("preset.custom"),
			L.T("preset.high_value"),
			L.T("preset.enemy_only"),
			L.T("preset.player_only"),
			L.T("preset.everything"),
			L.T("preset.stealth")
		};
	}

	public static readonly string[] presetNames = new string[]
	{
		"Custom",
		"High Value",
		"Enemy",
		"Player",
		"All",
		"Stealth"
	};

	public static void DrawTraceLines()
	{
	}

	public static void ApplyPreset(ESPPreset preset)
	{
		currentPreset = preset;
		switch (preset)
		{
			case ESPPreset.HighValueOnly:
				MaiDebugCheats.drawEspBool = false;
				MaiDebugCheats.drawItemEspBool = true;
				MaiDebugCheats.drawPlayerEspBool = false;
				MaiDebugCheats.drawExtractionPointEspBool = true;
				MaiDebugCheats.showItemValue = true;
				MaiDebugCheats.showItemNames = true;
				MaiDebugCheats.showItemDistance = true;
				MaiDebugCheats.minItemValue = 500;
				showTraceLinesEnemy = false;
				showTraceLinesItem = true;
				showTraceLinesPlayer = false;
				break;
			case ESPPreset.EnemyOnly:
				MaiDebugCheats.drawEspBool = true;
				MaiDebugCheats.drawItemEspBool = false;
				MaiDebugCheats.drawPlayerEspBool = false;
				MaiDebugCheats.drawExtractionPointEspBool = false;
				MaiDebugCheats.showEnemyNames = true;
				MaiDebugCheats.showEnemyDistance = true;
				MaiDebugCheats.showEnemyHP = true;
				showTraceLinesEnemy = true;
				showTraceLinesItem = false;
				showTraceLinesPlayer = false;
				break;
			case ESPPreset.PlayerOnly:
				MaiDebugCheats.drawEspBool = false;
				MaiDebugCheats.drawItemEspBool = false;
				MaiDebugCheats.drawPlayerEspBool = true;
				MaiDebugCheats.drawExtractionPointEspBool = false;
				MaiDebugCheats.showPlayerNames = true;
				MaiDebugCheats.showPlayerDistance = true;
				MaiDebugCheats.showPlayerHP = true;
				showTraceLinesEnemy = false;
				showTraceLinesItem = false;
				showTraceLinesPlayer = true;
				break;
			case ESPPreset.Everything:
				MaiDebugCheats.drawEspBool = true;
				MaiDebugCheats.drawItemEspBool = true;
				MaiDebugCheats.drawPlayerEspBool = true;
				MaiDebugCheats.drawExtractionPointEspBool = true;
				MaiDebugCheats.showEnemyNames = true;
				MaiDebugCheats.showEnemyDistance = true;
				MaiDebugCheats.showEnemyHP = true;
				MaiDebugCheats.showEnemyBox = true;
				MaiDebugCheats.showItemNames = true;
				MaiDebugCheats.showItemValue = true;
				MaiDebugCheats.showItemDistance = true;
				MaiDebugCheats.showPlayerNames = true;
				MaiDebugCheats.showPlayerDistance = true;
				MaiDebugCheats.showPlayerHP = true;
				MaiDebugCheats.showExtractionNames = true;
				MaiDebugCheats.showExtractionDistance = true;
				showTraceLinesEnemy = true;
				showTraceLinesItem = true;
				showTraceLinesPlayer = true;
				break;
			case ESPPreset.Stealth:
				MaiDebugCheats.drawEspBool = true;
				MaiDebugCheats.drawItemEspBool = false;
				MaiDebugCheats.drawPlayerEspBool = false;
				MaiDebugCheats.drawExtractionPointEspBool = true;
				MaiDebugCheats.showEnemyNames = false;
				MaiDebugCheats.showEnemyDistance = true;
				MaiDebugCheats.showEnemyHP = false;
				MaiDebugCheats.showEnemyBox = false;
				MaiDebugCheats.drawChamsBool = false;
				showTraceLinesEnemy = false;
				showTraceLinesItem = false;
				showTraceLinesPlayer = false;
				break;
		}
	}
}
