using Game.IDE.Presentation;
using Game.Nodes.Runtime;
using Game.OS.Reputation;
using Game.OS.Requirements;
using Game.World.Time;
using UnityEngine;

namespace Game.OS.Release
{
	[DisallowMultipleComponent]
	public sealed class OsReleaseService : MonoBehaviour
	{
		private const string NoValue = "None";

		[SerializeField] private NodeCodeRegistry codeRegistry;
		[SerializeField] private OsRuntimeMetricsService metricsService;

		[Header("System Core")]
		[SerializeField] private string systemCoreKey = "system_core";

		[Header("Release")]
		[SerializeField] private string releaseChannel = "Alpha";
		[SerializeField] private string releaseVersion = "0.1.0";

		[Header("Growth")]
		[SerializeField] private OsReleaseGrowthProfile growthProfile;

		[Header("Module Requirements")]
		[SerializeField] private OsModuleRequirementProfile moduleRequirementProfile;
		[SerializeField] private GameTimeService timeService;

		[Header("Reputation")]
		[SerializeField] private OsPlayerReputationService reputationService;
		[SerializeField] private bool usePlayerReputation = true;

		[Header("Debug")]
		[SerializeField] private bool logDeployResult = true;

		private bool isPublished;
		private string supportedDevices = NoValue;
		private int activeInstallations;
		private float userRating;
		private string latestRelease = NoValue;
		private string updateStatus = "Not Published";
		private float growthTimer;
		private bool firstGrowthApplied;

		public OsReleaseState Current
		{
			get
			{
				float reputation = 0f;
				string reputationTier = NoValue;
				OsModuleRequirementEvaluation requirements = EvaluateModuleRequirements();

				if (usePlayerReputation)
				{
					ResolveReputationService();

					if (reputationService != null)
					{
						reputation = reputationService.CurrentReputationPercent;
						reputationTier = reputationService.CurrentTier;
					}
				}

				return new OsReleaseState(
					isPublished,
					isPublished ? releaseChannel : NoValue,
					HasSupportedDevices ? supportedDevices : NoValue,
					activeInstallations,
					userRating,
					isPublished ? latestRelease : NoValue,
					updateStatus,
					requirements.StatusText,
					requirements.MissingModulesText,
					requirements.QualityPenalty * 100f,
					reputation,
					reputationTier
				);
			}
		}

		public bool HasPublishedBuild => isPublished;
		public bool HasSupportedDevices => !string.IsNullOrWhiteSpace(supportedDevices) &&
										   supportedDevices != NoValue;
		public int ActiveInstallations => activeInstallations;

		private void Awake()
		{
			ResolveServices();
			ResetReleaseState();
		}

		private void Update()
		{
			if (!isPublished || !HasSupportedDevices)
			{
				return;
			}

			growthTimer += Time.unscaledDeltaTime;

			if (growthTimer < GetGrowthIntervalSeconds())
			{
				return;
			}

			growthTimer = 0f;
			AdvanceReleaseGrowth();
		}

		public static OsReleaseService GetOrCreate()
		{
			OsReleaseService service =
				FindAnyObjectByType<OsReleaseService>(FindObjectsInactive.Include);

			if (service != null)
			{
				return service;
			}

			GameObject serviceObject = new GameObject("OsReleaseService");
			return serviceObject.AddComponent<OsReleaseService>();
		}

		public bool DeployBuild()
		{
			ResolveServices();

			if (codeRegistry == null ||
				!codeRegistry.TryGetState(systemCoreKey, out NodeCodeState systemCoreState) ||
				systemCoreState == null ||
				!systemCoreState.LastBuildSucceeded)
			{
				ResetReleaseState();
				LogDeploy("Deploy failed: SystemCore is not built successfully.");
				return false;
			}

			isPublished = true;
			supportedDevices = ResolveSupportedDevices(systemCoreState);
			activeInstallations = 0;
			userRating = 0f;
			latestRelease = releaseVersion;
			updateStatus = HasSupportedDevices ? "Published" : "No Devices";
			growthTimer = 0f;
			firstGrowthApplied = false;

			LogDeploy(
				$"Deploy published. Version={latestRelease}, Devices={supportedDevices}.");

			return true;
		}

		public void ResetReleaseState()
		{
			isPublished = false;
			supportedDevices = NoValue;
			activeInstallations = 0;
			userRating = 0f;
			latestRelease = NoValue;
			updateStatus = "Not Published";
			growthTimer = 0f;
			firstGrowthApplied = false;
		}

