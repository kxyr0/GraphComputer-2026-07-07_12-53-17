using System;
using UnityEngine;

namespace Game.OS.Requirements
{
	[Serializable]
	public sealed class OsRequiredModule
	{
		[SerializeField] private string moduleId;
		[SerializeField] private string displayName;
		[SerializeField] private string[] alternateModuleIds;
		[SerializeField, Range(0f, 1f)] private float missingQualityPenalty = 0.1f;

		public string ModuleId => moduleId ?? string.Empty;
		public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ModuleId : displayName;
		public float MissingQualityPenalty => Mathf.Clamp01(missingQualityPenalty);

		public bool IsSatisfiedBy(Func<string, bool> hasModule)
		{
			if (hasModule == null)
			{
				return false;
			}

			if (hasModule(ModuleId))
			{
				return true;
			}

			if (alternateModuleIds == null)
			{
				return false;
			}

			foreach (string alternateId in alternateModuleIds)
			{
				if (!string.IsNullOrWhiteSpace(alternateId) && hasModule(alternateId))
				{
					return true;
				}
			}

			return false;
		}
	}
}
