using System.Globalization;

namespace Game.OS.Release
{
	public readonly struct OsReleaseState
	{
		public readonly bool IsPublished;
		public readonly string ReleaseChannel;
		public readonly string SupportedDevices;
		public readonly int ActiveInstallations;
		public readonly float UserRating;
		public readonly string LatestRelease;
		public readonly string UpdateStatus;

		public string ActiveInstallationsText => ActiveInstallations.ToString("N0", CultureInfo.InvariantCulture);
		public string UserRatingText => $"{UserRating:0.0} / 5";

		public OsReleaseState(
			bool isPublished,
			string releaseChannel,
			string supportedDevices,
			int activeInstallations,
			float userRating,
			string latestRelease,
			string updateStatus)
		{
			IsPublished = isPublished;
			ReleaseChannel = releaseChannel ?? "None";
			SupportedDevices = supportedDevices ?? "None";
			ActiveInstallations = activeInstallations;
			UserRating = userRating;
			LatestRelease = latestRelease ?? "None";
			UpdateStatus = updateStatus ?? "Not Published";
		}
	}
}
