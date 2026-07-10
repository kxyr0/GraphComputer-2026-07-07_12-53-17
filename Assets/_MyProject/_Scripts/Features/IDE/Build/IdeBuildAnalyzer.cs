using System;
using System.Collections.Generic;
using Game.IDE.Runtime;

namespace Game.IDE.Presentation
{
	public sealed class IdeBuildReport
	{
		public string ModuleName = "UnknownModule";
		public ModuleBuildSettings ModuleSettings = ModuleBuildSettings.Balanced();

		public readonly List<string> Info = new List<string>();
		public readonly List<string> Warnings = new List<string>();
		public readonly List<string> Errors = new List<string>();
		public readonly List<string> Effects = new List<string>();

		public int StabilityDelta;
		public int SystemLoadDelta;
		public int TechnicalDebtDelta;

		public bool IsSuccessful => Errors == null || Errors.Count == 0;
	}

	public static class IdeBuildAnalyzer
	{
		public static IdeBuildReport Analyze(string code)
		{
			IdeBuildReport report = new IdeBuildReport();

			string[] lines = NormalizeLines(code);
			string moduleName = FindModuleName(lines);

			report.ModuleName = string.IsNullOrWhiteSpace(moduleName)
				? "UnknownModule"
				: moduleName;

			IdeModuleBuildProfile profile = IdeModuleRuleSet.GetProfile(moduleName);
			report.ModuleSettings = ModuleBuildSettings.Parse(lines, report.Warnings);

			if (string.IsNullOrWhiteSpace(moduleName))
			{
				report.Errors.Add("Missing module declaration. Example: module SystemCore");
			}
			else
			{
				report.Info.Add($"Module detected: {moduleName}");
			}

			report.Info.Add(
				report.ModuleSettings.HasExplicitMode
					? $"Mode detected: {ModuleOperatingModeUtility.ToDisplayName(report.ModuleSettings.Mode)}"
					: $"Mode defaulted: {ModuleOperatingModeUtility.ToDisplayName(report.ModuleSettings.Mode)}");

			if (report.ModuleSettings.HasScanTraffic)
			{
				report.Info.Add($"scanTraffic: {FormatBool(report.ModuleSettings.ScanTraffic)}");
			}

			if (report.ModuleSettings.HasAllowUnknownPorts)
			{
				report.Info.Add($"allowUnknownPorts: {FormatBool(report.ModuleSettings.AllowUnknownPorts)}");
			}

			if (report.ModuleSettings.HasPlatform)
			{
				report.Info.Add($"platform: {report.ModuleSettings.Platform}");
			}

			AnalyzeStructure(lines, report);
			AnalyzeArchitecture(lines, report, profile);
			AnalyzeModuleSpecificSyntax(lines, report, profile);
			AnalyzeUnknownStatements(lines, report, profile);
			AnalyzeEffects(lines, report);

			return report;
		}

