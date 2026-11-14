using System.Diagnostics;
using UnityEngine;

namespace r.e.p.o_cheat;

internal static class DLog
{
	[Conditional("DEBUG")]
	public static void Log(string message)
	{
		Debug.Log((object)message);
	}

	[Conditional("DEBUG")]
	public static void LogError(string message)
	{
		Debug.LogError((object)message);
	}

	[Conditional("DEBUG")]
	public static void LogWarning(string message)
	{
		Debug.LogWarning((object)message);
	}
}
