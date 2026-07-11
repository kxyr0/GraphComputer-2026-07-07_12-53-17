using Game.OS.Reputation;
using UnityEngine;

namespace Game.OS.Release
{
	[CreateAssetMenu(
		fileName = "OsReleaseGrowthProfile",
		menuName = "Graph Computer/OS/Release Growth Profile"
	)]
	public sealed class OsReleaseGrowthProfile : ScriptableObject
	{
		public const float DefaultGrowthIntervalSeconds = 6f;

		[Header("Timing")]
		[SerializeField, Min(0.5f)] private float growthIntervalSeconds = DefaultGrowthIntervalSeconds;

		[Header("Limits")]
		[SerializeField, Min(1)] private int maxInstallations = 999999;

		[Header("Discovery")]
		[SerializeField, Min(0)] private int firstWaveInstalls = 1;
		[SerializeField, Min(0)] private int minDiscoveryInstalls = 0;
		[SerializeField, Min(0)] private int maxDiscoveryInstalls = 12;

		[Header("Quality Thresholds")]
		[SerializeField, Range(0f, 1f)] private float quietQualityThreshold = 0.4f;
		[SerializeField, Range(0f, 1f)] private float goodQualityThreshold = 0.65f;
		[SerializeField, Range(0f, 1f)] private float excellentQualityThreshold = 0.85f;

		[Header("Word Of Mouth")]
		[SerializeField, Range(0f, 2f)] private float quietRecommendationRate = 0.03f;
		[SerializeField, Range(0f, 2f)] private float goodRecommendationRate = 0.28f;
		[SerializeField, Range(0f, 2f)] private float excellentRecommendationRate = 0.75f;
		[SerializeField, Range(0f, 1f)] private float badReputationChurnRate = 0.3f;

		[Header("Rating")]
		[SerializeField, Range(0.1f, 5f)] private float minimumRating = 1f;
		[SerializeField, Range(0.1f, 5f)] private float maximumRating = 5f;
		[SerializeField, Range(0.01f, 1f)] private float ratingSmoothing = 0.35f;

		public float GrowthIntervalSeconds => Mathf.Max(0.5f, growthIntervalSeconds);

		public OsReleaseGrowthResult Evaluate(
			int currentInstallations,
			float quality,
			float currentRating)
		{
			return Evaluate(
				currentInstallations,
				quality,
				currentRating,
				allowFirstWave: true
			);
		}

		public OsReleaseGrowthResult Evaluate(
			int currentInstallations,
			float quality,
			float currentRating,
			bool allowFirstWave)
		{
			return Evaluate(
				currentInstallations,
				quality,
				currentRating,
				allowFirstWave,
				OsReleaseAudienceModifiers.Neutral
			);
		}

		public OsReleaseGrowthResult Evaluate(
			int currentInstallations,
			float quality,
			float currentRating,
			bool allowFirstWave,
			OsReleaseAudienceModifiers audienceModifiers)
		{
			currentInstallations = Mathf.Clamp(currentInstallations, 0, maxInstallations);
			quality = Mathf.Clamp01(quality);

			int installDelta = CalculateInstallDelta(currentInstallations, quality, audienceModifiers);

			if (allowFirstWave && currentInstallations <= 0)
			{
				installDelta = Mathf.Max(CalculateFirstWaveInstalls(audienceModifiers), installDelta);
			}

			int nextInstallations = Mathf.Clamp(
				currentInstallations + installDelta,
				0,
				maxInstallations
			);

			OsReleaseReception reception = GetReception(quality, nextInstallations - currentInstallations);
			float nextRating = CalculateRating(nextInstallations, quality, currentRating);

			return new OsReleaseGrowthResult(
				nextInstallations,
				nextInstallations - currentInstallations,
				nextRating,
				reception,
				GetStatusText(reception, nextInstallations - currentInstallations)
			);
		}

