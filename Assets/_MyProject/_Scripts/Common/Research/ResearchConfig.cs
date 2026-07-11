using System;
using UnityEngine;

namespace Game.Research
{
	[CreateAssetMenu(
		fileName = "ResearchConfig",
		menuName = "Graph Computer/Research/Research Config"
	)]
	public sealed class ResearchConfig : ScriptableObject
	{
		[SerializeField] private ResearchDefinition[] researches;

		public ResearchDefinition[] Researches => researches ?? Array.Empty<ResearchDefinition>();
		public int Count => researches?.Length ?? 0;

		public bool TryGetDefinition(string researchId, out ResearchDefinition definition)
		{
			definition = null;

			if (string.IsNullOrWhiteSpace(researchId) || researches == null)
			{
				return false;
			}

			foreach (ResearchDefinition research in researches)
			{
				if (research == null || !research.Matches(researchId))
				{
					continue;
				}

				definition = research;
				return true;
			}

			return false;
		}

		public bool AreMutuallyExclusive(string firstResearchId, string secondResearchId)
		{
			if (string.IsNullOrWhiteSpace(firstResearchId) ||
				string.IsNullOrWhiteSpace(secondResearchId) ||
				string.Equals(firstResearchId, secondResearchId, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return TryGetDefinition(firstResearchId, out ResearchDefinition first) &&
				   TryGetDefinition(secondResearchId, out ResearchDefinition second) &&
				   (first.ListsMutualExclusion(secondResearchId) ||
					second.ListsMutualExclusion(firstResearchId));
		}

		public bool IsNodeUnlockedByCompletedResearch(
			string nodeDefinitionId,
			Func<string, bool> isResearchCompleted)
		{
			if (string.IsNullOrWhiteSpace(nodeDefinitionId) ||
				isResearchCompleted == null ||
				researches == null)
			{
				return false;
			}

			foreach (ResearchDefinition research in researches)
			{
				if (research == null ||
					!research.UnlocksNode(nodeDefinitionId) ||
					!isResearchCompleted(research.Id))
				{
					continue;
				}

				return true;
			}

			return false;
		}
	}
}
