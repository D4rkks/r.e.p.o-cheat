using System;
using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat;

/// <summary>
/// 场景对象缓存 — 按类型定时刷新 FindObjectsOfType 结果，
/// 避免各功能在 Update/OnGUI 中每帧全场景扫描。
/// 同一类型的所有调用方共享一个快照，刷新间隔取各调用方请求的最小值。
/// 缓存数组在每次刷新时重建，两次刷新之间被销毁的对象按 UnityEngine.Object 空语义过滤。
/// </summary>
public static class SceneCache
{
	private class Entry
	{
		public Array objects;

		public float nextRefreshTime;
	}

	private static readonly Dictionary<Type, Entry> _entries = new Dictionary<Type, Entry>();

	/// <summary>
	/// 获取指定类型的活跃对象快照（与 FindObjectsOfType 语义一致：仅活跃对象）。
	/// 首次调用立即扫描，之后按 interval 秒刷新；多个调用方共享同一类型的缓存。
	/// </summary>
	public static T[] GetObjects<T>(float interval) where T : UnityEngine.Object
	{
		Type type = typeof(T);
		if (!_entries.TryGetValue(type, out Entry entry))
		{
			entry = new Entry();
			_entries[type] = entry;
		}

		if (Time.time >= entry.nextRefreshTime)
		{
			entry.objects = UnityEngine.Object.FindObjectsOfType<T>();
			entry.nextRefreshTime = Time.time + Mathf.Max(0.05f, interval);
		}
		else
		{
			// 刷新截止时间取各调用方请求间隔的最小值
			float candidate = Time.time + Mathf.Max(0.05f, interval);
			if (candidate < entry.nextRefreshTime)
			{
				entry.nextRefreshTime = candidate;
			}
		}

		T[] cached = (T[])entry.objects;
		if (cached == null || cached.Length == 0)
		{
			return cached;
		}

		// 过滤两次刷新之间被销毁的对象（UnityEngine.Object 空语义）
		int valid = 0;
		for (int i = 0; i < cached.Length; i++)
		{
			if ((UnityEngine.Object)(object)cached[i] != null)
			{
				cached[valid] = cached[i];
				valid++;
			}
		}
		if (valid != cached.Length)
		{
			Array trimmed = Array.CreateInstance(type, valid);
			Array.Copy(cached, trimmed, valid);
			entry.objects = trimmed;
			cached = (T[])trimmed;
		}
		return cached;
	}
}
