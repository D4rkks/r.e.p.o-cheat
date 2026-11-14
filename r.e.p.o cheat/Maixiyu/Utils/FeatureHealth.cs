using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// Runtime probe for "clicked but nothing happened": types, methods, Harmony, scope.
/// </summary>
public static class FeatureHealth
{
	public struct Row
	{
		public string Id;
		public string NameKey;
		public string ScopeKey;
		public bool TypeOk;
		public bool MethodOk;
		public bool HarmonyOk;
		public string Detail;
	}

	public static int HarmonyPatched;
	public static readonly List<string> HarmonyFailures = new List<string>();
	public static string LastError = "";

	public static void RecordHarmony(int patched, IEnumerable<string> failures)
	{
		HarmonyPatched = patched;
		HarmonyFailures.Clear();
		if (failures == null)
		{
			return;
		}
		foreach (string failure in failures)
		{
			if (!string.IsNullOrEmpty(failure))
			{
				HarmonyFailures.Add(failure);
			}
		}
	}

	public static List<Row> Evaluate()
	{
		var rows = new List<Row>();
		try
		{
			rows.Add(Probe("scare_local", "health.scare_local", "scope.local",
				typeof(AudioScare), "PlayImpact", null));
			rows.Add(Probe("scare_ambience", "health.scare_ambience", "scope.host",
				typeof(AmbienceBreakers), "PlaySoundRPC", null));
			rows.Add(Probe("expression", "health.expression", "scope.self",
				typeof(PlayerAvatar), "PlayerExpressionSet", null));
			rows.Add(Probe("grab", "health.grab", "scope.self",
				typeof(PhysGrabber), null, null));
			rows.Add(Probe("haul", "health.haul", "scope.local",
				typeof(RoundDirector), null, null));
			rows.Add(Probe("gyro", "health.gyro", "scope.self",
				typeof(PhotonTransformView), "OnPhotonSerializeView", "GyroSpin"));
			rows.Add(Probe("midjoin", "health.midjoin", "scope.host",
				typeof(NetworkManager), "PlayerSpawnedRPC", "MidJoin"));
			rows.Add(MissingGuess("scare_guess", "health.scare_guess", "MakeNoiseRPC / PlaySound / DoorOpenRPC"));
		}
		catch (Exception ex)
		{
			LastError = ex.GetType().Name + ": " + ex.Message;
		}
		return rows;
	}

	public static bool HarmonyClassOk(string className)
	{
		if (string.IsNullOrEmpty(className))
		{
			return HarmonyFailures.Count == 0;
		}
		for (int i = 0; i < HarmonyFailures.Count; i++)
		{
			if (HarmonyFailures[i].IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return false;
			}
		}
		return true;
	}

	private static Row Probe(string id, string nameKey, string scopeKey, Type type, string method, string harmonyClass)
	{
		var row = new Row
		{
			Id = id,
			NameKey = nameKey,
			ScopeKey = scopeKey,
			TypeOk = type != null,
			MethodOk = true,
			HarmonyOk = HarmonyClassOk(harmonyClass),
			Detail = ""
		};
		if (type == null)
		{
			row.MethodOk = false;
			row.Detail = "type missing";
			return row;
		}
		if (!string.IsNullOrEmpty(method))
		{
			MethodInfo info = type.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			row.MethodOk = info != null;
			row.Detail = row.MethodOk ? type.Name + "." + method : type.Name + " missing " + method;
		}
		else
		{
			row.Detail = type.Name;
		}
		if (!row.HarmonyOk)
		{
			row.Detail = (row.Detail.Length > 0 ? row.Detail + " | " : "") + "Harmony failed";
		}
		return row;
	}

	private static Row MissingGuess(string id, string nameKey, string guessed)
	{
		return new Row
		{
			Id = id,
			NameKey = nameKey,
			ScopeKey = "scope.none",
			TypeOk = true,
			MethodOk = false,
			HarmonyOk = true,
			Detail = guessed
		};
	}
}
