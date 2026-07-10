using System;
using System.Collections.Generic;

namespace Game.IDE.Runtime
{
	public readonly struct ModuleBuildSettings
	{
		public readonly ModuleOperatingMode Mode;
		public readonly bool HasExplicitMode;
		public readonly bool ScanTraffic;
		public readonly bool HasScanTraffic;
		public readonly bool AllowUnknownPorts;
		public readonly bool HasAllowUnknownPorts;
		public readonly string Platform;
		public readonly bool HasPlatform;

		public ModuleBuildSettings(
			ModuleOperatingMode mode,
			bool hasExplicitMode,
			bool scanTraffic,
			bool hasScanTraffic,
			bool allowUnknownPorts,
			bool hasAllowUnknownPorts,
			string platform,
			bool hasPlatform)
		{
			Mode = mode;
			HasExplicitMode = hasExplicitMode;
			ScanTraffic = scanTraffic;
			HasScanTraffic = hasScanTraffic;
			AllowUnknownPorts = allowUnknownPorts;
			HasAllowUnknownPorts = hasAllowUnknownPorts;
			Platform = platform ?? string.Empty;
			HasPlatform = hasPlatform;
		}

		public static ModuleBuildSettings Balanced()
		{
			return new ModuleBuildSettings(
				ModuleOperatingMode.Balanced,
				hasExplicitMode: false,
				scanTraffic: false,
				hasScanTraffic: false,
				allowUnknownPorts: false,
				hasAllowUnknownPorts: false,
				platform: string.Empty,
				hasPlatform: false
			);
		}

		public static ModuleBuildSettings Parse(
			IEnumerable<string> lines,
			ICollection<string> warnings)
		{
			ModuleOperatingMode mode = ModuleOperatingMode.Balanced;
			bool hasExplicitMode = false;
			bool scanTraffic = false;
			bool hasScanTraffic = false;
			bool allowUnknownPorts = false;
			bool hasAllowUnknownPorts = false;
			string platform = string.Empty;
			bool hasPlatform = false;

			if (lines == null)
			{
				return Balanced();
			}

			foreach (string rawLine in lines)
			{
				string line = CleanLine(rawLine);

				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if (TryReadAssignment(line, "mode", out string modeText))
				{
					hasExplicitMode = true;

					if (ModuleOperatingModeUtility.TryParse(modeText, out ModuleOperatingMode parsedMode))
					{
						mode = parsedMode;
					}
					else
					{
						warnings?.Add(
							$"Unknown module mode '{modeText}'. Falling back to balanced mode.");
						mode = ModuleOperatingMode.Balanced;
					}

					continue;
				}

				if (TryReadBoolAssignment(line, "scanTraffic", out bool parsedScanTraffic))
				{
					scanTraffic = parsedScanTraffic;
					hasScanTraffic = true;
					continue;
				}

				if (TryReadBoolAssignment(line, "allowUnknownPorts", out bool parsedAllowUnknownPorts))
				{
					allowUnknownPorts = parsedAllowUnknownPorts;
					hasAllowUnknownPorts = true;
					continue;
				}

				if (TryReadAssignment(line, "platform", out string platformText))
				{
					platform = NormalizePlatform(platformText);
					hasPlatform = !string.IsNullOrWhiteSpace(platform);
				}
			}

			return new ModuleBuildSettings(
				mode,
				hasExplicitMode,
				scanTraffic,
				hasScanTraffic,
				allowUnknownPorts,
				hasAllowUnknownPorts,
				platform,
				hasPlatform
			);
		}

		private static string NormalizePlatform(string value)
		{
			string normalized = NormalizeValue(value);

			if (string.IsNullOrWhiteSpace(normalized))
			{
				return string.Empty;
			}

			if (string.Equals(normalized, "pc", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(normalized, "windows", StringComparison.OrdinalIgnoreCase))
			{
				return "PC";
			}

			return normalized.Trim();
		}

		private static bool TryReadBoolAssignment(
			string line,
			string key,
			out bool value)
		{
			value = false;

			if (!TryReadAssignment(line, key, out string text))
			{
				return false;
			}

			switch (NormalizeValue(text))
			{
				case "true":
				case "yes":
				case "on":
				case "1":
					value = true;
					return true;

				case "false":
				case "no":
				case "off":
				case "0":
					value = false;
					return true;

				default:
					return false;
			}
		}

		private static bool TryReadAssignment(
			string line,
			string key,
			out string value)
		{
			value = string.Empty;

			if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(key))
			{
				return false;
			}

			string trimmed = line.Trim();

			if (!trimmed.StartsWith(key, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (trimmed.Length > key.Length &&
				(char.IsLetterOrDigit(trimmed[key.Length]) || trimmed[key.Length] == '_'))
			{
				return false;
			}

			int separatorIndex = trimmed.IndexOf('=');

			if (separatorIndex < 0)
			{
				separatorIndex = trimmed.IndexOf(':');
			}

			if (separatorIndex < 0 || separatorIndex >= trimmed.Length - 1)
			{
				return false;
			}

			value = NormalizeValue(trimmed.Substring(separatorIndex + 1));
			return !string.IsNullOrWhiteSpace(value);
		}

		private static string NormalizeValue(string value)
		{
			return (value ?? string.Empty)
				.Trim()
				.TrimEnd(',')
				.TrimEnd(';')
				.Trim()
				.Trim('"')
				.Trim('\'');
		}

		private static string CleanLine(string line)
		{
			if (line == null)
			{
				return string.Empty;
			}

			int commentIndex = line.IndexOf("//", StringComparison.Ordinal);

			if (commentIndex >= 0)
			{
				line = line.Substring(0, commentIndex);
			}

			return line.Trim();
		}
	}
}