		private void AdvanceReleaseGrowth()
		{
			if (growthProfile == null)
			{
				updateStatus = "Missing Growth Profile";
				return;
			}

			OsModuleRequirementEvaluation requirements = EvaluateModuleRequirements();
			float quality = CalculateReleaseQuality(requirements);
			OsReleaseGrowthResult growth = growthProfile.Evaluate(
				activeInstallations,
				quality,
				userRating,
				allowFirstWave: !firstGrowthApplied,
				ResolveAudienceModifiers()
			);

			activeInstallations = growth.ActiveInstallations;
			userRating = growth.UserRating;
			updateStatus = requirements.HasMissingModules ? requirements.StatusText : growth.UpdateStatus;
			firstGrowthApplied = true;

			ApplyReputationFeedback(quality, growth);
		}

		private float GetGrowthIntervalSeconds()
		{
			return growthProfile != null
				? growthProfile.GrowthIntervalSeconds
				: OsReleaseGrowthProfile.DefaultGrowthIntervalSeconds;
		}

		private float CalculateReleaseQuality(OsModuleRequirementEvaluation requirements)
		{
			ResolveServices();

			if (metricsService == null)
			{
				return 0.25f;
			}

			metricsService.Recalculate();

			OsRuntimeMetrics metrics = metricsService.Current;
			float stability = metrics.StabilityPercent / 100f;
			float load = 1f - metrics.SystemLoadPercent / 100f;
			float memory = 1f - metrics.MemoryUsagePercent / 100f;
			float network = metrics.NetworkEraScorePercent / 100f;
			float baseQuality = stability * 0.4f + load * 0.25f + memory * 0.2f + network * 0.15f;

			return Mathf.Clamp01(baseQuality - requirements.QualityPenalty);
		}

		private void ResolveServices()
		{
			if (codeRegistry == null)
			{
				codeRegistry = NodeCodeRegistry.Instance;
			}

			if (codeRegistry == null)
			{
				codeRegistry = FindAnyObjectByType<NodeCodeRegistry>(FindObjectsInactive.Include);
			}

			if (metricsService == null)
			{
				metricsService = FindAnyObjectByType<OsRuntimeMetricsService>(FindObjectsInactive.Include);
			}

			if (timeService == null)
			{
				timeService = FindAnyObjectByType<GameTimeService>(FindObjectsInactive.Include);
			}

			ResolveReputationService();
		}

		private OsModuleRequirementEvaluation EvaluateModuleRequirements()
		{
			int year = GetCurrentYear();

			if (moduleRequirementProfile == null)
			{
				return OsModuleRequirementEvaluation.NotConfigured(year);
			}

			ResolveServices();

			if (metricsService == null)
			{
				return OsModuleRequirementEvaluation.NotConfigured(year);
			}

			metricsService.Recalculate();

			return moduleRequirementProfile.Evaluate(
				year,
				metricsService.HasOperationalReachableDefinition
			);
		}

		private int GetCurrentYear()
		{
			if (timeService == null)
			{
				timeService = FindAnyObjectByType<GameTimeService>(FindObjectsInactive.Include);
			}

			return timeService != null ? timeService.Year : 1995;
		}

		private OsReleaseAudienceModifiers ResolveAudienceModifiers()
		{
			if (!usePlayerReputation)
			{
				return OsReleaseAudienceModifiers.Neutral;
			}

			ResolveReputationService();

			return reputationService != null
				? reputationService.GetAudienceModifiers()
				: OsReleaseAudienceModifiers.Neutral;
		}

		private void ApplyReputationFeedback(float releaseQuality, OsReleaseGrowthResult growth)
		{
			if (!usePlayerReputation)
			{
				return;
			}

			ResolveReputationService();
			reputationService?.ApplyReleaseFeedback(releaseQuality, growth);
		}

		private void ResolveReputationService()
		{
			if (!usePlayerReputation || reputationService != null)
			{
				return;
			}

			reputationService = FindAnyObjectByType<OsPlayerReputationService>(FindObjectsInactive.Include);
		}

		private static string ResolveSupportedDevices(NodeCodeState systemCoreState)
		{
			if (systemCoreState == null ||
				!systemCoreState.ModuleSettings.HasPlatform ||
				string.IsNullOrWhiteSpace(systemCoreState.ModuleSettings.Platform))
			{
				return NoValue;
			}

			return systemCoreState.ModuleSettings.Platform;
		}

		private void LogDeploy(string message)
		{
			if (logDeployResult)
			{
				Debug.Log($"[{nameof(OsReleaseService)}] {message}", this);
			}
		}
	}
}
