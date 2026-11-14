using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// Grab stability HUD plus remaining-quota combo suggestion.
/// Read-only: uses PhysGrabObject / ValuableObject / RoundDirector fields.
/// </summary>
public static class HaulAssistant
{
	public static bool HudEnabled = true;

	public static string QuotaLine = "";
	public static string ComboLine = "";
	public static string GrabLine = "";
	public static string RiskLine = "";

	private static readonly FieldInfo GrabbedBody = AccessTools.Field(typeof(PhysGrabber), "grabbedPhysGrabObject");
	private static readonly FieldInfo DollarCurrent = AccessTools.Field(typeof(ValuableObject), "dollarValueCurrent");
	private static readonly FieldInfo HaulGoal = AccessTools.Field(typeof(RoundDirector), "haulGoal");
	private static readonly FieldInfo CurrentHaul = AccessTools.Field(typeof(RoundDirector), "currentHaul");
	private static readonly FieldInfo RoomCheck = AccessTools.Field(typeof(ValuableObject), "roomVolumeCheck");
	private static readonly FieldInfo InExtract = AccessTools.Field(typeof(RoomVolumeCheck), "inExtractionPoint");
	private static readonly FieldInfo ImpactHeavy = AccessTools.Field(typeof(PhysGrabObject), "impactHeavyTimer");
	private static readonly FieldInfo ImpactMedium = AccessTools.Field(typeof(PhysGrabObject), "impactMediumTimer");

	private static float _nextCombo;
	private static int _comboStamp;
	private static GUIStyle _title;
	private static GUIStyle _body;
	private static Texture2D _bg;

	public static void Tick()
	{
		if (!HudEnabled)
		{
			GrabLine = "";
			RiskLine = "";
			return;
		}

		UpdateQuota();
		UpdateGrab();
		if (Time.unscaledTime >= _nextCombo)
		{
			_nextCombo = Time.unscaledTime + 0.75f;
			UpdateCombo();
		}
	}

	public static void Draw()
	{
		if (!HudEnabled || Event.current.type != EventType.Repaint)
		{
			return;
		}
		if (string.IsNullOrEmpty(QuotaLine) && string.IsNullOrEmpty(GrabLine))
		{
			return;
		}
		EnsureGui();
		float width = 340f;
		float height = 118f;
		Rect rect = new Rect(16f, Screen.height - height - 18f, width, height);
		GUI.DrawTexture(rect, _bg);
		float y = rect.y + 8f;
		GUI.Label(new Rect(rect.x + 10f, y, width - 20f, 18f), L.T("haul.hud_title"), _title);
		y += 20f;
		if (!string.IsNullOrEmpty(QuotaLine))
		{
			GUI.Label(new Rect(rect.x + 10f, y, width - 20f, 16f), QuotaLine, _body);
			y += 16f;
		}
		if (!string.IsNullOrEmpty(ComboLine))
		{
			GUI.Label(new Rect(rect.x + 10f, y, width - 20f, 16f), ComboLine, _body);
			y += 16f;
		}
		if (!string.IsNullOrEmpty(GrabLine))
		{
			GUI.Label(new Rect(rect.x + 10f, y, width - 20f, 16f), GrabLine, _body);
			y += 16f;
		}
		if (!string.IsNullOrEmpty(RiskLine))
		{
			GUI.Label(new Rect(rect.x + 10f, y, width - 20f, 16f), RiskLine, _body);
		}
	}

	public static void Unload()
	{
		GrabLine = "";
		RiskLine = "";
		ComboLine = "";
		QuotaLine = "";
	}

	private static void UpdateQuota()
	{
		try
		{
			RoundDirector round = RoundDirector.instance;
			int goal = ReadInt(HaulGoal, round);
			if (round == null || goal <= 0)
			{
				QuotaLine = "";
				return;
			}
			int current = ReadInt(CurrentHaul, round);
			int need = Mathf.Max(0, goal - current);
			QuotaLine = L.T("haul.quota_fmt", current.ToString(), goal.ToString(), need.ToString());
		}
		catch
		{
			QuotaLine = "";
		}
	}

