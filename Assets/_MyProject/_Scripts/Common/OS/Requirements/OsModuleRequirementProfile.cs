using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.OS.Requirements
{
	[CreateAssetMenu(
		fileName = "OsModuleRequirementProfile",
		menuName = "Graph Computer/OS/Module Requirement Profile"
	)]
	public sealed class OsModuleRequirementProfile : ScriptableObject
	{
		[SerializeField] private OsAnnualModuleRequirement[] annualRequirements;

		public OsModuleRequirementEvaluation Evaluate(
			int currentYear,
			Func<string, bool> hasModule)
		{
			if (annualRequirements == null || annualRequirements.Length == 0)
			{
				return new OsModuleRequirementEvaluation(
					currentYear,
					Array.Empty<string>(),
					0,
					0f,
					"Current"
				);
			}

			List<string> missingModuleNames = new();
			HashSet<string> seenModuleIds = new(StringComparer.OrdinalIgnoreCase);
			int requiredCount = 0;
			float qualityPenalty = 0f;

			foreach (OsAnnualModuleRequirement annualRequirement in annualRequirements)
			{
				if (annualRequirement == null || !annualRequirement.AppliesTo(currentYear))
				{
					continue;
				}

				foreach (OsRequiredModule module in annualRequirement.RequiredModules)
				{
					if (module == null || string.IsNullOrWhiteSpace(module.ModuleId))
					{
						continue;
					}

					if (!seenModuleIds.Add(module.ModuleId))
					{
						continue;
					}

					requiredCount++;

					if (module.IsSatisfiedBy(hasModule))
					{
						continue;
					}

					missingModuleNames.Add(module.DisplayName);
					qualityPenalty += module.MissingQualityPenalty;
				}
			}

			string statusText = missingModuleNames.Count > 0
				? $"Outdated OS: Missing {missingModuleNames.Count} module(s)"
				: "Current";

			return new OsModuleRequirementEvaluation(
				currentYear,
				missingModuleNames.ToArray(),
				requiredCount,
				Mathf.Clamp01(qualityPenalty),
				statusText
			);
		}
	}
}
