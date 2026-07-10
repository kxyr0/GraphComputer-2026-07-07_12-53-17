using System;
using System.Collections.Generic;
using Game.Nodes.Runtime;
using Game.OS.Release;
using Game.World.Time;
using UnityEngine;

namespace Game.World.Reports
{
	[DisallowMultipleComponent]
	public sealed class UserReportsService : MonoBehaviour
	{
		private static readonly string[] CoreMissingReports =
		{
			"I installed the OS, but nothing really works yet.",
			"The system looks unfinished.",
			"Core services are missing.",
			"I opened the desktop and found empty system tools.",
			"Nothing responds after the first screen.",
			"This build feels like a shell without an engine.",
			"Basic services are not ready for users."
		};

		private static readonly string[] KernelOfflineReports =
		{
			"Apps freeze before they even start.",
			"The system cannot handle basic multitasking.",
			"Everything feels unstable.",
			"Windows hang before I can switch tasks.",
			"Launching two apps at once breaks the session.",
			"The desktop stops responding under basic use.",
			"The kernel is not keeping the OS alive."
		};

		private static readonly string[] LowStabilityReports =
		{
			"The OS crashed while I was working.",
			"I lost progress after a random freeze.",
			"This version feels unreliable.",
			"It crashes before I can finish a simple task.",
			"I keep restarting after random lockups.",
			"This update made my machine feel fragile.",
			"The session dies without a clear reason."
		};

		private static readonly string[] HighLoadReports =
		{
			"The computer becomes slow after a few minutes.",
			"Everything feels heavy and unresponsive.",
			"The OS uses too many resources.",
			"Opening a window already feels expensive.",
			"CPU load spikes while I am doing almost nothing.",
			"The interface lags during normal use.",
			"Performance drops faster than expected."
		};

		private static readonly string[] HighMemoryReports =
		{
			"Memory fills up too quickly.",
			"Old machines cannot run this version smoothly.",
			"Apps slow down after opening a few windows.",
			"Background services leave too little RAM for work.",
			"I need to close apps just to keep the desktop usable.",
			"The memory footprint is too high for this build.",
			"Tabs and tools start stuttering after a short session."
		};

		private static readonly string[] HighStorageReports =
		{
			"The system eats disk space too fast.",
			"Updates are taking too much storage.",
			"Cleanup tools are missing.",
			"Temporary files keep growing without warning.",
			"The install size is hard to justify.",
			"I cannot find what is filling the drive.",
			"Storage management needs clearer controls."
		};

		private static readonly string[] SlowNetworkReports =
		{
			"Downloads are painfully slow.",
			"Online features feel unreliable.",
			"Network performance needs work.",
			"Pages time out before they finish loading.",
			"Sync keeps retrying instead of completing.",
			"The connection feels unstable even on a good network.",
			"Cloud features are too slow to trust."
		};

		private static readonly string[] WeakSecurityReports =
		{
			"I do not trust this OS with important files.",
			"Security tools feel unfinished.",
			"The system needs better protection.",
			"Permissions are too unclear for daily use.",
			"I want stronger warnings before unsafe actions.",
			"The protection status does not explain enough.",
			"Encryption and vulnerability checks need work."
		};

		private static readonly string[] FalseBlockReports =
		{
			"Legitimate apps keep getting blocked.",
			"The firewall is too aggressive for normal work.",
			"I spend too much time approving safe connections.",
			"Online tools break because protection blocks too much.",
			"Security is useful, but the false blocks are frustrating.",
			"Trusted services should not be interrupted this often."
		};

		private static readonly string[] PraiseReports =
		{
			"This version feels smooth.",
			"Boot and basic apps work well.",
			"The system is becoming reliable.",
			"Performance is much better than before.",
			"I can finally use the desktop without waiting.",
			"The latest changes made the OS feel cleaner.",
			"Apps open quickly and stay responsive.",
			"This build is starting to feel like a real release."
		};

		private static readonly string[] MixedReports =
		{
			"The OS is usable, but a few parts still feel rough.",
			"Some apps work well, while others still need polish.",
			"The system is improving, but users still notice issues.",
			"It works for simple tasks, but I would not rely on it yet.",
			"The direction is good, but the rough edges are visible.",
			"I can use it, but I keep finding small blockers.",
			"The core experience is close, but not fully comfortable."
		};

		private static readonly string[] NicknamePrefixes =
		{
			"pixel",
			"byte",
			"kernel",
			"boot",
			"cache",
			"signal",
			"vector",
			"sys",
			"nova",
			"mono",
			"logic",
			"desk",
			"ram",
			"trace",
			"patch",
			"flux"
		};

