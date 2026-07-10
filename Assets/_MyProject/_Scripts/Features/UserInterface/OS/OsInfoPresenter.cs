using Game.Nodes.Runtime;
using Game.OS.Release;
using UnityEngine;

namespace Game.UI.OS
{
	[DisallowMultipleComponent]
	public sealed class OsInfoPresenter : MonoBehaviour
	{
		[Header("Panels")]
		[SerializeField] private OsInfoPanelView statusPanel;
		[SerializeField] private OsInfoPanelView healthPanel;
		[SerializeField] private OsInfoPanelView releasePanel;

		[Header("Data")]
		[SerializeField] private OsInfoSection statusSection = OsInfoDefaults.CreateStatusSection();
		[SerializeField] private OsInfoSection healthSection = OsInfoDefaults.CreateHealthSection();
		[SerializeField] private OsInfoSection releaseSection = OsInfoDefaults.CreateReleaseSection();
		[SerializeField] private bool useDefaultStatusSection = true;
		[SerializeField] private bool useDefaultReleaseSection = true;

		[Header("Runtime")]
		[SerializeField] private OsRuntimeMetricsService metricsService;
		[SerializeField] private OsReleaseService releaseService;
		[SerializeField] private bool useRuntimeHealthMetrics = true;
		[SerializeField] private bool useRuntimeReleaseInfo = true;
		[SerializeField, Min(0.05f)] private float runtimeRefreshInterval = 0.5f;

		[Header("Behaviour")]
		[SerializeField] private bool refreshOnEnable = true;

		private float refreshTimer;

		private void Reset()
		{
			ApplyDefaultData();
		}

		private void Awake()
		{
			EnsureData();
			Refresh();
		}

		private void OnEnable()
		{
			if (refreshOnEnable)
			{
				Refresh();
			}
		}

		private void Update()
		{
			if (!useRuntimeHealthMetrics && !useRuntimeReleaseInfo)
			{
				return;
			}

			refreshTimer += Time.unscaledDeltaTime;

			if (refreshTimer < runtimeRefreshInterval)
			{
				return;
			}

			refreshTimer = 0f;
			Refresh();
		}

		private void OnDisable()
		{
			statusPanel.Clear();
			healthPanel.Clear();
			releasePanel.Clear();
		}

		[ContextMenu("Refresh OS Info")]
		public void Refresh()
		{
			OsInfoSection currentStatusSection = useDefaultStatusSection
				? OsInfoDefaults.CreateStatusSection()
				: statusSection;

			OsInfoSection currentHealthSection = TryCreateRuntimeHealthSection(out OsInfoSection runtimeHealthSection)
				? runtimeHealthSection
				: healthSection;

			OsInfoSection currentReleaseSection = TryCreateRuntimeReleaseSection(out OsInfoSection runtimeReleaseSection)
				? runtimeReleaseSection
				: useDefaultReleaseSection
					? OsInfoDefaults.CreateReleaseSection()
					: releaseSection;

			statusPanel.Render(currentStatusSection);
			healthPanel.Render(currentHealthSection);
			releasePanel.Render(currentReleaseSection);
		}

		[ContextMenu("Apply Default Data")]
		private void ApplyDefaultData()
		{
			statusSection = OsInfoDefaults.CreateStatusSection();
			healthSection = OsInfoDefaults.CreateHealthSection();
			releaseSection = OsInfoDefaults.CreateReleaseSection();
		}

		private void EnsureData()
		{
			if (statusSection == null || !statusSection.HasEntries)
			{
				statusSection = OsInfoDefaults.CreateStatusSection();
			}

			if (healthSection == null || !healthSection.HasEntries)
			{
				healthSection = OsInfoDefaults.CreateHealthSection();
			}

			if (releaseSection == null || !releaseSection.HasEntries)
			{
				releaseSection = OsInfoDefaults.CreateReleaseSection();
			}
		}

		private bool TryCreateRuntimeHealthSection(out OsInfoSection section)
		{
			section = null;

			if (!useRuntimeHealthMetrics)
			{
				return false;
			}

			ResolveMetricsService();

			if (metricsService == null)
			{
				return false;
			}

			metricsService.Recalculate();

			OsRuntimeMetrics system = metricsService.Current;
			OsSecurityRuntimeMetrics security = metricsService.CurrentSecurity;
			OsKernelRuntimeMetrics kernel = metricsService.CurrentKernel;
			OsControlRuntimeMetrics control = metricsService.CurrentControl;
			OsDataRuntimeMetrics data = metricsService.CurrentData;

			float stability = system.StabilityPercent;
			float securityScore = CalculateSecurityScore(security);
			float performance = CalculatePerformanceScore(system, kernel, control);
			float compatibility = control.DeviceSupportPercent;
			float codeQuality = CalculateCodeQualityScore(system, kernel, data);

			section = new OsInfoSection(
				"Health OS",
				new[]
				{
					CreatePercentEntry("Stability", stability),
					CreatePercentEntry("Security", securityScore),
					CreatePercentEntry("Performance", performance),
					CreatePercentEntry("Compatibility", compatibility),
					CreatePercentEntry("Code Quality", codeQuality)
				}
			);

			return true;
		}

