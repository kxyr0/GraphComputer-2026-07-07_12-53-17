using UnityEngine;

namespace Game.OS.Reputation
{
	public readonly struct OsReleaseAudienceModifiers
	{
		public static OsReleaseAudienceModifiers Neutral => new(1f, 1f, 1f, 1f);

		public readonly float FirstWaveMultiplier;
		public readonly float DiscoveryMultiplier;
		public readonly float RecommendationMultiplier;
		public readonly float BadReputationChurnMultiplier;

		public OsReleaseAudienceModifiers(
			float firstWaveMultiplier,
			float discoveryMultiplier,
			float recommendationMultiplier,
			float badReputationChurnMultiplier)
		{
			FirstWaveMultiplier = Mathf.Max(0f, firstWaveMultiplier);
			DiscoveryMultiplier = Mathf.Max(0f, discoveryMultiplier);
			RecommendationMultiplier = Mathf.Max(0f, recommendationMultiplier);
			BadReputationChurnMultiplier = Mathf.Max(0f, badReputationChurnMultiplier);
		}
	}
}
