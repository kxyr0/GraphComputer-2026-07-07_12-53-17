using System;
using UnityEngine;

namespace Game.Nodes.Runtime
{
	[Serializable]
	public struct OsNetworkEraBenchmark
	{
		[SerializeField, Min(0)] private int year;
		[SerializeField, Range(1, 12)] private int month;
		[SerializeField, Range(1, 30)] private int day;
		[SerializeField, Min(0f)] private float minimumMbps;
		[SerializeField, Min(0f)] private float goodMbps;
		[SerializeField, Min(0f)] private float excellentMbps;
		[SerializeField, Range(0f, 50f)] private float maxStabilityPenalty;
		[SerializeField, Range(0f, 50f)] private float maxLoadPenalty;

		public int Year => year;
		public int Month => month;
		public int Day => day;
		public float MinimumMbps => minimumMbps;
		public float GoodMbps => goodMbps;
		public float ExcellentMbps => excellentMbps;
		public float MaxStabilityPenalty => maxStabilityPenalty;
		public float MaxLoadPenalty => maxLoadPenalty;
		public int AbsoluteDay => ToAbsoluteDay(year, month, day);

		public OsNetworkEraBenchmark(
			int year,
			int month,
			int day,
			float minimumMbps,
			float goodMbps,
			float excellentMbps,
			float maxStabilityPenalty,
			float maxLoadPenalty)
		{
			this.year = year;
			this.month = Mathf.Clamp(month, 1, 12);
			this.day = Mathf.Clamp(day, 1, 30);
			this.minimumMbps = Mathf.Max(0f, minimumMbps);
			this.goodMbps = Mathf.Max(this.minimumMbps, goodMbps);
			this.excellentMbps = Mathf.Max(this.goodMbps, excellentMbps);
			this.maxStabilityPenalty = Mathf.Max(0f, maxStabilityPenalty);
			this.maxLoadPenalty = Mathf.Max(0f, maxLoadPenalty);
		}

		public static OsNetworkEraBenchmark[] Default1995To1996()
		{
			return new[]
			{
				new OsNetworkEraBenchmark(1995, 1, 1, 0.10f, 0.25f, 0.40f, 0f, 0f),
				new OsNetworkEraBenchmark(1996, 1, 1, 0.25f, 0.75f, 1.50f, 10f, 14f)
			};
		}

		public static int ToAbsoluteDay(int year, int month, int day)
		{
			return year * 360 + (month - 1) * 30 + (day - 1);
		}

		public static OsNetworkEraBenchmark Lerp(
			OsNetworkEraBenchmark from,
			OsNetworkEraBenchmark to,
			float t)
		{
			t = Mathf.Clamp01(t);

			return new OsNetworkEraBenchmark(
				from.year,
				from.month,
				from.day,
				Mathf.Lerp(from.minimumMbps, to.minimumMbps, t),
				Mathf.Lerp(from.goodMbps, to.goodMbps, t),
				Mathf.Lerp(from.excellentMbps, to.excellentMbps, t),
				Mathf.Lerp(from.maxStabilityPenalty, to.maxStabilityPenalty, t),
				Mathf.Lerp(from.maxLoadPenalty, to.maxLoadPenalty, t)
			);
		}
	}
}