	private static void UpdateGrab()
	{
		try
		{
			PhysGrabber grabber = PhysGrabber.instance;
			PhysGrabObject body = grabber != null && GrabbedBody != null
				? GrabbedBody.GetValue(grabber) as PhysGrabObject
				: null;
			if (grabber == null || !grabber.grabbed || body == null)
			{
				GrabLine = "";
				RiskLine = "";
				return;
			}

			int value = 0;
			ValuableObject valuable = body.GetComponent<ValuableObject>();
			if (valuable != null)
			{
				value = ReadFloatAsInt(DollarCurrent, valuable);
			}

			float mass = body.massOriginal > 0.01f ? body.massOriginal : 1f;
			Vector3 vel = body.rb != null ? body.rb.velocity : Vector3.zero;
			Vector3 ang = body.rb != null ? body.rb.angularVelocity : Vector3.zero;
			float speed = vel.magnitude;
			float spin = ang.magnitude * Mathf.Rad2Deg;
			float tilt = Vector3.Angle(body.transform.up, Vector3.up);
			int grabbers = body.playerGrabbing != null ? body.playerGrabbing.Count : 0;
			float ratio = value / mass;
			float heavy = ReadFloat(ImpactHeavy, body);
			float medium = ReadFloat(ImpactMedium, body);

			GrabLine = L.T("haul.grab_fmt",
				value.ToString(),
				mass.ToString("F1"),
				ratio.ToString("F0"),
				speed.ToString("F1"),
				spin.ToString("F0"),
				tilt.ToString("F0"),
				grabbers.ToString());

			string risk = "haul.risk_stable";
			if (heavy > 0f || speed > 8f)
			{
				risk = "haul.risk_fast";
			}
			else if (medium > 0f || spin > 220f || tilt > 55f)
			{
				risk = "haul.risk_wobble";
			}
			else if (grabbers < 2 && mass > 2.2f)
			{
				risk = "haul.risk_need_help";
			}
			RiskLine = L.T(risk);
		}
		catch
		{
			GrabLine = "";
			RiskLine = "";
		}
	}

	private static void UpdateCombo()
	{
		try
		{
			RoundDirector round = RoundDirector.instance;
			int goal = ReadInt(HaulGoal, round);
			if (round == null || goal <= 0)
			{
				ComboLine = "";
				return;
			}
			int need = goal - ReadInt(CurrentHaul, round);
			if (need <= 0)
			{
				ComboLine = L.T("haul.combo_met");
				return;
			}

			var values = new List<int>();
			var names = new List<string>();
			ValuableObject[] all = UnityEngine.Object.FindObjectsOfType<ValuableObject>();
			int stamp = all != null ? all.Length * 17 + need : need;
			if (stamp == _comboStamp && !string.IsNullOrEmpty(ComboLine))
			{
				return;
			}
			_comboStamp = stamp;

			if (all != null)
			{
				for (int i = 0; i < all.Length; i++)
				{
					ValuableObject item = all[i];
					if (item == null)
					{
						continue;
					}
					if (IsInExtraction(item))
					{
						continue;
					}
					int value = ReadFloatAsInt(DollarCurrent, item);
					if (value <= 0)
					{
						continue;
					}
					values.Add(value);
					names.Add(((UnityEngine.Object)item).name.Replace("(Clone)", "").Trim() + " $" + value);
					if (values.Count >= 18)
					{
						break;
					}
				}
			}

			if (!HaulComboSolver.TryPick(values.ToArray(), need, out int[] indices, out int sum))
			{
				ComboLine = L.T("haul.combo_none");
				return;
			}

			int shown = Mathf.Min(indices.Length, 3);
			string list = "";
			for (int i = 0; i < shown; i++)
			{
				if (i > 0)
				{
					list += ", ";
				}
				list += names[indices[i]];
			}
			if (indices.Length > shown)
			{
				list += " +" + (indices.Length - shown);
			}
			ComboLine = L.T("haul.combo_fmt", need.ToString(), sum.ToString(), (sum - need).ToString(), list);
		}
		catch
		{
			ComboLine = "";
		}
	}

	private static bool IsInExtraction(ValuableObject item)
	{
		object check = RoomCheck != null ? RoomCheck.GetValue(item) : null;
		if (check == null || InExtract == null)
		{
			return false;
		}
		object raw = InExtract.GetValue(check);
		return raw is bool inside && inside;
	}

	private static int ReadInt(FieldInfo field, object instance)
	{
		if (field == null || instance == null)
		{
			return 0;
		}
		object raw = field.GetValue(instance);
		if (raw is int i)
		{
			return i;
		}
		if (raw is float f)
		{
			return (int)f;
		}
		return 0;
	}

	private static int ReadFloatAsInt(FieldInfo field, object instance)
	{
		return (int)ReadFloat(field, instance);
	}

	private static float ReadFloat(FieldInfo field, object instance)
	{
		if (field == null || instance == null)
		{
			return 0f;
		}
		object raw = field.GetValue(instance);
		if (raw is float f)
		{
			return f;
		}
		if (raw is int i)
		{
			return i;
		}
		return 0f;
	}

	private static void EnsureGui()
	{
		if (_title != null)
		{
			return;
		}
		_bg = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		_bg.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.1f, 0.72f));
		_bg.Apply();
		_title = new GUIStyle(GUI.skin.label)
		{
			fontSize = 12,
			fontStyle = FontStyle.Bold
		};
		_title.normal.textColor = new Color(1f, 0.85f, 0.4f);
		_body = new GUIStyle(GUI.skin.label)
		{
			fontSize = 11
		};
		_body.normal.textColor = Color.white;
	}
}
