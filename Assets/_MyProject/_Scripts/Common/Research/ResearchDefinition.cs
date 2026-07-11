using System;
using UnityEngine;

namespace Game.Research
{
	[Serializable]
	public sealed class ResearchDefinition
	{
		[SerializeField] private string id;
		[SerializeField] private string tabId = "system_core";
		[SerializeField] private string title;
		[SerializeField, TextArea] private string description;
		[SerializeField, Min(1)] private int costDays = 1;
		[SerializeField] private Vector2Int gridPosition;
		[SerializeField] private string[] prerequisiteIds;
		[SerializeField] private string[] mutualExclusionIds;
		[SerializeField] private string[] unlockedNodeDefinitionIds;

		public string Id => id ?? string.Empty;
		public string TabId => string.IsNullOrWhiteSpace(tabId) ? "system_core" : tabId;
		public string Title => string.IsNullOrWhiteSpace(title) ? Id : title;
		public string Description => description ?? string.Empty;
		public int CostDays => Mathf.Max(1, costDays);
		public Vector2Int GridPosition => gridPosition;
		public string[] PrerequisiteIds => prerequisiteIds ?? Array.Empty<string>();
		public string[] MutualExclusionIds => mutualExclusionIds ?? Array.Empty<string>();
		public string[] UnlockedNodeDefinitionIds => unlockedNodeDefinitionIds ?? Array.Empty<string>();

		public bool Matches(string researchId)
		{
			return string.Equals(Id, researchId, StringComparison.OrdinalIgnoreCase);
		}

		public bool IsInTab(string researchTabId)
		{
			return string.IsNullOrWhiteSpace(researchTabId) ||
				   string.Equals(TabId, researchTabId, StringComparison.OrdinalIgnoreCase);
		}

		public bool ListsMutualExclusion(string researchId)
		{
			if (string.IsNullOrWhiteSpace(researchId))
			{
				return false;
			}

			foreach (string excludedId in MutualExclusionIds)
			{
				if (string.Equals(excludedId, researchId, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		public bool UnlocksNode(string nodeDefinitionId)
		{
			if (string.IsNullOrWhiteSpace(nodeDefinitionId))
			{
				return false;
			}

			foreach (string unlockedNodeId in UnlockedNodeDefinitionIds)
			{
				if (string.Equals(unlockedNodeId, nodeDefinitionId, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
	}
}
