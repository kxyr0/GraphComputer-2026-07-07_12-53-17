using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Game.IDE.Runtime
{
	public static class TimeModuleSpeedParser
	{
		private static readonly Regex ModuleRegex = new(
			@"^\s*module\s+Time\s*$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled
		);

		private static readonly Regex SpeedKeywordRegex = new(
			@"^\s*(speed|time|gameSpeed|timeSpeed|set\s+speed|run\s+time)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled
		);

		private static readonly Regex SpeedRegex = new(
			@"^\s*((speed|time|gameSpeed|timeSpeed)\s*(=|:|to)?|set\s+speed\s+to|run\s+time\s+at)\s*(?<value>[0-9]+([.,][0-9]+)?)\s*x?\s*$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled
		);

		public static bool IsTimeModule(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				return false;
			}

			string[] lines = code.Split(
				new[] { "\r\n", "\n", "\r" },
				StringSplitOptions.None
			);

			foreach (string rawLine in lines)
			{
				string line = RemoveComment(rawLine).Trim();

				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				return ModuleRegex.IsMatch(line);
			}

			return false;
		}

		public static bool IsTimeSpeedLine(string line)
		{
			return TryParseSpeedLine(line, out _, out _);
		}

		public static bool IsTimeSpeedCandidateLine(string line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				return false;
			}

			line = RemoveComment(line).Trim();

			return SpeedKeywordRegex.IsMatch(line);
		}

		public static bool TryParseSpeedLine(
			string line,
			out float speed,
			out string error)
		{
			speed = 1f;
			error = string.Empty;

			line = RemoveComment(line).Trim();

			if (!IsTimeSpeedCandidateLine(line))
			{
				error = "Line is not a Time speed statement.";
				return false;
			}

			Match match = SpeedRegex.Match(line);

			if (!match.Success)
			{
				error = "Expected speed syntax like 'time = 60x', 'speed = 60x', 'speed 60x', or 'set speed to 0.5x'.";
				return false;
			}

			string valueText = match.Groups["value"].Value.Replace(',', '.');

			if (!float.TryParse(
					valueText,
					NumberStyles.Float,
					CultureInfo.InvariantCulture,
					out speed))
			{
				error = $"Invalid time speed value: {valueText}";
				return false;
			}

			if (float.IsNaN(speed) || float.IsInfinity(speed))
			{
				error = "Time speed must be a finite number.";
				return false;
			}

			if (speed < 0f)
			{
				error = "Time speed cannot be negative.";
				return false;
			}

			return true;
		}

		public static bool TryParseSpeed(
			string code,
			out float speed,
			out string error)
		{
			speed = 1f;
			error = string.Empty;

			if (string.IsNullOrWhiteSpace(code))
			{
				error = "Time module code is empty.";
				return false;
			}

			if (!IsTimeModule(code))
			{
				error = "This code is not a Time module.";
				return false;
			}

			string[] lines = code.Split(
				new[] { "\r\n", "\n", "\r" },
				StringSplitOptions.None
			);

			foreach (string rawLine in lines)
			{
				string line = RemoveComment(rawLine).Trim();

				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if (!IsTimeSpeedCandidateLine(line))
				{
					continue;
				}

				if (!TryParseSpeedLine(line, out speed, out error))
				{
					return false;
				}

				return true;
			}

			error = "Time module has no speed line. Example: time = 60x";
			return false;
		}

		public static string FormatSpeed(float speed)
		{
			return speed.ToString("0.###", CultureInfo.InvariantCulture);
		}

		private static string RemoveComment(string line)
		{
			if (string.IsNullOrEmpty(line))
			{
				return string.Empty;
			}

			int commentIndex = line.IndexOf("//", StringComparison.Ordinal);

			return commentIndex >= 0
				? line[..commentIndex]
				: line;
		}
	}
}
