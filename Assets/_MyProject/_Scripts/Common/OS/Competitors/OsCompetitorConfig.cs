using UnityEngine;

namespace Game.OS.Competitors
{
	[CreateAssetMenu(
		fileName = "OsCompetitorConfig",
		menuName = "Graph Computer/OS/Competitor Config"
	)]
	public sealed class OsCompetitorConfig : ScriptableObject
	{
		public const float DefaultGrowthIntervalSeconds = 8f;

		[SerializeField, Min(0.5f)] private float growthIntervalSeconds = DefaultGrowthIntervalSeconds;
		[SerializeField] private OsCompetitorDefinition[] competitors;

		public float GrowthIntervalSeconds => Mathf.Max(0.5f, growthIntervalSeconds);
		public int CompetitorCount => competitors?.Length ?? 0;

		public bool TryGetCompetitor(int index, out OsCompetitorDefinition competitor)
		{
			competitor = null;

			if (competitors == null || index < 0 || index >= competitors.Length)
			{
				return false;
			}

			competitor = competitors[index];
			return competitor != null;
		}
	}
}
