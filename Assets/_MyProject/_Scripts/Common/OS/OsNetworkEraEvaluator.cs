using UnityEngine;

namespace Game.Nodes.Runtime
{
	public static class OsNetworkEraEvaluator
	{
		private static readonly OsNetworkEraBenchmark[] DefaultBenchmarks =
			OsNetworkEraBenchmark.Default1995To1996();

		public static OsNetworkEraAssessment Evaluate(
			float networkSpeedMbps,
			int currentAbsoluteDay,
			OsNetworkEraBenchmark[] benchmarks)
		{
			OsNetworkEraBenchmark benchmark = ResolveBenchmark(currentAbsoluteDay, benchmarks);
			float excellent = Mathf.Max(0.01f, benchmark.ExcellentMbps);
			float good = Mathf.Max(0.01f, benchmark.GoodMbps);
			float minimum = Mathf.Max(0.01f, benchmark.MinimumMbps);
			float speed = Mathf.Max(0f, networkSpeedMbps);
			OsNetworkEraQuality quality = ResolveQuality(speed, minimum, good, excellent);
			float score = excellent > 0f ? speed / excellent * 100f : 100f;
			float gap = speed >= good ? 0f : Mathf.Clamp01((good - speed) / good);

			return new OsNetworkEraAssessment(
				quality,
				score,
				benchmark.MaxStabilityPenalty * gap,
				benchmark.MaxLoadPenalty * gap
			);
		}

		private static OsNetworkEraQuality ResolveQuality(
			float speed,
			float minimum,
			float good,
			float excellent)
		{
			if (speed >= excellent)
			{
				return OsNetworkEraQuality.Excellent;
			}

			if (speed >= good)
			{
				return OsNetworkEraQuality.Good;
			}

			if (speed >= minimum)
			{
				return OsNetworkEraQuality.Aging;
			}

			return OsNetworkEraQuality.Obsolete;
		}

		private static OsNetworkEraBenchmark ResolveBenchmark(
			int currentAbsoluteDay,
			OsNetworkEraBenchmark[] benchmarks)
		{
			OsNetworkEraBenchmark[] source =
				benchmarks != null && benchmarks.Length > 0
					? benchmarks
					: DefaultBenchmarks;

			OsNetworkEraBenchmark previous = source[0];
			OsNetworkEraBenchmark next = source[source.Length - 1];

			for (int i = 0; i < source.Length; i++)
			{
				OsNetworkEraBenchmark current = source[i];

				if (current.AbsoluteDay <= currentAbsoluteDay)
				{
					previous = current;
				}

				if (current.AbsoluteDay >= currentAbsoluteDay)
				{
					next = current;
					break;
				}
			}

			int previousDay = previous.AbsoluteDay;
			int nextDay = next.AbsoluteDay;

			if (nextDay <= previousDay)
			{
				return previous;
			}

			float t = (currentAbsoluteDay - previousDay) / (float)(nextDay - previousDay);
			return OsNetworkEraBenchmark.Lerp(previous, next, t);
		}
	}
}