		private static readonly string[] NicknameSuffixes =
		{
			"pilot",
			"runner",
			"tester",
			"reader",
			"admin",
			"mapper",
			"loader",
			"driver",
			"keeper",
			"sender",
			"builder",
			"watcher",
			"worker",
			"debugger",
			"operator",
			"guest"
		};

		[SerializeField] private OsRuntimeMetricsService metricsService;
		[SerializeField] private GameTimeService timeService;
		[SerializeField] private OsReleaseService releaseService;

		[Header("Generation")]
		[SerializeField, Min(1f)] private float generationIntervalSeconds = 8f;
		[SerializeField] private bool generateOnStart = true;
		[SerializeField] private bool logReports = true;

		[Header("Release Gate")]
		[SerializeField] private bool requirePublishedRelease = true;
		[SerializeField] private bool syncActiveUsersFromRelease = true;

		[Header("History")]
		[SerializeField, Min(1)] private int maxReports = 24;

		[Header("Users")]
		[SerializeField, Min(0)] private int baseActiveUsers;
		[SerializeField, Min(0)] private int minActiveUsers = 0;
		[SerializeField, Min(1)] private int maxActiveUsers = 9999;

		private readonly List<UserReportEntry> reports = new();
		private readonly List<UserReportCandidate> candidates = new();

		private float generationTimer;
		private int totalReports;
		private int totalCriticalReports;
		private bool hasGenerated;
		private bool loggedNoActiveUsers;

		public IReadOnlyList<UserReportEntry> Reports => reports;
		public UserReportEntry LatestReport { get; private set; }
		public UserReportMood Mood { get; private set; } = UserReportMood.Mixed;
		public int ActiveUsers { get; private set; }
		public int ReportCount => totalReports;
		public int CriticalCount => totalCriticalReports;
		public string LatestMessage => LatestReport != null
			? LatestReport.Message
			: "No reports yet.";

		private void Awake()
		{
			ResolveServices();
			ActiveUsers = requirePublishedRelease
				? 0
				: Mathf.Clamp(baseActiveUsers, minActiveUsers, maxActiveUsers);
			SyncReleaseAudience();
		}

		private void Start()
		{
			if (generateOnStart && IsReleaseAvailableForReports())
			{
				EnsureReport();
			}
		}

		private void Update()
		{
			ResolveServices();
			SyncReleaseAudience();

			if (!IsReleaseAvailableForReports())
			{
				return;
			}

			generationTimer += UnityEngine.Time.unscaledDeltaTime;

			if (generationTimer < generationIntervalSeconds)
			{
				return;
			}

			generationTimer = 0f;
			GenerateReport();
		}

		private void OnValidate()
		{
			generationIntervalSeconds = Mathf.Max(1f, generationIntervalSeconds);
			maxReports = Mathf.Max(1, maxReports);
			baseActiveUsers = Mathf.Max(0, baseActiveUsers);
			minActiveUsers = Mathf.Max(0, minActiveUsers);
			maxActiveUsers = Mathf.Max(1, maxActiveUsers);

			if (maxActiveUsers < minActiveUsers)
			{
				maxActiveUsers = minActiveUsers;
			}
		}

		public static UserReportsService GetOrCreate()
		{
			UserReportsService service =
				FindAnyObjectByType<UserReportsService>(FindObjectsInactive.Include);

			if (service != null)
			{
				return service;
			}

			GameObject serviceObject = new GameObject("UserReportsService");
			return serviceObject.AddComponent<UserReportsService>();
		}

		public void EnsureReport()
		{
			if (hasGenerated)
			{
				return;
			}

			if (!IsReleaseAvailableForReports())
			{
				return;
			}

			GenerateReport();
		}

		public void GenerateReport()
		{
			ResolveServices();
			SyncReleaseAudience();

			if (!IsReleaseAvailableForReports())
			{
				return;
			}

			UserReportContext context = BuildContext();
			UserReportMood previousMood = Mood;
			Mood = CalculateMood(context);

			if (hasGenerated && previousMood != Mood && logReports)
			{
				Debug.Log($"[UserReports] Mood changed: {previousMood} -> {Mood}", this);
			}

			if (!CanGenerateReport())
			{
				LogNoActiveUsersOnce();
				return;
			}

			UserReportCandidate candidate = SelectCandidate(context);
			string message = SelectTemplate(candidate);
			string authorName = SelectAuthorName(candidate);
			UserReportEntry entry = new UserReportEntry(
				message,
				candidate.Severity,
				candidate.Category,
				GetDateText(),
				authorName
			);

			AddReport(entry);
			UpdateActiveUsers(entry.Severity);

			hasGenerated = true;
			loggedNoActiveUsers = false;

			if (logReports)
			{
				Debug.Log(
					$"[UserReports] Generated {entry.Severity} report from {entry.AuthorName}: {entry.Message}",
					this);
			}
		}

