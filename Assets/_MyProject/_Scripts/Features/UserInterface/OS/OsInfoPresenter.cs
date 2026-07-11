using Game.Nodes.Runtime;
using Game.OS.Competitors;
using Game.OS.Release;
using UnityEngine;
using VContainer;

namespace Game.UI.OS
{
	[DisallowMultipleComponent]
	public sealed class OsInfoPresenter : MonoBehaviour
	{
		[Header("Panels")]
		[SerializeField] private OsInfoPageBinding[] pages;
		[SerializeField] private OsInfoPanelView statusPanel;
		[SerializeField] private OsInfoPanelView healthPanel;
		[SerializeField] private OsInfoPanelView releasePanel;
		[SerializeField] private OsInfoPanelView competitorsPanel;

		[Header("Data")]
		[SerializeField] private OsInfoSection statusSection = OsInfoDefaults.CreateStatusSection();
		[SerializeField] private OsInfoSection healthSection = OsInfoDefaults.CreateHealthSection();
		[SerializeField] private OsInfoSection releaseSection = OsInfoDefaults.CreateReleaseSection();
		[SerializeField] private OsInfoSection competitorsSection = OsInfoDefaults.CreateCompetitorsSection();
		[SerializeField] private bool useDefaultStatusSection = true;
		[SerializeField] private bool useDefaultReleaseSection = true;
		[SerializeField] private bool useDefaultCompetitorsSection = true;

		[Header("Runtime")]
		[SerializeField] private OsRuntimeMetricsService metricsService;
		[SerializeField] private OsReleaseService releaseService;
		[SerializeField] private OsCompetitorService competitorService;
		[SerializeField] private bool useRuntimeHealthMetrics = true;
		[SerializeField] private bool useRuntimeReleaseInfo = true;
		[SerializeField] private bool useRuntimeCompetitorInfo = true;
		[SerializeField, Min(0.05f)] private float runtimeRefreshInterval = 0.5f;

		[Header("Competitor Ranking")]
		[SerializeField] private bool includePlayerInCompetitorRanking = true;
		[SerializeField] private string playerCompanyName = "You";
		[SerializeField] private string playerOperatingSystemName = "GraphOS";

		[Header("Behaviour")]
		[SerializeField] private bool refreshOnEnable = true;

		private float refreshTimer;

		[Inject]
		private void Construct(
			OsRuntimeMetricsService runtimeMetricsService,
			OsReleaseService osReleaseService,
			OsCompetitorService osCompetitorService)
		{
			if (metricsService == null)
			{
				metricsService = runtimeMetricsService;
			}

			if (releaseService == null)
			{
				releaseService = osReleaseService;
			}

			if (competitorService == null)
			{
				competitorService = osCompetitorService;
			}
		}

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
			if (!useRuntimeHealthMetrics && !useRuntimeReleaseInfo && !useRuntimeCompetitorInfo)
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
			ClearPanels();
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

			OsInfoSection currentCompetitorsSection = TryCreateRuntimeCompetitorsSection(out OsInfoSection runtimeCompetitorsSection)
				? runtimeCompetitorsSection
				: useDefaultCompetitorsSection
					? OsInfoDefaults.CreateCompetitorsSection()
					: competitorsSection;

			RenderSections(
				currentStatusSection,
				currentHealthSection,
				currentReleaseSection,
				currentCompetitorsSection
			);
		}

		[ContextMenu("Apply Default Data")]
		private void ApplyDefaultData()
		{
			statusSection = OsInfoDefaults.CreateStatusSection();
			healthSection = OsInfoDefaults.CreateHealthSection();
			releaseSection = OsInfoDefaults.CreateReleaseSection();
			competitorsSection = OsInfoDefaults.CreateCompetitorsSection();
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

			if (competitorsSection == null || !competitorsSection.HasEntries)
			{
				competitorsSection = OsInfoDefaults.CreateCompetitorsSection();
			}
		}

		private void RenderSections(
			OsInfoSection status,
			OsInfoSection health,
			OsInfoSection release,
			OsInfoSection competitors)
		{
			if (pages != null && pages.Length > 0)
			{
				foreach (OsInfoPageBinding page in pages)
				{
					if (page == null)
					{
						continue;
					}

					page.Render(GetSectionForPage(page.PageType, status, health, release, competitors));
				}

				return;
			}

			statusPanel?.Render(status);
			healthPanel?.Render(health);
			releasePanel?.Render(release);
			competitorsPanel?.Render(competitors);
		}

		private void ClearPanels()
		{
			if (pages != null && pages.Length > 0)
			{
				foreach (OsInfoPageBinding page in pages)
				{
					page?.Clear();
				}

				return;
			}

			statusPanel?.Clear();
			healthPanel?.Clear();
			releasePanel?.Clear();
			competitorsPanel?.Clear();
		}

