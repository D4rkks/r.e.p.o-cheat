using System;

namespace r.e.p.o_cheat;

/// <summary>
/// Min-overshoot subset of item values for the remaining extraction quota.
/// Pure C# so tests can run without a live round.
/// </summary>
public static class HaulComboSolver
{
	public static bool TryPick(int[] values, int target, out int[] indices, out int sum)
	{
		indices = Array.Empty<int>();
		sum = 0;
		if (values == null || values.Length == 0 || target <= 0)
		{
			return false;
		}

		int n = values.Length;
		if (n <= 18)
		{
			return TryExact(values, target, out indices, out sum);
		}
		return TryGreedy(values, target, out indices, out sum);
	}

	private static bool TryExact(int[] values, int target, out int[] indices, out int sum)
	{
		int n = values.Length;
		int bestMask = 0;
		int bestSum = int.MaxValue;
		int bestCount = int.MaxValue;
		int limit = 1 << n;
		for (int mask = 1; mask < limit; mask++)
		{
			int total = 0;
			int count = 0;
			for (int i = 0; i < n; i++)
			{
				if ((mask & (1 << i)) == 0)
				{
					continue;
				}
				total += values[i];
				count++;
				if (total >= target && total - target > bestSum - target && bestSum >= target)
				{
					break;
				}
			}
			if (total < target)
			{
				continue;
			}
			int over = total - target;
			int bestOver = bestSum == int.MaxValue ? int.MaxValue : bestSum - target;
			if (over < bestOver || (over == bestOver && count < bestCount))
			{
				bestMask = mask;
				bestSum = total;
				bestCount = count;
			}
		}

		if (bestSum == int.MaxValue)
		{
			return TryGreedy(values, target, out indices, out sum);
		}

		sum = bestSum;
		indices = MaskToIndices(bestMask, n);
		return true;
	}

	private static bool TryGreedy(int[] values, int target, out int[] indices, out int sum)
	{
		int n = values.Length;
		int[] order = new int[n];
		for (int i = 0; i < n; i++)
		{
			order[i] = i;
		}
		Array.Sort(order, (a, b) => values[b].CompareTo(values[a]));

		var picked = new System.Collections.Generic.List<int>();
		sum = 0;
		for (int i = 0; i < n; i++)
		{
			int idx = order[i];
			if (values[idx] <= 0)
			{
				continue;
			}
			picked.Add(idx);
			sum += values[idx];
			if (sum >= target)
			{
				break;
			}
		}

		for (int i = picked.Count - 1; i >= 0; i--)
		{
			int value = values[picked[i]];
			if (sum - value >= target)
			{
				sum -= value;
				picked.RemoveAt(i);
			}
		}

		indices = picked.ToArray();
		return indices.Length > 0;
	}

	private static int[] MaskToIndices(int mask, int n)
	{
		int count = 0;
		for (int i = 0; i < n; i++)
		{
			if ((mask & (1 << i)) != 0)
			{
				count++;
			}
		}
		int[] indices = new int[count];
		int w = 0;
		for (int i = 0; i < n; i++)
		{
			if ((mask & (1 << i)) != 0)
			{
				indices[w++] = i;
			}
		}
		return indices;
	}
}