		private void ResolveServices()
		{
			if (metricsService == null)
			{
				metricsService = FindAnyObjectByType<OsRuntimeMetricsService>();
			}

			if (timeService == null)
			{
				timeService = FindAnyObjectByType<GameTimeService>();
			}

			if (releaseService == null)
			{
				releaseService = FindAnyObjectByType<OsReleaseService>(FindObjectsInactive.Include);
			}
		}

		private UserReportContext BuildContext()
		{
			if (metricsService == null)
			{
				return UserReportContext.MissingMetrics();
			}

			metricsService.Recalculate();

			OsRuntimeMetrics system = metricsService.Current;
			OsKernelRuntimeMetrics kernel = metricsService.CurrentKernel;
			OsSecurityRuntimeMetrics security = metricsService.CurrentSecurity;

			bool coreMissing =
				system.StabilityPercent <= 0.5f &&
				system.SystemLoadPercent <= 0.5f &&
				system.MemoryUsagePercent <= 0.5f &&
				system.StorageUsagePercent <= 0.5f &&
				system.NetworkSpeedMbps <= 0.01f;

			bool kernelOffline =
				string.Equals(kernel.KernelTypeText, "Offline", StringComparison.OrdinalIgnoreCase) ||
				kernel.ProcessLimit <= 0 ||
				kernel.CrashRiskPercent >= 99f;

			return new UserReportContext(
				system,
				kernel,
				security,
				coreMissing,
				kernelOffline,
				CalculateIssueScore(system, kernel, security, coreMissing, kernelOffline)
			);
		}

		private int CalculateIssueScore(
			OsRuntimeMetrics system,
			OsKernelRuntimeMetrics kernel,
			OsSecurityRuntimeMetrics security,
			bool coreMissing,
			bool kernelOffline)
		{
			int score = 0;

			if (coreMissing)
			{
				score += 6;
			}

			if (kernelOffline)
			{
				score += 5;
			}

			score += ScoreBelow(system.StabilityPercent, 35f, 55f, 5, 3);
			score += ScoreAbove(system.SystemLoadPercent, 85f, 70f, 4, 2);
			score += ScoreAbove(system.MemoryUsagePercent, 85f, 70f, 3, 2);
			score += ScoreAbove(system.StorageUsagePercent, 90f, 75f, 3, 2);
			score += ScoreBelow(system.NetworkEraScorePercent, 35f, 70f, 3, 2);
			score += ScoreAbove(kernel.CrashRiskPercent, 65f, 35f, 4, 2);
			score += ScoreAbove(security.Vulnerabilities, 8f, 5f, 4, 2);
			score += ScoreAbove(security.FalseBlocksPercent, 30f, 15f, 3, 1);
			score += ScoreBelow(security.EncryptionPercent, 25f, 50f, 3, 1);

			return score;
		}

		private UserReportMood CalculateMood(UserReportContext context)
		{
			if (context.CoreMissing || context.IssueScore >= 14)
			{
				return UserReportMood.Critical;
			}

			if (context.IssueScore >= 10)
			{
				return UserReportMood.Angry;
			}

			if (context.IssueScore >= 7)
			{
				return UserReportMood.Concerned;
			}

			if (context.IssueScore >= 4)
			{
				return UserReportMood.Mixed;
			}

			if (IsExcellent(context))
			{
				return UserReportMood.Excellent;
			}

			if (IsStable(context))
			{
				return UserReportMood.Stable;
			}

			return UserReportMood.Mixed;
		}

		private bool IsExcellent(UserReportContext context)
		{
			return
				context.System.StabilityPercent >= 85f &&
				context.System.SystemLoadPercent <= 35f &&
				context.System.MemoryUsagePercent <= 55f &&
				context.Security.Vulnerabilities <= 2 &&
				context.Kernel.CrashRiskPercent <= 15f &&
				context.System.NetworkEraScorePercent >= 100f;
		}

		private bool IsStable(UserReportContext context)
		{
			return
				context.System.StabilityPercent >= 70f &&
				context.System.SystemLoadPercent <= 55f &&
				context.System.MemoryUsagePercent <= 70f &&
				context.Security.Vulnerabilities <= 5 &&
				context.Kernel.CrashRiskPercent <= 35f;
		}