		private static OsInfoSection GetSectionForPage(
			OsInfoPageType pageType,
			OsInfoSection status,
			OsInfoSection health,
			OsInfoSection release,
			OsInfoSection competitors)
		{
			return pageType switch
			{
				OsInfoPageType.HealthSystem => health,
				OsInfoPageType.ReleaseInformation => release,
				OsInfoPageType.Competitors => competitors,
				_ => status
			};
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
						"Module Requirements",
						release.ModuleRequirementsStatus,
						GetModuleRequirementsTone(release.ModuleRequirementPenaltyPercent)),
					new OsInfoEntry(
						"Missing Modules",
						release.MissingRequiredModules,
						GetMissingModulesTone(release.MissingRequiredModules)),
					new OsInfoEntry(
						"Active Installations",
						release.ActiveInstallationsText,
						release.ActiveInstallations > 0 ? OsInfoTone.Good : OsInfoTone.Bad),
					new OsInfoEntry(
						"Player Reputation",
						release.PlayerReputationText,
						GetPositiveScoreTone(release.PlayerReputationPercent)),
					new OsInfoEntry(
						"Audience Trust",
						release.PlayerReputationTier,
						GetPositiveScoreTone(release.PlayerReputationPercent)),
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

		private bool TryCreateRuntimeCompetitorsSection(out OsInfoSection section)
		{
			section = null;

			if (!useRuntimeCompetitorInfo)
			{
				return false;
			}

			ResolveCompetitorService();

			OsCompetitorState[] competitors = competitorService != null && competitorService.HasConfig
				? competitorService.CreateSnapshot()
				: System.Array.Empty<OsCompetitorState>();

			if ((competitors == null || competitors.Length == 0) && !includePlayerInCompetitorRanking)
			{
				return false;
			}

			OsCompetitorState[] rankedCompetitors = OsCompetitorLeaderboardBuilder.CreateRankedSnapshot(
				competitors,
				CreatePlayerCompetitorState(),
				includePlayerInCompetitorRanking
			);

			if (rankedCompetitors == null || rankedCompetitors.Length == 0)
			{
				return false;
			}

			OsInfoEntry[] entries = new OsInfoEntry[rankedCompetitors.Length];

			for (int i = 0; i < rankedCompetitors.Length; i++)
			{
				entries[i] = CreateCompetitorEntry(rankedCompetitors[i], i + 1);
			}

			section = new OsInfoSection("Competitors", entries);
			return true;
		}

		private void ResolveCompetitorService()
		{
			if (competitorService != null)
			{
				return;
			}

			competitorService = FindAnyObjectByType<OsCompetitorService>(FindObjectsInactive.Include);
		}

		private OsCompetitorState CreatePlayerCompetitorState()
		{
			ResolveReleaseService();

			OsReleaseState release = releaseService != null ? releaseService.Current : default;
			int activeUsers = releaseService != null ? release.ActiveInstallations : 0;

			return new OsCompetitorState(
				playerCompanyName,
				playerOperatingSystemName,
				0,
				activeUsers,
				isPlayer: true
			);
		}

		private static OsInfoEntry CreateCompetitorEntry(OsCompetitorState competitor, int rank)
		{
			return new OsInfoEntry(
				$"{rank}. {competitor.CompanyName}",
				competitor.DisplayValue,
				competitor.IsPlayer
					? OsInfoTone.Good
					: GetCompetitorUsersTone(competitor.CurrentUsers)
			);
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

		private static OsInfoTone GetModuleRequirementsTone(float penaltyPercent)
		{
			if (penaltyPercent <= 0f)
			{
				return OsInfoTone.Good;
			}

			return penaltyPercent < 25f ? OsInfoTone.Warning : OsInfoTone.Bad;
		}

		private static OsInfoTone GetMissingModulesTone(string missingModules)
		{
			return string.IsNullOrWhiteSpace(missingModules) ||
				   string.Equals(missingModules, "None", System.StringComparison.OrdinalIgnoreCase)
				? OsInfoTone.Good
				: OsInfoTone.Bad;
		}

		private static OsInfoTone GetCompetitorUsersTone(int users)
		{
			if (users >= 1000000)
			{
				return OsInfoTone.Bad;
			}

			if (users >= 100000)
			{
				return OsInfoTone.Warning;
			}

			return OsInfoTone.Neutral;
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

			if (status != null &&
				status.IndexOf("Word Of Mouth", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return status.IndexOf("Excellent", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
					   status.IndexOf("Positive", System.StringComparison.OrdinalIgnoreCase) >= 0
					? OsInfoTone.Good
					: OsInfoTone.Warning;
			}

			if (string.Equals(status, "Published", System.StringComparison.OrdinalIgnoreCase))
			{
				return OsInfoTone.Warning;
			}

			return OsInfoTone.Bad;
		}
	}
}
