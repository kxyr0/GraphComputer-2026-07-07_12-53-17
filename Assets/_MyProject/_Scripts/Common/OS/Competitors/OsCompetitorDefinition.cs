using System;
using UnityEngine;

namespace Game.OS.Competitors
{
	[Serializable]
	public sealed class OsCompetitorDefinition
	{
		[SerializeField] private string companyName = "Company";
		[SerializeField] private string operatingSystemName = "OS";
		[SerializeField, Min(0)] private int startingUsers;
		[SerializeField, Min(0)] private int flatGrowthPerStep = 100;
		[SerializeField, Min(0f)] private float percentGrowthPerStep = 0.01f;
		[SerializeField, Min(0)] private int maxUsers = 1000000;

		public string CompanyName => string.IsNullOrWhiteSpace(companyName) ? "Unknown Company" : companyName;
		public string OperatingSystemName => string.IsNullOrWhiteSpace(operatingSystemName) ? "Unknown OS" : operatingSystemName;
		public int StartingUsers => Mathf.Max(0, startingUsers);

		public int EvaluateNextUsers(int currentUsers)
		{
			int current = Mathf.Clamp(currentUsers, 0, Mathf.Max(0, maxUsers));
			int percentGrowth = Mathf.RoundToInt(current * Mathf.Max(0f, percentGrowthPerStep));
			int nextUsers = current + Mathf.Max(0, flatGrowthPerStep) + percentGrowth;

			return Mathf.Clamp(nextUsers, 0, Mathf.Max(0, maxUsers));
		}
	}
}
