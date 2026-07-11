using System.Globalization;

namespace Game.OS.Competitors
{
	public readonly struct OsCompetitorState
	{
		public readonly string CompanyName;
		public readonly string OperatingSystemName;
		public readonly int StartingUsers;
		public readonly int CurrentUsers;
		public readonly bool IsPlayer;

		public string CurrentUsersText => CurrentUsers.ToString("N0", CultureInfo.InvariantCulture);
		public string StartingUsersText => StartingUsers.ToString("N0", CultureInfo.InvariantCulture);
		public string DisplayValue => $"{OperatingSystemName} | {CurrentUsersText} users";

		public OsCompetitorState(
			string companyName,
			string operatingSystemName,
			int startingUsers,
			int currentUsers,
			bool isPlayer = false)
		{
			CompanyName = string.IsNullOrWhiteSpace(companyName) ? "Unknown Company" : companyName;
			OperatingSystemName = string.IsNullOrWhiteSpace(operatingSystemName) ? "Unknown OS" : operatingSystemName;
			StartingUsers = startingUsers < 0 ? 0 : startingUsers;
			CurrentUsers = currentUsers < 0 ? 0 : currentUsers;
			IsPlayer = isPlayer;
		}
	}
}