		private int CalculateInstallDelta(
			int currentInstallations,
			float quality,
			OsReleaseAudienceModifiers audienceModifiers)
		{
			int discovery = Mathf.RoundToInt(CalculateDiscovery(quality) * audienceModifiers.DiscoveryMultiplier);
			int recommendations = Mathf.RoundToInt(
				currentInstallations *
				GetRecommendationRate(quality) *
				audienceModifiers.RecommendationMultiplier
			);
			int churn = Mathf.CeilToInt(
				currentInstallations *
				GetChurnRate(quality) *
				audienceModifiers.BadReputationChurnMultiplier
			);

			return discovery + recommendations - churn;
		}

		private int CalculateFirstWaveInstalls(OsReleaseAudienceModifiers audienceModifiers)
		{
			return Mathf.Max(0, Mathf.RoundToInt(firstWaveInstalls * audienceModifiers.FirstWaveMultiplier));
		}

		private int CalculateDiscovery(float quality)
		{
			if (quality < quietQualityThreshold)
			{
				float badFactor = quietQualityThreshold <= 0f
					? 0f
					: quality / quietQualityThreshold;
				float badDiscovery = maxDiscoveryInstalls * 0.2f;
				return Mathf.RoundToInt(Mathf.Lerp(0f, badDiscovery, badFactor));
			}

			float goodFactor = Mathf.InverseLerp(quietQualityThreshold, 1f, quality);
			return Mathf.RoundToInt(Mathf.Lerp(minDiscoveryInstalls, maxDiscoveryInstalls, goodFactor));
		}

		private float GetRecommendationRate(float quality)
		{
			if (quality >= excellentQualityThreshold)
			{
				float factor = Mathf.InverseLerp(excellentQualityThreshold, 1f, quality);
				return Mathf.Lerp(goodRecommendationRate, excellentRecommendationRate, factor);
			}

			if (quality >= goodQualityThreshold)
			{
				float factor = Mathf.InverseLerp(goodQualityThreshold, excellentQualityThreshold, quality);
				return Mathf.Lerp(quietRecommendationRate, goodRecommendationRate, factor);
			}

			return quality >= quietQualityThreshold ? quietRecommendationRate : 0f;
		}

		private float GetChurnRate(float quality)
		{
			if (quality >= quietQualityThreshold)
			{
				return 0f;
			}

			float factor = quietQualityThreshold <= 0f
				? 0f
				: quality / quietQualityThreshold;
			return Mathf.Lerp(badReputationChurnRate, 0f, factor);
		}

		private float CalculateRating(int activeInstallations, float quality, float currentRating)
		{
			if (activeInstallations <= 0)
			{
				return 0f;
			}

			float min = Mathf.Min(minimumRating, maximumRating);
			float max = Mathf.Max(minimumRating, maximumRating);
			float targetRating = Mathf.Lerp(min, max, quality);

			if (currentRating <= 0f)
			{
				return targetRating;
			}

			return Mathf.Lerp(currentRating, targetRating, ratingSmoothing);
		}

		private OsReleaseReception GetReception(float quality, int delta)
		{
			if (quality >= excellentQualityThreshold)
			{
				return OsReleaseReception.Excellent;
			}

			if (quality >= goodQualityThreshold)
			{
				return OsReleaseReception.Positive;
			}

			if (quality >= quietQualityThreshold)
			{
				return OsReleaseReception.Quiet;
			}

			return delta < 0 ? OsReleaseReception.Negative : OsReleaseReception.Quiet;
		}

		private static string GetStatusText(OsReleaseReception reception, int delta)
		{
			if (delta < 0 || reception == OsReleaseReception.Negative)
			{
				return "Bad Reputation";
			}

			return reception switch
			{
				OsReleaseReception.Excellent => "Excellent Word Of Mouth",
				OsReleaseReception.Positive => "Positive Word Of Mouth",
				OsReleaseReception.Quiet => "Quiet Growth",
				_ => "Current"
			};
		}
	}
}
