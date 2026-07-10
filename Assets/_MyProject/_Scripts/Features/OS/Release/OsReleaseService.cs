using Game.IDE.Presentation;
using Game.Nodes.Runtime;
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
		[SerializeField, Min(0.5f)] private float growthIntervalSeconds = 6f;
		[SerializeField, Min(0)] private int minInstallGrowth = 8;
		[SerializeField, Min(0)] private int maxInstallGrowth = 64;
		[SerializeField, Min(1)] private int maxInstallations = 999999;
		[SerializeField, Range(0.01f, 1f)] private float ratingSmoothing = 0.35f;

		[Header("Debug")]
		[SerializeField] private bool logDeployResult = true;

		private bool isPublished;
		private string supportedDevices = NoValue;
		private int activeInstallations;
		private float userRating;
		private string latestRelease = NoValue;
		private string updateStatus = "Not Published";
		private float growthTimer;

		public OsReleaseState Current => new OsReleaseState(
			isPublished,
			isPublished ? releaseChannel : NoValue,
			HasSupportedDevices ? supportedDevices : NoValue,
			activeInstallations,
			userRating,
			isPublished ? latestRelease : NoValue,
			updateStatus
		);

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

			if (growthTimer < growthIntervalSeconds)
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
		}

		private void AdvanceReleaseGrowth()
		{
			float quality = CalculateReleaseQuality();
			int growth = Mathf.RoundToInt(Mathf.Lerp(minInstallGrowth, maxInstallGrowth, quality));

			activeInstallations = Mathf.Clamp(
				activeInstallations + Mathf.Max(0, growth),
				0,
				maxInstallations
			);

			float targetRating = Mathf.Clamp(1f + quality * 4f, 1f, 5f);
			userRating = activeInstallations <= 0
				? 0f
				: Mathf.Lerp(userRating <= 0f ? targetRating : userRating, targetRating, ratingSmoothing);

			updateStatus = "Current";
		}

		private float CalculateReleaseQuality()
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

			return Mathf.Clamp01(stability * 0.4f + load * 0.25f + memory * 0.2f + network * 0.15f);
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
