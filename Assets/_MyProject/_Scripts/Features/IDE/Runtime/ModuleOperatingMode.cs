using System;

namespace Game.IDE.Runtime
{
	public enum ModuleOperatingMode
	{
		Balanced,
		Strict,
		Permissive,
		Aggressive,
		Safe,
		Performance
	}

	public static class ModuleOperatingModeUtility
	{
		public static ModuleOperatingMode ParseOrDefault(
			string value,
			ModuleOperatingMode fallback = ModuleOperatingMode.Balanced)
		{
			return TryParse(value, out ModuleOperatingMode mode)
				? mode
				: fallback;
		}

		public static bool TryParse(string value, out ModuleOperatingMode mode)
		{
			mode = ModuleOperatingMode.Balanced;

			string normalized = Normalize(value);

			if (string.IsNullOrWhiteSpace(normalized))
			{
				return false;
			}

			switch (normalized)
			{
				case "balanced":
				case "balance":
				case "normal":
					mode = ModuleOperatingMode.Balanced;
					return true;

				case "strict":
				case "secure":
				case "security":
					mode = ModuleOperatingMode.Strict;
					return true;

				case "permissive":
				case "open":
				case "relaxed":
					mode = ModuleOperatingMode.Permissive;
					return true;

				case "aggressive":
				case "fast":
					mode = ModuleOperatingMode.Aggressive;
					return true;

				case "safe":
				case "stable":
				case "conservative":
					mode = ModuleOperatingMode.Safe;
					return true;

				case "performance":
				case "speed":
				case "throughput":
					mode = ModuleOperatingMode.Performance;
					return true;

				default:
					return false;
			}
		}

		public static string ToCodeValue(ModuleOperatingMode mode)
		{
			return mode switch
			{
				ModuleOperatingMode.Strict => "strict",
				ModuleOperatingMode.Permissive => "permissive",
				ModuleOperatingMode.Aggressive => "aggressive",
				ModuleOperatingMode.Safe => "safe",
				ModuleOperatingMode.Performance => "performance",
				_ => "balanced"
			};
		}

		public static string ToDisplayName(ModuleOperatingMode mode)
		{
			return mode switch
			{
				ModuleOperatingMode.Strict => "Strict",
				ModuleOperatingMode.Permissive => "Permissive",
				ModuleOperatingMode.Aggressive => "Aggressive",
				ModuleOperatingMode.Safe => "Safe",
				ModuleOperatingMode.Performance => "Performance",
				_ => "Balanced"
			};
		}

		private static string Normalize(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			return value
				.Trim()
				.Trim('"')
				.Trim('\'')
				.Replace("-", string.Empty)
				.Replace("_", string.Empty)
				.ToLowerInvariant();
		}
	}
}
