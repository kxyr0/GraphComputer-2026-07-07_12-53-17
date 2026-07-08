using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeBuildButtonController : MonoBehaviour
	{
		[SerializeField] private Button buildButton;

		[Header("IDE")]
		[SerializeField] private IdeInputCodeEditor codeEditor;
		[SerializeField] private IdeOutputConsoleView outputConsole;
		[SerializeField] private IdeBottomTabsController tabsController;

		[Header("Output")]
		[SerializeField] private string outputTabId = "output";
		[SerializeField] private bool clearOutputBeforeBuild = true;

		private void Reset()
		{
			buildButton = GetComponent<Button>();
		}

		private void Awake()
		{
			if (buildButton == null)
			{
				buildButton = GetComponent<Button>();
			}
		}

		private void OnEnable()
		{
			if (buildButton != null)
			{
				buildButton.onClick.AddListener(BuildCurrentCode);
			}
		}

		private void OnDisable()
		{
			if (buildButton != null)
			{
				buildButton.onClick.RemoveListener(BuildCurrentCode);
			}
		}

		public void BuildCurrentCode()
		{
			if (codeEditor == null)
			{
				Debug.LogError($"[{nameof(IdeBuildButtonController)}] Code editor is missing.", this);
				return;
			}

			if (outputConsole == null)
			{
				Debug.LogError($"[{nameof(IdeBuildButtonController)}] Output console is missing.", this);
				return;
			}

			if (tabsController != null)
			{
				tabsController.SelectTabById(outputTabId);
			}

			if (clearOutputBeforeBuild)
			{
				outputConsole.Clear();
			}

			string code = codeEditor.GetCode();
			IdeBuildReport report = IdeBuildAnalyzer.Analyze(code);

			WriteReport(report);
		}

		private void WriteReport(IdeBuildReport report)
		{
			outputConsole.WriteSystem($"[Build] {report.ModuleName}.os");

			foreach (string message in report.Info)
			{
				outputConsole.WriteInfo(message);
			}

			foreach (string warning in report.Warnings)
			{
				outputConsole.WriteWarning($"[WARN] {warning}");
			}

			foreach (string error in report.Errors)
			{
				outputConsole.WriteError($"[ERROR] {error}");
			}

			if (report.Errors.Count > 0)
			{
				outputConsole.WriteError(
					$"[Build] Failed with {report.Errors.Count} error(s), {report.Warnings.Count} warning(s)."
				);

				return;
			}

			if (report.Warnings.Count > 0)
			{
				outputConsole.WriteWarning(
					$"[Build] Completed with {report.Warnings.Count} warning(s)."
				);
			}
			else
			{
				outputConsole.WriteSuccess("[Build] Completed successfully.");
			}

			outputConsole.WriteInfo("Effects:");
			outputConsole.WriteInfo($"  Stability Delta      {FormatSigned(report.StabilityDelta)}");
			outputConsole.WriteInfo($"  System Load Delta    {FormatSigned(report.SystemLoadDelta)}");
			outputConsole.WriteInfo($"  Technical Debt Delta {FormatSigned(report.TechnicalDebtDelta)}");
		}

		private static string FormatSigned(int value)
		{
			return value >= 0 ? $"+{value}" : value.ToString();
		}
	}

	public sealed class IdeBuildReport
	{
		public string ModuleName = "UnknownModule";

		public readonly List<string> Info = new();
		public readonly List<string> Warnings = new();
		public readonly List<string> Errors = new();

		public int StabilityDelta;
		public int SystemLoadDelta;
		public int TechnicalDebtDelta;
	}

	public static class IdeBuildAnalyzer
	{
		public static IdeBuildReport Analyze(string code)
		{
			IdeBuildReport report = new();

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
			AnalyzeEffects(lines, report);

			return report;
		}

		private static void AnalyzeStructure(string[] lines, IdeBuildReport report)
		{
			int ifCount = 0;
			int endCount = 0;

			for (int i = 0; i < lines.Length; i++)
			{
				string line = CleanLine(lines[i]);

				if (line.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
				{
					ifCount++;
				}

				if (string.Equals(line, "end", StringComparison.OrdinalIgnoreCase))
				{
					endCount++;
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

				if (line.Contains("throttle", StringComparison.OrdinalIgnoreCase))
				{
					report.SystemLoadDelta -= 2;
					report.StabilityDelta += 1;
				}

				if (line.Contains("cache", StringComparison.OrdinalIgnoreCase))
				{
					report.SystemLoadDelta -= 1;
					report.TechnicalDebtDelta += 1;
				}

				if (line.Contains("recovery", StringComparison.OrdinalIgnoreCase))
				{
					report.StabilityDelta += 3;
					report.SystemLoadDelta += 1;
				}

				if (line.Contains("scan", StringComparison.OrdinalIgnoreCase))
				{
					report.SystemLoadDelta += 2;
				}

				if (line.Contains("every tick", StringComparison.OrdinalIgnoreCase))
				{
					report.Warnings.Add("High-frequency operation detected: 'every tick'. This may overload older hardware.");
					report.SystemLoadDelta += 5;
					report.TechnicalDebtDelta += 3;
				}
			}
		}

		private static bool IsDirectMetricMutation(string line)
		{
			return
				line.Contains("+=", StringComparison.Ordinal) &&
				(
					line.StartsWith("stability", StringComparison.OrdinalIgnoreCase) ||
					line.StartsWith("network", StringComparison.OrdinalIgnoreCase) ||
					line.StartsWith("security", StringComparison.OrdinalIgnoreCase) ||
					line.StartsWith("load", StringComparison.OrdinalIgnoreCase) ||
					line.StartsWith("memory", StringComparison.OrdinalIgnoreCase) ||
					line.StartsWith("storage", StringComparison.OrdinalIgnoreCase)
				);
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
					' ',
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

				if (line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
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
				line = line[..commentIndex];
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
	}
}