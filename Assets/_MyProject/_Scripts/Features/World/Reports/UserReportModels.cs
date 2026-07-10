using System;

namespace Game.World.Reports
{
	public enum UserReportSeverity
	{
		Info,
		Praise,
		Warning,
		Critical
	}

	public enum UserReportMood
	{
		Excellent,
		Stable,
		Mixed,
		Concerned,
		Angry,
		Critical
	}

	[Serializable]
	public sealed class UserReportEntry
	{
		public string Message;
		public UserReportSeverity Severity;
		public string Category;
		public string DateText;
		public string AuthorName;

		public UserReportEntry(
			string message,
			UserReportSeverity severity,
			string category,
			string dateText,
			string authorName)
		{
			Message = message ?? string.Empty;
			Severity = severity;
			Category = category ?? string.Empty;
			DateText = dateText ?? string.Empty;
			AuthorName = authorName ?? string.Empty;
		}
	}
}