		private static void AnalyzeStructure(string[] lines, IdeBuildReport report)
		{
			int ifCount = 0;
			int endCount = 0;
			int openedBraceCount = 0;
			int closedBraceCount = 0;

			foreach (string rawLine in lines)
			{
				string line = CleanLine(rawLine);

				if (line.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
				{
					ifCount++;
				}

				if (string.Equals(line, "end", StringComparison.OrdinalIgnoreCase))
				{
					endCount++;
				}

				for (int i = 0; i < line.Length; i++)
				{
					if (line[i] == '{')
					{
						openedBraceCount++;
					}
					else if (line[i] == '}')
					{
						closedBraceCount++;
					}
				}
			}

			if (endCount < ifCount)
			{
				report.Errors.Add("Control flow block is not closed. Missing 'end'.");
			}

			if (endCount > ifCount)
			{
				report.Warnings.Add("Extra 'end' detected. Build may ignore unused block endings.");
				report.TechnicalDebtDelta += 2;
			}

			if (openedBraceCount > closedBraceCount)
			{
				report.Errors.Add("Code block is not closed. Missing '}'.");
			}

			if (closedBraceCount > openedBraceCount)
			{
				report.Warnings.Add("Extra closing brace detected.");
				report.TechnicalDebtDelta += 2;
			}
		}

		private static void AnalyzeArchitecture(
			string[] lines,
			IdeBuildReport report,
			IdeModuleBuildProfile profile)
		{
			bool hasRequire = ContainsKeyword(lines, "require ");
			bool hasBudget = ContainsKeyword(lines, "budget");
			bool hasExport = ContainsKeyword(lines, "export");
			bool hasIf = ContainsKeyword(lines, "if ");
			bool hasAlert = ContainsKeyword(lines, "alert");

			if (profile.WarnMissingRequire && !hasRequire)
			{
				report.Warnings.Add("No explicit dependencies declared. Use 'require Kernel as kernel' or similar.");
				report.TechnicalDebtDelta += 4;
			}

			if (profile.WarnMissingBudget && !hasBudget)
			{
				report.Warnings.Add("No budget block found. Hardware limits are not defined.");
				report.TechnicalDebtDelta += 3;
			}

			if (profile.WarnMissingExport && !hasExport)
			{
				report.Warnings.Add("No export block found. Runtime metrics are inferred automatically.");
				report.TechnicalDebtDelta += 3;
			}

			if (profile.WarnMissingConditionalLogic && !hasIf)
			{
				report.Warnings.Add("No conditional logic found. Module cannot react to runtime pressure.");
				report.StabilityDelta -= 2;
			}

			if (profile.WarnMissingAlertPath && !hasAlert)
			{
				report.Warnings.Add("No alert path found. User-facing failures may be silent.");
				report.TechnicalDebtDelta += 2;
			}
		}

		private static void AnalyzeModuleSpecificSyntax(
			string[] lines,
			IdeBuildReport report,
			IdeModuleBuildProfile profile)
		{
			if (!profile.AllowTimeSpeedStatements)
			{
				return;
			}

			bool speedReported = false;

			for (int i = 0; i < lines.Length; i++)
			{
				string line = CleanLine(lines[i]);

				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if (!TimeModuleSpeedParser.IsTimeSpeedCandidateLine(line))
				{
					continue;
				}

				if (!TimeModuleSpeedParser.TryParseSpeedLine(
						line,
						out float speed,
						out string error))
				{
					report.Errors.Add($"Invalid Time speed at line {i + 1}: {error}");
					continue;
				}

				if (speedReported)
				{
					continue;
				}

				string speedText = TimeModuleSpeedParser.FormatSpeed(speed);

				report.Info.Add($"Time speed detected: {speedText}x");
				report.Effects.Add($"Game Time Speed {speedText}x");
				speedReported = true;
			}
		}

		private static void AnalyzeEffects(string[] lines, IdeBuildReport report)
		{
			foreach (string rawLine in lines)
			{
				string line = CleanLine(rawLine);

				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if (IsDirectMetricMutation(line))
				{
					report.Warnings.Add($"Direct metric mutation detected: '{line}'. Prefer export/calculated metrics.");
					report.StabilityDelta += 1;
					report.TechnicalDebtDelta += 5;
				}

				if (Contains(line, "throttle"))
				{
					report.SystemLoadDelta -= 2;
					report.StabilityDelta += 1;
				}

				if (Contains(line, "cache"))
				{
					report.SystemLoadDelta -= 1;
					report.TechnicalDebtDelta += 1;
				}

				if (Contains(line, "monitor") || Contains(line, "watch"))
				{
					report.StabilityDelta += 1;
					report.SystemLoadDelta += 1;
				}

				if (Contains(line, "bind") || Contains(line, "mount") || Contains(line, "route"))
				{
					report.StabilityDelta += 1;
				}

				if (Contains(line, "index"))
				{
					report.SystemLoadDelta += 1;
					report.TechnicalDebtDelta += 1;
				}

				if (Contains(line, "recovery"))
				{
					report.StabilityDelta += 3;
					report.SystemLoadDelta += 1;
				}

				if (Contains(line, "recover"))
				{
					report.StabilityDelta += 3;
					report.SystemLoadDelta += 1;
				}

				if (Contains(line, "scan"))
				{
					report.SystemLoadDelta += 2;
				}

				if (Contains(line, "protect") || Contains(line, "encrypt"))
				{
					report.StabilityDelta += 2;
					report.SystemLoadDelta += 1;
				}

				if (Contains(line, "backup"))
				{
					report.StabilityDelta += 2;
					report.SystemLoadDelta += 1;
					report.TechnicalDebtDelta += 1;
				}

				if (Contains(line, "allow") || Contains(line, "block"))
				{
					report.StabilityDelta += 1;
					report.SystemLoadDelta += 1;
				}

				if (Contains(line, "flush") || Contains(line, "reduce"))
				{
					report.SystemLoadDelta -= 1;
					report.TechnicalDebtDelta += 1;
				}

				if (Contains(line, "every tick"))
				{
					report.Warnings.Add("High-frequency operation detected: 'every tick'. This may overload older hardware.");
					report.SystemLoadDelta += 5;
					report.TechnicalDebtDelta += 3;
				}

				if (Contains(line, "every 1s"))
				{
					report.SystemLoadDelta += 1;
				}
			}
		}

		private static bool IsDirectMetricMutation(string line)
		{
			if (!line.Contains("+="))
			{
				return false;
			}

			return
				line.StartsWith("stability", StringComparison.OrdinalIgnoreCase) ||
				line.StartsWith("network", StringComparison.OrdinalIgnoreCase) ||
				line.StartsWith("security", StringComparison.OrdinalIgnoreCase) ||
				line.StartsWith("load", StringComparison.OrdinalIgnoreCase) ||
				line.StartsWith("memory", StringComparison.OrdinalIgnoreCase) ||
				line.StartsWith("storage", StringComparison.OrdinalIgnoreCase);
		}

		private static string FindModuleName(string[] lines)
		{
			foreach (string rawLine in lines)
			{
				string line = CleanLine(rawLine);

				if (!line.StartsWith("module ", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				string[] parts = line.Split(
					new[] { ' ', '\t' },
					StringSplitOptions.RemoveEmptyEntries
				);

				if (parts.Length >= 2)
				{
					return parts[1].Trim();
				}
			}

			return string.Empty;
		}

		private static bool ContainsKeyword(string[] lines, string keyword)
		{
			foreach (string rawLine in lines)
			{
				string line = CleanLine(rawLine);

				if (Contains(line, keyword))
				{
					return true;
				}
			}

			return false;
		}

		private static bool Contains(string source, string value)
		{
			return source != null &&
				   source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
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

		private static string[] NormalizeLines(string code)
		{
			return (code ?? string.Empty)
				.Replace("\r\n", "\n")
				.Replace('\r', '\n')
				.Split('\n');
		}

		private static string FormatBool(bool value)
		{
			return value ? "true" : "false";
		}

		private static void AnalyzeUnknownStatements(
			string[] lines,
			IdeBuildReport report,
			IdeModuleBuildProfile profile)
		{
			for (int i = 0; i < lines.Length; i++)
			{
				string line = CleanLine(lines[i]);

				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if (profile.AllowTimeSpeedStatements &&
					TimeModuleSpeedParser.IsTimeSpeedCandidateLine(line))
				{
					continue;
				}

				if (IsKnownStatement(line, profile))
				{
					continue;
				}

				report.Errors.Add($"Unknown statement at line {i + 1}: '{line}'.");
			}
		}

		private static bool IsKnownStatement(
			string line,
			IdeModuleBuildProfile profile)
		{
			string lower = line.ToLowerInvariant();

			if (lower == "end" || lower == "}" || lower.EndsWith("{"))
			{
				return true;
			}

			if (lower.StartsWith("module "))
			{
				return true;
			}

			if (lower.StartsWith("require "))
			{
				return true;
			}

			if (lower.StartsWith("optional "))
			{
				return true;
			}

			if (IsKnownSettingAssignment(lower, "mode") ||
				IsKnownSettingAssignment(lower, "scantraffic") ||
				IsKnownSettingAssignment(lower, "allowunknownports") ||
				IsKnownSettingAssignment(lower, "platform"))
			{
				return true;
			}

			if (lower.StartsWith("if "))
			{
				return true;
			}

			if (lower.StartsWith("alert "))
			{
				return true;
			}

			if (lower.StartsWith("warning "))
			{
				return true;
			}

			if (lower.StartsWith("start "))
			{
				return true;
			}

			if (lower.StartsWith("mount "))
			{
				return true;
			}

			if (lower.StartsWith("load "))
			{
				return true;
			}

			if (lower.StartsWith("scan "))
			{
				return true;
			}

			if (lower.StartsWith("rule "))
			{
				return true;
			}

			if (lower.StartsWith("allow "))
			{
				return true;
			}

			if (lower.StartsWith("block "))
			{
				return true;
			}

			if (lower.StartsWith("protect "))
			{
				return true;
			}

			if (lower.StartsWith("encrypt "))
			{
				return true;
			}

			if (lower.StartsWith("backup "))
			{
				return true;
			}

			if (lower.StartsWith("cache "))
			{
				return true;
			}

			if (lower.StartsWith("index "))
			{
				return true;
			}

			if (lower.StartsWith("watch "))
			{
				return true;
			}

			if (lower.StartsWith("monitor "))
			{
				return true;
			}

			if (lower.StartsWith("route "))
			{
				return true;
			}

			if (lower.StartsWith("recover "))
			{
				return true;
			}

			if (lower.StartsWith("schedule "))
			{
				return true;
			}

			if (lower.StartsWith("bind "))
			{
				return true;
			}

			if (lower.StartsWith("throttle "))
			{
				return true;
			}

			if (lower.StartsWith("flush "))
			{
				return true;
			}

			if (lower.StartsWith("reduce "))
			{
				return true;
			}

			if (lower.StartsWith("enable "))
			{
				return true;
			}

			if (profile.AllowGenericAssignments &&
				(lower.Contains("=") || lower.Contains("<=") || lower.Contains(">=")))
			{
				return true;
			}

			return false;
		}

		private static bool IsKnownSettingAssignment(string line, string key)
		{
			if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(key))
			{
				return false;
			}

			if (!line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (line.Length > key.Length &&
				(char.IsLetterOrDigit(line[key.Length]) || line[key.Length] == '_'))
			{
				return false;
			}

			return line.Contains("=") || line.Contains(":");
		}
	}

	internal sealed class IdeModuleBuildProfile
	{
		public bool WarnMissingRequire { get; }
		public bool WarnMissingBudget { get; }
		public bool WarnMissingExport { get; }
		public bool WarnMissingConditionalLogic { get; }
		public bool WarnMissingAlertPath { get; }
		public bool AllowGenericAssignments { get; }
		public bool AllowTimeSpeedStatements { get; }

		private IdeModuleBuildProfile(
			bool warnMissingRequire,
			bool warnMissingBudget,
			bool warnMissingExport,
			bool warnMissingConditionalLogic,
			bool warnMissingAlertPath,
			bool allowGenericAssignments,
			bool allowTimeSpeedStatements)
		{
			WarnMissingRequire = warnMissingRequire;
			WarnMissingBudget = warnMissingBudget;
			WarnMissingExport = warnMissingExport;
			WarnMissingConditionalLogic = warnMissingConditionalLogic;
			WarnMissingAlertPath = warnMissingAlertPath;
			AllowGenericAssignments = allowGenericAssignments;
			AllowTimeSpeedStatements = allowTimeSpeedStatements;
		}

		public static IdeModuleBuildProfile Time()
		{
			return new IdeModuleBuildProfile(
				warnMissingRequire: false,
				warnMissingBudget: false,
				warnMissingExport: false,
				warnMissingConditionalLogic: false,
				warnMissingAlertPath: false,
				allowGenericAssignments: false,
				allowTimeSpeedStatements: true
			);
		}

		public static IdeModuleBuildProfile DefaultOs()
		{
			return new IdeModuleBuildProfile(
				warnMissingRequire: true,
				warnMissingBudget: true,
				warnMissingExport: true,
				warnMissingConditionalLogic: true,
				warnMissingAlertPath: true,
				allowGenericAssignments: true,
				allowTimeSpeedStatements: false
			);
		}

		public static IdeModuleBuildProfile PassiveUtility()
		{
			return new IdeModuleBuildProfile(
				warnMissingRequire: false,
				warnMissingBudget: false,
				warnMissingExport: false,
				warnMissingConditionalLogic: false,
				warnMissingAlertPath: false,
				allowGenericAssignments: true,
				allowTimeSpeedStatements: false
			);
		}
	}

	internal static class IdeModuleRuleSet
	{
		private static readonly HashSet<string> OsModules = new(
			StringComparer.OrdinalIgnoreCase)
		{
			"SystemCore",
			"Kernel",
			"Data",
			"Security",
			"Control"
		};

		public static IdeModuleBuildProfile GetProfile(string moduleName)
		{
			if (string.Equals(moduleName, "Time", StringComparison.OrdinalIgnoreCase))
			{
				return IdeModuleBuildProfile.Time();
			}

			if (OsModules.Contains(moduleName ?? string.Empty))
			{
				return IdeModuleBuildProfile.DefaultOs();
			}

			return IdeModuleBuildProfile.PassiveUtility();
		}
	}
}