		private UserReportCandidate SelectCandidate(UserReportContext context)
		{
			candidates.Clear();

			AddCondition(
				context.CoreMissing,
				UserReportSeverity.Critical,
				"Core",
				1000,
				CoreMissingReports);

			AddCondition(
				context.KernelOffline,
				UserReportSeverity.Critical,
				"Kernel",
				900,
				KernelOfflineReports);

			AddCondition(
				context.System.StabilityPercent < 55f && !context.CoreMissing,
				context.System.StabilityPercent < 35f
					? UserReportSeverity.Critical
					: UserReportSeverity.Warning,
				"Stability",
				80 + Mathf.RoundToInt(55f - context.System.StabilityPercent),
				LowStabilityReports);

			AddCondition(
				context.System.SystemLoadPercent > 70f,
				context.System.SystemLoadPercent > 88f
					? UserReportSeverity.Critical
					: UserReportSeverity.Warning,
				"Performance",
				70 + Mathf.RoundToInt(context.System.SystemLoadPercent),
				HighLoadReports);

			AddCondition(
				context.System.MemoryUsagePercent > 70f,
				UserReportSeverity.Warning,
				"Memory",
				60 + Mathf.RoundToInt(context.System.MemoryUsagePercent),
				HighMemoryReports);

			AddCondition(
				context.System.StorageUsagePercent > 75f,
				UserReportSeverity.Warning,
				"Storage",
				55 + Mathf.RoundToInt(context.System.StorageUsagePercent),
				HighStorageReports);

			AddCondition(
				context.System.NetworkEraScorePercent < 70f && !context.CoreMissing,
				context.System.NetworkEraScorePercent < 35f
					? UserReportSeverity.Critical
					: UserReportSeverity.Warning,
				"Network",
				65 + Mathf.RoundToInt(100f - context.System.NetworkEraScorePercent),
				SlowNetworkReports);

			AddCondition(
				context.Security.Vulnerabilities > 5 || context.Security.EncryptionPercent < 50f,
				context.Security.Vulnerabilities > 8 || context.Security.EncryptionPercent < 25f
					? UserReportSeverity.Critical
					: UserReportSeverity.Warning,
				"Security",
				75 + context.Security.Vulnerabilities,
				WeakSecurityReports);

			AddCondition(
				context.Security.FalseBlocksPercent > 15f,
				context.Security.FalseBlocksPercent > 35f
					? UserReportSeverity.Critical
					: UserReportSeverity.Warning,
				"Firewall",
				60 + Mathf.RoundToInt(context.Security.FalseBlocksPercent),
				FalseBlockReports);

			AddCondition(
				Mood == UserReportMood.Excellent || Mood == UserReportMood.Stable,
				UserReportSeverity.Praise,
				"General",
				Mood == UserReportMood.Excellent ? 70 : 50,
				PraiseReports);

			if (candidates.Count == 0)
			{
				return new UserReportCandidate(
					UserReportSeverity.Info,
					"General",
					0,
					MixedReports
				);
			}

			candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
			return candidates[0];
		}

		private void AddCondition(
			bool condition,
			UserReportSeverity severity,
			string category,
			int score,
			string[] templates)
		{
			if (!condition)
			{
				return;
			}

			candidates.Add(new UserReportCandidate(severity, category, score, templates));
		}

		private string SelectTemplate(UserReportCandidate candidate)
		{
			if (candidate.Templates == null || candidate.Templates.Length == 0)
			{
				return "Users are waiting for more system feedback.";
			}

			int seed = (StableStringHash(candidate.Category) & int.MaxValue) + totalReports;
			int index = seed % candidate.Templates.Length;
			return candidate.Templates[index];
		}

		private string SelectAuthorName(UserReportCandidate candidate)
		{
			int seed =
				(StableStringHash(candidate.Category) & int.MaxValue) +
				totalReports * 97 +
				(int)candidate.Severity * 31;
			int prefixIndex = seed % NicknamePrefixes.Length;
			int suffixIndex = (seed / NicknamePrefixes.Length) % NicknameSuffixes.Length;
			int number = 10 + seed % 90;
			return $"{NicknamePrefixes[prefixIndex]}_{NicknameSuffixes[suffixIndex]}{number}";
		}

		private void AddReport(UserReportEntry entry)
		{
			LatestReport = entry;
			reports.Add(entry);

			while (reports.Count > maxReports)
			{
				reports.RemoveAt(0);
			}

			totalReports++;

			if (entry.Severity == UserReportSeverity.Critical)
			{
				totalCriticalReports++;
			}
		}

		private bool CanGenerateReport()
		{
			return ActiveUsers > 0;
		}

