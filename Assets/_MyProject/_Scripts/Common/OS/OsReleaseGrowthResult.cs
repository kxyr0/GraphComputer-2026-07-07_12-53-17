namespace Game.OS.Release
{
	public enum OsReleaseReception
	{
		None,
		Negative,
		Quiet,
		Positive,
		Excellent
	}

	public readonly struct OsReleaseGrowthResult
	{
		public readonly int ActiveInstallations;
		public readonly int InstallDelta;
		public readonly float UserRating;
		public readonly OsReleaseReception Reception;
		public readonly string UpdateStatus;

		public OsReleaseGrowthResult(
			int activeInstallations,
			int installDelta,
			float userRating,
			OsReleaseReception reception,
			string updateStatus)
		{
			ActiveInstallations = activeInstallations;
			InstallDelta = installDelta;
			UserRating = userRating;
			Reception = reception;
			UpdateStatus = updateStatus ?? string.Empty;
		}
	}
}
