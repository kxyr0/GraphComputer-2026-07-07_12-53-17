using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeBuildButtonController : MonoBehaviour
	{
		private static readonly string[] BuildPipelineSteps =
		{
			"Compiling Kernel...",
			"Linking File System...",
			"Testing Drivers...",
			"Validating Release..."
		};

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

		[Header("Build Pipeline")]
		[SerializeField] private bool simulateBuildPipeline = true;
		[SerializeField, Min(0f)] private float minBuildStepDelay = 0.45f;
		[SerializeField, Min(0f)] private float maxBuildStepDelay = 1.25f;
		[SerializeField, Range(0f, 1f)] private float transientReleaseFailureChance = 0.1f;

		private Coroutine buildCoroutine;

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

			if (buildCoroutine != null)
			{
				StopCoroutine(buildCoroutine);
				buildCoroutine = null;
				SetBuildButtonInteractable(true);
			}
		}

		public void BuildCurrentCode()
		{
			if (buildCoroutine != null)
			{
				return;
			}

			buildCoroutine = StartCoroutine(BuildCurrentCodeRoutine());
		}

		private IEnumerator BuildCurrentCodeRoutine()
		{
			if (codeEditor == null)
			{
				Debug.LogError($"[{nameof(IdeBuildButtonController)}] Code editor is missing.", this);
				buildCoroutine = null;
				yield break;
			}

			if (outputConsole == null)
			{
				Debug.LogError($"[{nameof(IdeBuildButtonController)}] Output console is missing.", this);
				buildCoroutine = null;
				yield break;
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
			string documentId = codeTabsController != null
				? codeTabsController.ActiveDocumentId
				: "unknown_document";

			SetBuildButtonInteractable(false);

			try
			{
				yield return WriteBuildPipelineSteps();
				CompleteBuild(code, documentId);
			}
			finally
			{
				SetBuildButtonInteractable(true);
				buildCoroutine = null;
			}
		}

		private IEnumerator WriteBuildPipelineSteps()
		{
			if (!simulateBuildPipeline)
			{
				yield break;
			}

			foreach (string step in BuildPipelineSteps)
			{
				outputConsole.BeginWriteBatch();
				outputConsole.WriteSystem(step);
				outputConsole.EndWriteBatch(scrollToBottom: true);

				yield return new WaitForSeconds(GetBuildStepDelay());
			}
		}

		private void CompleteBuild(string code, string documentId)
		{
			IdeBuildReport report = IdeBuildAnalyzer.Analyze(code);
			TryInjectTransientReleaseFailure(report);

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

			if (timeModuleSpeedApplier == null)
			{
				timeModuleSpeedApplier = FindFirstObjectByType<TimeModuleSpeedApplier>(FindObjectsInactive.Include);
			}

			if (timeModuleSpeedApplier != null)
			{
				timeModuleSpeedApplier.TryApply(
					documentId,
					code,
					report.IsSuccessful
				);
			}

			WriteReport(report);
		}

		private void TryInjectTransientReleaseFailure(IdeBuildReport report)
		{
			if (report == null ||
				!report.IsSuccessful ||
				transientReleaseFailureChance <= 0f ||
				Random.value > transientReleaseFailureChance)
			{
				return;
			}

			report.Errors.Add("Release validation failed: driver smoke test timed out.");
		}

		private float GetBuildStepDelay()
		{
			float minDelay = Mathf.Max(0f, minBuildStepDelay);
			float maxDelay = Mathf.Max(minDelay, maxBuildStepDelay);

			if (Mathf.Approximately(minDelay, maxDelay))
			{
				return minDelay;
			}

			return Random.Range(minDelay, maxDelay);
		}

		private void SetBuildButtonInteractable(bool isInteractable)
		{
			if (buildButton != null)
			{
				buildButton.interactable = isInteractable;
			}
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

			if (!report.IsSuccessful)
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

			if (report.Effects.Count > 0)
			{
				foreach (string effect in report.Effects)
				{
					outputConsole.WriteInfo($"  {effect}");
				}
			}
			else
			{
				outputConsole.WriteInfo($"  Stability Delta      {FormatSigned(report.StabilityDelta)}");
				outputConsole.WriteInfo($"  System Load Delta    {FormatSigned(report.SystemLoadDelta)}");
				outputConsole.WriteInfo($"  Technical Debt Delta {FormatSigned(report.TechnicalDebtDelta)}");
			}

			outputConsole.EndWriteBatch(scrollToBottom: true);
		}

		private static string FormatSigned(int value)
		{
			return value >= 0 ? $"+{value}" : value.ToString();
		}
	}
}
