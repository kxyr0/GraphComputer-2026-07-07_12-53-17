using Game.OS.Release;
using UnityEngine;

namespace Game.OS.Reputation
{
	[CreateAssetMenu(
		fileName = "OsPlayerReputationProfile",
		menuName = "Graph Computer/OS/Player Reputation Profile"
	)]
	public sealed class OsPlayerReputationProfile : ScriptableObject
	{
		public const float DefaultInitialReputationPercent = 0f;

		[Header("State")]
		[SerializeField, Range(0f, 100f)] private float initialReputationPercent = DefaultInitialReputationPercent;

		[Header("Quality Thresholds")]
		[SerializeField, Range(0f, 1f)] private float badQualityThreshold = 0.4f;
		[SerializeField, Range(0f, 1f)] private float goodQualityThreshold = 0.65f;
		[SerializeField, Range(0f, 1f)] private float excellentQualityThreshold = 0.85f;

		[Header("Reputation Change")]
		[SerializeField] private float quietDriftPerStep;
		[SerializeField, Min(0f)] private float positiveGainPerStep = 1.25f;
		[SerializeField, Min(0f)] private float excellentGainPerStep = 2.5f;
		[SerializeField, Min(0f)] private float negativeLossPerStep = 4f;
		[SerializeField, Min(1)] private int installationsPerBonusPoint = 100;
		[SerializeField, Min(0f)] private float installBonusPerPoint = 0.35f;
		[SerializeField, Min(0f)] private float maxInstallBonusPerStep = 3f;

		[Header("Audience Multipliers")]
		[SerializeField, Min(0f)] private float firstWaveMultiplierAtZero = 1f;
		[SerializeField, Min(0f)] private float firstWaveMultiplierAtMax = 6f;
		[SerializeField, Min(0f)] private float discoveryMultiplierAtZero = 1f;
		[SerializeField, Min(0f)] private float discoveryMultiplierAtMax = 2.25f;
		[SerializeField, Min(0f)] private float recommendationMultiplierAtZero = 1f;
		[SerializeField, Min(0f)] private float recommendationMultiplierAtMax = 2f;
		[SerializeField, Min(0f)] private float badChurnMultiplierAtZero = 1.25f;
		[SerializeField, Min(0f)] private float badChurnMultiplierAtMax = 0.65f;

		public float InitialReputationPercent => Mathf.Clamp(initialReputationPercent, 0f, 100f);

		public OsReleaseAudienceModifiers EvaluateAudienceModifiers(float reputationPercent)
		{
			float factor = Mathf.Clamp01(reputationPercent / 100f);

			return new OsReleaseAudienceModifiers(
				Mathf.Lerp(firstWaveMultiplierAtZero, firstWaveMultiplierAtMax, factor),
				Mathf.Lerp(discoveryMultiplierAtZero, discoveryMultiplierAtMax, factor),
				Mathf.Lerp(recommendationMultiplierAtZero, recommendationMultiplierAtMax, factor),
				Mathf.Lerp(badChurnMultiplierAtZero, badChurnMultiplierAtMax, factor)
			);
		}

		public float EvaluateNextReputation(
			float currentReputationPercent,
			float releaseQuality,
			OsReleaseReception reception,
			int installDelta)
		{
			float current = Mathf.Clamp(currentReputationPercent, 0f, 100f);
			float quality = Mathf.Clamp01(releaseQuality);
			float delta = CalculateReceptionDelta(quality, reception) +
						  CalculateInstallBonus(quality, installDelta);

			return Mathf.Clamp(current + delta, 0f, 100f);
		}

		public string GetTierText(float reputationPercent)
		{
			float reputation = Mathf.Clamp(reputationPercent, 0f, 100f);

			if (reputation >= 90f)
			{
				return "Famous";
			}

			if (reputation >= 70f)
			{
				return "Trusted";
			}

			if (reputation >= 45f)
			{
				return "Recognized";
			}

			if (reputation >= 20f)
			{
				return "Niche";
			}

			return reputation > 0f ? "Unknown" : "None";
		}

		private float CalculateReceptionDelta(float quality, OsReleaseReception reception)
		{
			if (quality < badQualityThreshold || reception == OsReleaseReception.Negative)
			{
				float failureFactor = badQualityThreshold <= 0f
					? 1f
					: 1f - Mathf.Clamp01(quality / badQualityThreshold);

				return -negativeLossPerStep * Mathf.Lerp(0.35f, 1f, failureFactor);
			}

			if (quality >= excellentQualityThreshold || reception == OsReleaseReception.Excellent)
			{
				return excellentGainPerStep;
			}

			if (quality >= goodQualityThreshold || reception == OsReleaseReception.Positive)
			{
				float factor = Mathf.InverseLerp(goodQualityThreshold, excellentQualityThreshold, quality);
				return Mathf.Lerp(positiveGainPerStep, excellentGainPerStep, factor);
			}

			return quietDriftPerStep;
		}

		private float CalculateInstallBonus(float quality, int installDelta)
		{
			if (installDelta <= 0 || quality < goodQualityThreshold)
			{
				return 0f;
			}

			float bonusPoints = installDelta / (float)Mathf.Max(1, installationsPerBonusPoint);
			return Mathf.Min(maxInstallBonusPerStep, bonusPoints * installBonusPerPoint);
		}
	}
}
