using System;

namespace Game.OS.Requirements
{
	public readonly struct OsModuleRequirementEvaluation
	{
		public static OsModuleRequirementEvaluation NotConfigured(int year)
		{
			return new OsModuleRequirementEvaluation(
				year,
				Array.Empty<string>(),
				0,
				0f,
				"Not Configured"
			);
		}

		public readonly int Year;
		public readonly string[] MissingModuleNames;
		public readonly int RequiredModuleCount;
		public readonly float QualityPenalty;
		public readonly string StatusText;

		public bool HasMissingModules => MissingModuleNames != null && MissingModuleNames.Length > 0;
		public int MissingModuleCount => MissingModuleNames?.Length ?? 0;
		public string MissingModulesText => HasMissingModules ? string.Join(", ", MissingModuleNames) : "None";

		public OsModuleRequirementEvaluation(
			int year,
			string[] missingModuleNames,
			int requiredModuleCount,
			float qualityPenalty,
			string statusText)
		{
			Year = year;
			MissingModuleNames = missingModuleNames ?? Array.Empty<string>();
			RequiredModuleCount = Math.Max(0, requiredModuleCount);
			QualityPenalty = Math.Max(0f, qualityPenalty);
			StatusText = string.IsNullOrWhiteSpace(statusText) ? "Current" : statusText;
		}
	}
}
