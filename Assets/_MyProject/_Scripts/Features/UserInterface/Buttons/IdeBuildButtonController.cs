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

		[Header("Tabs")]
		[SerializeField] private IdeBottomTabsController bottomTabsController;
		[SerializeField] private IdeCodeTabsController codeTabsController;

		[Header("Code State")]
		[SerializeField] private NodeCodeRegistry codeRegistry;

		[Header("Module Effects")]
		[SerializeField] private TimeModuleSpeedApplier timeModuleSpeedApplier;

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

			if (codeRegistry == null)
			{
				codeRegistry = FindFirstObjectByType<NodeCodeRegistry>(FindObjectsInactive.Include);
			}

			if (codeTabsController == null)
			{
				codeTabsController = FindFirstObjectByType<IdeCodeTabsController>(FindObjectsInactive.Include);
			}

			if (bottomTabsController == null)
			{
				bottomTabsController = FindFirstObjectByType<IdeBottomTabsController>(FindObjectsInactive.Include);
			}

			if (timeModuleSpeedApplier == null)
			{
				timeModuleSpeedApplier = FindFirstObjectByType<TimeModuleSpeedApplier>(FindObjectsInactive.Include);
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

			if (bottomTabsController != null)
			{
				bottomTabsController.SelectTabById(outputTabId);
			}

			if (clearOutputBeforeBuild)
			{
				outputConsole.Clear();
			}

			if (codeTabsController != null)
			{
				codeTabsController.SaveActiveDocument();
			}

			string code = codeEditor.GetCode();
			IdeBuildReport report = IdeBuildAnalyzer.Analyze(code);

			string documentId = codeTabsController != null
				? codeTabsController.ActiveDocumentId
				: "unknown_document";

			if (codeRegistry == null)
			{
				codeRegistry = NodeCodeRegistry.Instance;
			}

			if (codeRegistry == null)
			{
				codeRegistry = FindFirstObjectByType<NodeCodeRegistry>(FindObjectsInactive.Include);
			}

			if (codeRegistry != null)
			{
				codeRegistry.SaveBuildResult(
					documentId,
					code,
					report
				);
			}

			bool buildSucceeded = report.Errors == null || report.Errors.Count == 0;

			if (timeModuleSpeedApplier == null)
			{
				timeModuleSpeedApplier = FindFirstObjectByType<TimeModuleSpeedApplier>(FindObjectsInactive.Include);
			}

			if (timeModuleSpeedApplier != null)
			{
				timeModuleSpeedApplier.TryApply(
					documentId,
					code,
					buildSucceeded
				);
			}

			WriteReport(report);
		}

		private void WriteReport(IdeBuildReport report)
		{
			outputConsole.BeginWriteBatch();

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

				outputConsole.EndWriteBatch(scrollToBottom: true);
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

			outputConsole.EndWriteBatch(scrollToBottom: true);
		}

		private static string FormatSigned(int value)
		{
			return value >= 0 ? $"+{value}" : value.ToString();
		}
	}
}