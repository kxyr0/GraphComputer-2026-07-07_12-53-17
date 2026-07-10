namespace Game.UI.OS
{
	public static class OsInfoDefaults
	{
		public static OsInfoSection CreateStatusSection()
		{
			return new OsInfoSection(
				"Stata OS",
				new[]
				{
					new OsInfoEntry("OS Name", "GraphOS", OsInfoTone.Good),
					new OsInfoEntry("Version", "0.1.0-alpha", OsInfoTone.Warning),
					new OsInfoEntry("Build", "0007", OsInfoTone.Neutral),
					new OsInfoEntry("Codename", "Genesis"),
					new OsInfoEntry("Development Stage", "Prototype", OsInfoTone.Warning)
				}
			);
		}

		public static OsInfoSection CreateHealthSection()
		{
			return new OsInfoSection(
				"Health OS",
				new[]
				{
					new OsInfoEntry("Stability", "64%", OsInfoTone.Warning),
					new OsInfoEntry("Security", "31%", OsInfoTone.Bad),
					new OsInfoEntry("Performance", "72%", OsInfoTone.Good),
					new OsInfoEntry("Compatibility", "18%", OsInfoTone.Bad),
					new OsInfoEntry("Code Quality", "56%", OsInfoTone.Warning)
				}
			);
		}

		public static OsInfoSection CreateReleaseSection()
		{
			return new OsInfoSection(
				"Release Information",
				new[]
				{
					new OsInfoEntry("Release Channel", "None", OsInfoTone.Bad),
					new OsInfoEntry("Supported Devices", "None", OsInfoTone.Bad),
					new OsInfoEntry("Active Installations", "0", OsInfoTone.Bad),
					new OsInfoEntry("User Rating", "0.0 / 5", OsInfoTone.Bad),
					new OsInfoEntry("Latest Release", "None", OsInfoTone.Bad),
					new OsInfoEntry("Update Status", "Not Published", OsInfoTone.Bad)
				}
			);
		}
	}
}
