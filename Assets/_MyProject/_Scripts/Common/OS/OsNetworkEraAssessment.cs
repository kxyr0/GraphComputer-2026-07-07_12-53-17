using UnityEngine;

namespace Game.Nodes.Runtime
{
	public readonly struct OsNetworkEraAssessment
	{
		public readonly OsNetworkEraQuality Quality;
		public readonly float ScorePercent;
		public readonly float StabilityPenalty;
		public readonly float LoadPenalty;

		public string QualityText => Quality switch
		{
			OsNetworkEraQuality.Excellent => "Excellent",
			OsNetworkEraQuality.Good => "Good",
			OsNetworkEraQuality.Aging => "Aging",
			_ => "Obsolete"
		};

		public OsNetworkEraAssessment(
			OsNetworkEraQuality quality,
			float scorePercent,
			float stabilityPenalty,
			float loadPenalty)
		{
			Quality = quality;
			ScorePercent = Mathf.Clamp(scorePercent, 0f, 150f);
			StabilityPenalty = Mathf.Max(0f, stabilityPenalty);
			LoadPenalty = Mathf.Max(0f, loadPenalty);
		}
	}
}