		private bool IsReleaseAvailableForReports()
		{
			if (!requirePublishedRelease)
			{
				return true;
			}

			if (releaseService == null)
			{
				ResolveServices();
			}

			return releaseService != null &&
				   releaseService.HasPublishedBuild &&
				   releaseService.ActiveInstallations > 0;
		}

		private void SyncReleaseAudience()
		{
			if (!syncActiveUsersFromRelease)
			{
				return;
			}

			if (releaseService == null)
			{
				ResolveServices();
			}

			if (releaseService == null)
			{
				if (requirePublishedRelease)
				{
					ActiveUsers = 0;
				}

				return;
			}

			ActiveUsers = Mathf.Clamp(
				releaseService.ActiveInstallations,
				minActiveUsers,
				maxActiveUsers
			);
		}

		private void LogNoActiveUsersOnce()
		{
			if (!logReports || loggedNoActiveUsers)
			{
				return;
			}

			loggedNoActiveUsers = true;
			Debug.Log("[UserReports] Report generation paused: no active users.", this);
		}

		private void UpdateActiveUsers(UserReportSeverity severity)
		{
			if (syncActiveUsersFromRelease && releaseService != null)
			{
				SyncReleaseAudience();
				return;
			}

			int previous = ActiveUsers;
			int delta = CalculateUserDelta(Mood, severity);
			ActiveUsers = Mathf.Clamp(ActiveUsers + delta, minActiveUsers, maxActiveUsers);
			loggedNoActiveUsers = ActiveUsers <= 0 && loggedNoActiveUsers;

			if (previous != ActiveUsers && logReports)
			{
				Debug.Log($"[UserReports] Active users: {previous} -> {ActiveUsers}", this);
			}
		}

		private static int CalculateUserDelta(
			UserReportMood mood,
			UserReportSeverity severity)
		{
			int delta = mood switch
			{
				UserReportMood.Excellent => 12,
				UserReportMood.Stable => 7,
				UserReportMood.Mixed => 1,
				UserReportMood.Concerned => -4,
				UserReportMood.Angry => -9,
				UserReportMood.Critical => -16,
				_ => 0
			};

			if (severity == UserReportSeverity.Critical)
			{
				delta -= 4;
			}

			return delta;
		}

		private string GetDateText()
		{
			return timeService != null
				? timeService.GetDateText()
				: "Unknown Date";
		}

		private static int ScoreAbove(
			float value,
			float criticalThreshold,
			float warningThreshold,
			int criticalScore,
			int warningScore)
		{
			if (value >= criticalThreshold)
			{
				return criticalScore;
			}

			return value >= warningThreshold ? warningScore : 0;
		}

		private static int ScoreBelow(
			float value,
			float criticalThreshold,
			float warningThreshold,
			int criticalScore,
			int warningScore)
		{
			if (value <= criticalThreshold)
			{
				return criticalScore;
			}

			return value <= warningThreshold ? warningScore : 0;
		}

		private static int StableStringHash(string value)
		{
			unchecked
			{
				int hash = 17;

				if (string.IsNullOrEmpty(value))
				{
					return hash;
				}

				for (int i = 0; i < value.Length; i++)
				{
					hash = hash * 31 + value[i];
				}

				return hash;
			}
		}

		private readonly struct UserReportCandidate
		{
			public readonly UserReportSeverity Severity;
			public readonly string Category;
			public readonly int Score;
			public readonly string[] Templates;

			public UserReportCandidate(
				UserReportSeverity severity,
				string category,
				int score,
				string[] templates)
			{
				Severity = severity;
				Category = category;
				Score = score;
				Templates = templates;
			}
		}

		private readonly struct UserReportContext
		{
			public readonly OsRuntimeMetrics System;
			public readonly OsKernelRuntimeMetrics Kernel;
			public readonly OsSecurityRuntimeMetrics Security;
			public readonly bool CoreMissing;
			public readonly bool KernelOffline;
			public readonly int IssueScore;

			public UserReportContext(
				OsRuntimeMetrics system,
				OsKernelRuntimeMetrics kernel,
				OsSecurityRuntimeMetrics security,
				bool coreMissing,
				bool kernelOffline,
				int issueScore)
			{
				System = system;
				Kernel = kernel;
				Security = security;
				CoreMissing = coreMissing;
				KernelOffline = kernelOffline;
				IssueScore = issueScore;
			}

			public static UserReportContext MissingMetrics()
			{
				return new UserReportContext(
					new OsRuntimeMetrics(0f, 0f, 0f, 0f, 0f),
					new OsKernelRuntimeMetrics("Offline", 0, 100f, 100f, 0f),
					new OsSecurityRuntimeMetrics("Critical", "None", 99, 0f, 0f),
					true,
					true,
					15
				);
			}
		}
	}
}