		private void ResolveMetricsService()
		{
			if (metricsService != null)
			{
				return;
			}

			metricsService = FindAnyObjectByType<OsRuntimeMetricsService>(FindObjectsInactive.Include);
		}

		private bool TryCreateRuntimeReleaseSection(out OsInfoSection section)
		{
			section = null;

			if (!useRuntimeReleaseInfo)
			{
				return false;
			}

			ResolveReleaseService();

			if (releaseService == null)
			{
				return false;
			}

			OsReleaseState release = releaseService.Current;

			section = new OsInfoSection(
				"Release Information",
				new[]
				{
					new OsInfoEntry(
						"Release Channel",
						release.ReleaseChannel,
						release.IsPublished ? OsInfoTone.Warning : OsInfoTone.Bad),
					new OsInfoEntry(
						"Supported Devices",
						release.SupportedDevices,
						GetSupportedDevicesTone(release.SupportedDevices)),
					new OsInfoEntry(
						"Active Installations",
						release.ActiveInstallationsText,
						release.ActiveInstallations > 0 ? OsInfoTone.Good : OsInfoTone.Bad),
					new OsInfoEntry(
						"User Rating",
						release.UserRatingText,
						GetRatingTone(release.UserRating)),
					new OsInfoEntry(
						"Latest Release",
						release.LatestRelease,
						release.IsPublished ? OsInfoTone.Neutral : OsInfoTone.Bad),
					new OsInfoEntry(
						"Update Status",
						release.UpdateStatus,
						GetUpdateStatusTone(release.UpdateStatus))
				}
			);

			return true;
		}

		private void ResolveReleaseService()
		{
			if (releaseService != null)
			{
				return;
			}

			releaseService = OsReleaseService.GetOrCreate();
		}

		private static OsInfoEntry CreatePercentEntry(string label, float value)
		{
			float clampedValue = Mathf.Clamp(value, 0f, 100f);
			return new OsInfoEntry(
				label,
				$"{Mathf.RoundToInt(clampedValue)}%",
				GetPositiveScoreTone(clampedValue)
			);
		}

		private static float CalculateSecurityScore(OsSecurityRuntimeMetrics security)
		{
			float score =
				100f -
				security.Vulnerabilities * 10f -
				security.FalseBlocksPercent * 0.2f +
				security.EncryptionPercent * 0.35f;

			return Mathf.Clamp(score, 0f, 100f);
		}

		private static float CalculatePerformanceScore(
			OsRuntimeMetrics system,
			OsKernelRuntimeMetrics kernel,
			OsControlRuntimeMetrics control)
		{
			float score =
				100f -
				system.SystemLoadPercent * 0.55f -
				system.MemoryUsagePercent * 0.25f -
				control.GpuUsagePercent * 0.1f -
				kernel.ResponseTimeMs * 0.05f;

			return Mathf.Clamp(score, 0f, 100f);
		}

		private static float CalculateCodeQualityScore(
			OsRuntimeMetrics system,
			OsKernelRuntimeMetrics kernel,
			OsDataRuntimeMetrics data)
		{
			float score =
				system.StabilityPercent * 0.45f +
				(100f - kernel.CrashRiskPercent) * 0.25f +
				(100f - kernel.LeakRiskPercent) * 0.15f +
				(100f - data.FragmentationPercent) * 0.15f;

			return Mathf.Clamp(score, 0f, 100f);
		}

		private static OsInfoTone GetPositiveScoreTone(float value)
		{
			if (value >= 70f)
			{
				return OsInfoTone.Good;
			}

			if (value >= 40f)
			{
				return OsInfoTone.Warning;
			}

			return OsInfoTone.Bad;
		}

		private static OsInfoTone GetSupportedDevicesTone(string supportedDevices)
		{
			return string.IsNullOrWhiteSpace(supportedDevices) ||
				   string.Equals(supportedDevices, "None", System.StringComparison.OrdinalIgnoreCase)
				? OsInfoTone.Bad
				: OsInfoTone.Good;
		}

		private static OsInfoTone GetRatingTone(float rating)
		{
			if (rating >= 3.8f)
			{
				return OsInfoTone.Good;
			}

			if (rating >= 2.5f)
			{
				return OsInfoTone.Warning;
			}

			return OsInfoTone.Bad;
		}

		private static OsInfoTone GetUpdateStatusTone(string status)
		{
			if (string.Equals(status, "Current", System.StringComparison.OrdinalIgnoreCase))
			{
				return OsInfoTone.Good;
			}

			if (string.Equals(status, "Published", System.StringComparison.OrdinalIgnoreCase))
			{
				return OsInfoTone.Warning;
			}

			return OsInfoTone.Bad;
		}
	}
}
