using System;
using UnityEngine;

namespace Game.OS.Requirements
{
	[Serializable]
	public sealed class OsAnnualModuleRequirement
	{
		[SerializeField, Min(1995)] private int year = 1995;
		[SerializeField] private OsRequiredModule[] requiredModules;

		public int Year => year;
		public OsRequiredModule[] RequiredModules => requiredModules ?? Array.Empty<OsRequiredModule>();

		public bool AppliesTo(int currentYear)
		{
			return currentYear >= year;
		}
	}
}
