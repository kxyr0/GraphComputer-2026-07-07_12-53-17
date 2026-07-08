using System;
using System.Collections.Generic;
using Game.IDE.Runtime;
namespace Game.IDE.Presentation
{
	public sealed class IdeBuildReport
	{
		public string ModuleName = "UnknownModule";

		public readonly List<string> Info = new List<string>();
		public readonly List<string> Warnings = new List<string>();
		public readonly List<string> Errors = new List<string>();

		public int StabilityDelta;
		public int SystemLoadDelta;
		public int TechnicalDebtDelta;
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

			if (string.IsNullOrWhiteSpace(moduleName))
			{
				report.Errors.Add("Missing module declaration. Example: module SystemCore");
			}
			else
			{
				report.Info.Add($"Module detected: {moduleName}");
			}

			AnalyzeStructure(lines, report);
			AnalyzeArchitecture(lines, report);
			AnalyzeUnknownStatements(lines, report);
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

		private static void AnalyzeArchitecture(string[] lines, IdeBuildReport report)
		{
			bool hasRequire = ContainsKeyword(lines, "require ");
			bool hasBudget = ContainsKeyword(lines, "budget");
			bool hasExport = ContainsKeyword(lines, "export");
			bool hasIf = ContainsKeyword(lines, "if ");
			bool hasAlert = ContainsKeyword(lines, "alert");

			if (!hasRequire)
			{
				report.Warnings.Add("No explicit dependencies declared. Use 'require Kernel as kernel' or similar.");
				report.TechnicalDebtDelta += 4;
			}

			if (!hasBudget)
			{
				report.Warnings.Add("No budget block found. Hardware limits are not defined.");
				report.TechnicalDebtDelta += 3;
			}

			if (!hasExport)
			{
				report.Warnings.Add("No export block found. Runtime metrics are inferred automatically.");
				report.TechnicalDebtDelta += 3;
			}

			if (!hasIf)
			{
				report.Warnings.Add("No conditional logic found. Module cannot react to runtime pressure.");
				report.StabilityDelta -= 2;
			}

			if (!hasAlert)
			{
				report.Warnings.Add("No alert path found. User-facing failures may be silent.");
				report.TechnicalDebtDelta += 2;
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

				if (Contains(line, "recovery"))
				{
					report.StabilityDelta += 3;
					report.SystemLoadDelta += 1;
				}

				if (Contains(line, "scan"))
				{
					report.SystemLoadDelta += 2;
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
		private static void AnalyzeUnknownStatements(string[] lines, IdeBuildReport report)
		{
			for (int i = 0; i < lines.Length; i++)
			{
				string line = CleanLine(lines[i]);
				if (TimeModuleSpeedParser.IsTimeSpeedLine(line))
				{
					continue;
				}
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if (IsKnownStatement(line))
				{
					continue;
				}

				report.Errors.Add($"Unknown statement at line {i + 1}: '{line}'.");
			}
		}

		private static bool IsKnownStatement(string line)
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

			if (lower.Contains("=") || lower.Contains("<=") || lower.Contains(">="))
			{
				return true;
			}

			return false;
		}
	}
}