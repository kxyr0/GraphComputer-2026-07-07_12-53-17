using System;
using UnityEngine;

namespace Game.Nodes.Runtime
{
	public enum OsMetricVariationMode
	{
		None,
		SmoothNoise,
		RandomOnRefresh
	}

	[CreateAssetMenu(
		fileName = "OsRuntimeMetricsConfig",
		menuName = "Graph Computer/OS/Runtime Metrics Config"
	)]
	public sealed class OsRuntimeMetricsConfig : ScriptableObject
	{
		[Header("Root")]
		[SerializeField] private string rootNodeDefinitionId = "system_core";

		[Header("Base System Metrics")]
		[SerializeField, Range(0f, 100f)] private float baseStability = 50f;
		[SerializeField, Range(0f, 100f)] private float baseSystemLoad = 8f;
		[SerializeField, Range(0f, 100f)] private float baseMemoryUsage = 12f;
		[SerializeField, Range(0f, 100f)] private float baseStorageUsage = 4f;
		[SerializeField, Min(0f)] private float baseNetworkSpeedMbps = 0.25f;

		[Header("Network Era Obsolescence")]
		[SerializeField] private OsNetworkEraBenchmark[] networkEraBenchmarks =
			OsNetworkEraBenchmark.Default1995To1996();

		[Header("System Penalties")]
		[SerializeField, Range(0f, 25f)] private float missingMainBranchPenalty = 8f;
		[SerializeField, Range(0f, 1f)] private float highLoadStabilityPenalty = 0.35f;
		[SerializeField, Range(0f, 1f)] private float highMemoryStabilityPenalty = 0.2f;

		[Header("Data Metrics")]
		[SerializeField] private string dataFileSystemName = "FAT-95";
		[SerializeField, Min(0f)] private float baseDataReadSpeedMb = 1.2f;
		[SerializeField, Range(0f, 100f)] private float baseDataSafety = 42f;
		[SerializeField, Min(0f)] private float baseDataIndexSizeMb = 14f;

		[SerializeField, Min(0f)] private float fileSystemReadSpeedBonusMb = 0.9f;
		[SerializeField, Range(0f, 100f)] private float fileSystemSafetyBonus = 10f;
		[SerializeField, Range(0f, 100f)] private float backupSafetyBonus = 18f;
		[SerializeField, Range(0f, 100f)] private float recoverySafetyBonus = 12f;
		[SerializeField, Range(0.1f, 1f)] private float compressionIndexMultiplier = 0.72f;

		[Header("Kernel Metrics")]
		[SerializeField] private string kernelType = "Hybrid";
		[SerializeField, Min(1)] private int baseProcessLimit = 32;
		[SerializeField, Range(0f, 100f)] private float baseCrashRisk = 9f;
		[SerializeField, Min(0f)] private float baseResponseTimeMs = 16f;

		[SerializeField, Min(0)] private int processManagerLimitBonus = 32;
		[SerializeField, Min(0)] private int schedulerLimitBonus = 16;
		[SerializeField, Min(0)] private int memoryManagerLimitBonus = 16;

		[SerializeField, Range(0f, 100f)] private float memoryManagerCrashRiskReduction = 3f;
		[SerializeField, Range(0f, 100f)] private float schedulerCrashRiskReduction = 2f;
		[SerializeField, Range(0f, 100f)] private float recoveryCrashRiskReduction = 3f;

		[SerializeField, Min(0f)] private float processManagerResponseReductionMs = 2f;
		[SerializeField, Min(0f)] private float schedulerResponseReductionMs = 4f;
		[SerializeField, Min(0f)] private float memoryManagerResponseReductionMs = 1f;

		[Header("Security Metrics")]
		[SerializeField, Min(0)] private int baseVulnerabilities = 7;
		[SerializeField, Range(0f, 100f)] private float baseEncryptionPercent = 8f;
		[SerializeField] private string baseFirewallMode = "None";
		[SerializeField] private string activeFirewallMode = "Basic";

		[SerializeField, Min(0)] private int firewallVulnerabilityReduction = 2;
		[SerializeField, Min(0)] private int permissionsVulnerabilityReduction = 1;
		[SerializeField, Min(0)] private int antivirusVulnerabilityReduction = 3;
		[SerializeField, Min(0)] private int encryptionVulnerabilityReduction = 2;
		[SerializeField, Range(0f, 100f)] private float encryptionPercentBonus = 36f;

		[Header("Control Metrics")]
		[SerializeField, Range(0f, 100f)] private float baseDeviceSupport = 34f;
		[SerializeField, Min(0f)] private float baseInputDelayMs = 18f;
		[SerializeField] private string baseAudioLayer = "None";
		[SerializeField] private string activeAudioLayer = "Basic";
		[SerializeField, Range(0f, 100f)] private float basePowerControl = 18f;

		[SerializeField, Range(0f, 100f)] private float displayDeviceSupportBonus = 14f;
		[SerializeField, Range(0f, 100f)] private float soundDeviceSupportBonus = 6f;
		[SerializeField, Range(0f, 100f)] private float devicesSupportBonus = 28f;
		[SerializeField, Range(0f, 100f)] private float powerControlBonus = 34f;

		[SerializeField, Min(0f)] private float displayInputDelayReductionMs = 2f;
		[SerializeField, Min(0f)] private float devicesInputDelayReductionMs = 3f;
		[SerializeField, Min(0f)] private float powerInputDelayReductionMs = 1f;

		[Header("Refresh")]
		[SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

		[Header("Runtime Variation")]
		[SerializeField] private OsMetricVariationMode variationMode = OsMetricVariationMode.SmoothNoise;
		[SerializeField] private int variationSeed = 152;
		[SerializeField, Min(0.01f)] private float noiseSpeed = 0.7f;
		[SerializeField, Range(0f, 1f)] private float smoothing = 0.35f;

		[SerializeField, Range(0f, 20f)] private float stabilityJitter = 2f;
		[SerializeField, Range(0f, 20f)] private float loadJitter = 3f;
		[SerializeField, Range(0f, 20f)] private float memoryJitter = 2f;
		[SerializeField, Range(0f, 20f)] private float storageJitter = 1f;
		[SerializeField, Range(0f, 5f)] private float networkJitterMbps = 0.12f;

		[SerializeField, Range(0f, 5f)] private float dataReadSpeedJitterMb = 0.15f;
		[SerializeField, Range(0f, 10f)] private float dataSafetyJitter = 2f;
		[SerializeField, Range(0f, 10f)] private float dataIndexSizeJitterMb = 1f;

		[SerializeField, Range(0f, 10f)] private float kernelCrashRiskJitter = 1f;
		[SerializeField, Range(0f, 10f)] private float kernelResponseTimeJitterMs = 1f;

		[SerializeField, Range(0f, 4f)] private float securityVulnerabilityJitter = 1f;
		[SerializeField, Range(0f, 8f)] private float securityEncryptionJitter = 2f;

		[SerializeField, Range(0f, 8f)] private float controlDeviceSupportJitter = 2f;
		[SerializeField, Range(0f, 4f)] private float controlInputDelayJitterMs = 1f;
		[SerializeField, Range(0f, 8f)] private float controlPowerJitter = 2f;

		[Header("Node Metric Rules")]
		[SerializeField] private NodeMetricRule[] metricRules;

		public string RootNodeDefinitionId => rootNodeDefinitionId;

		public float BaseStability => baseStability;
		public float BaseSystemLoad => baseSystemLoad;
		public float BaseMemoryUsage => baseMemoryUsage;
		public float BaseStorageUsage => baseStorageUsage;
		public float BaseNetworkSpeedMbps => baseNetworkSpeedMbps;
		public OsNetworkEraBenchmark[] NetworkEraBenchmarks => networkEraBenchmarks;

		public float MissingMainBranchPenalty => missingMainBranchPenalty;
		public float HighLoadStabilityPenalty => highLoadStabilityPenalty;
		public float HighMemoryStabilityPenalty => highMemoryStabilityPenalty;

		public string DataFileSystemName => dataFileSystemName;
		public float BaseDataReadSpeedMb => baseDataReadSpeedMb;
		public float BaseDataSafety => baseDataSafety;
		public float BaseDataIndexSizeMb => baseDataIndexSizeMb;
		public float FileSystemReadSpeedBonusMb => fileSystemReadSpeedBonusMb;
		public float FileSystemSafetyBonus => fileSystemSafetyBonus;
		public float BackupSafetyBonus => backupSafetyBonus;
		public float RecoverySafetyBonus => recoverySafetyBonus;
		public float CompressionIndexMultiplier => compressionIndexMultiplier;

		public string KernelType => kernelType;
		public int BaseProcessLimit => baseProcessLimit;
		public float BaseCrashRisk => baseCrashRisk;
		public float BaseResponseTimeMs => baseResponseTimeMs;
		public int ProcessManagerLimitBonus => processManagerLimitBonus;
		public int SchedulerLimitBonus => schedulerLimitBonus;
		public int MemoryManagerLimitBonus => memoryManagerLimitBonus;
		public float MemoryManagerCrashRiskReduction => memoryManagerCrashRiskReduction;
		public float SchedulerCrashRiskReduction => schedulerCrashRiskReduction;
		public float RecoveryCrashRiskReduction => recoveryCrashRiskReduction;
		public float ProcessManagerResponseReductionMs => processManagerResponseReductionMs;
		public float SchedulerResponseReductionMs => schedulerResponseReductionMs;
		public float MemoryManagerResponseReductionMs => memoryManagerResponseReductionMs;

		public int BaseVulnerabilities => baseVulnerabilities;
		public float BaseEncryptionPercent => baseEncryptionPercent;
		public string BaseFirewallMode => baseFirewallMode;
		public string ActiveFirewallMode => activeFirewallMode;
		public int FirewallVulnerabilityReduction => firewallVulnerabilityReduction;
		public int PermissionsVulnerabilityReduction => permissionsVulnerabilityReduction;
		public int AntivirusVulnerabilityReduction => antivirusVulnerabilityReduction;
		public int EncryptionVulnerabilityReduction => encryptionVulnerabilityReduction;
		public float EncryptionPercentBonus => encryptionPercentBonus;

		public float BaseDeviceSupport => baseDeviceSupport;
		public float BaseInputDelayMs => baseInputDelayMs;
		public string BaseAudioLayer => baseAudioLayer;
		public string ActiveAudioLayer => activeAudioLayer;
		public float BasePowerControl => basePowerControl;
		public float DisplayDeviceSupportBonus => displayDeviceSupportBonus;
		public float SoundDeviceSupportBonus => soundDeviceSupportBonus;
		public float DevicesSupportBonus => devicesSupportBonus;
		public float PowerControlBonus => powerControlBonus;
		public float DisplayInputDelayReductionMs => displayInputDelayReductionMs;
		public float DevicesInputDelayReductionMs => devicesInputDelayReductionMs;
		public float PowerInputDelayReductionMs => powerInputDelayReductionMs;

		public float RefreshInterval => refreshInterval;
		public OsMetricVariationMode VariationMode => variationMode;
		public int VariationSeed => variationSeed;
		public float NoiseSpeed => noiseSpeed;
		public float Smoothing => smoothing;

		public float StabilityJitter => stabilityJitter;
		public float LoadJitter => loadJitter;
		public float MemoryJitter => memoryJitter;
		public float StorageJitter => storageJitter;
		public float NetworkJitterMbps => networkJitterMbps;

		public float DataReadSpeedJitterMb => dataReadSpeedJitterMb;
		public float DataSafetyJitter => dataSafetyJitter;
		public float DataIndexSizeJitterMb => dataIndexSizeJitterMb;

		public float KernelCrashRiskJitter => kernelCrashRiskJitter;
		public float KernelResponseTimeJitterMs => kernelResponseTimeJitterMs;

		public float SecurityVulnerabilityJitter => securityVulnerabilityJitter;
		public float SecurityEncryptionJitter => securityEncryptionJitter;

		public float ControlDeviceSupportJitter => controlDeviceSupportJitter;
		public float ControlInputDelayJitterMs => controlInputDelayJitterMs;
		public float ControlPowerJitter => controlPowerJitter;

		public NodeMetricRule[] MetricRules => metricRules;

		private void Reset()
		{
			FillDefaultRules();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			refreshInterval = Mathf.Max(0.05f, refreshInterval);
			noiseSpeed = Mathf.Max(0.01f, noiseSpeed);
			baseProcessLimit = Mathf.Max(1, baseProcessLimit);
			baseResponseTimeMs = Mathf.Max(0f, baseResponseTimeMs);
			baseVulnerabilities = Mathf.Max(0, baseVulnerabilities);
			baseInputDelayMs = Mathf.Max(0f, baseInputDelayMs);
		}
#endif

		[ContextMenu("Fill Default Rules")]
		public void FillDefaultRules()
		{
			metricRules = new[]
			{
				new NodeMetricRule("system_core", 12f, 4f, 5f, 3f, 0.20f),

				new NodeMetricRule("data", 7f, 4f, 6f, 9f, 0.05f),
				new NodeMetricRule("kernel", 16f, 8f, 10f, 3f, 0f),
				new NodeMetricRule("security", 8f, 5f, 4f, 2f, 0.10f),
				new NodeMetricRule("control", 4f, 6f, 4f, 2f, 0.30f),

				new NodeMetricRule("file_system", 6f, 4f, 3f, 10f, 0f),
				new NodeMetricRule("backup", 4f, 3f, 3f, 12f, 0f),
				new NodeMetricRule("compression", 2f, 6f, 2f, -5f, 0f),
				new NodeMetricRule("recovery", 3f, 3f, 2f, 4f, 0f),

				new NodeMetricRule("firewall", 5f, 4f, 3f, 1f, -0.05f),
				new NodeMetricRule("permissions", 4f, 3f, 2f, 1f, 0f),
				new NodeMetricRule("antivirus", 6f, 8f, 7f, 3f, -0.10f),
				new NodeMetricRule("encryption", 5f, 7f, 5f, 2f, -0.05f),

				new NodeMetricRule("memory_manager", 10f, 5f, -8f, 1f, 0f),
				new NodeMetricRule("process_manager", 8f, 6f, 4f, 1f, 0f),
				new NodeMetricRule("scheduler", 7f, 4f, 3f, 1f, 0f),
				new NodeMetricRule("kernel_recovery", 6f, 3f, 3f, 2f, 0f),

				new NodeMetricRule("display_manager", 3f, 7f, 5f, 1f, 0f),
				new NodeMetricRule("sound", 2f, 3f, 2f, 1f, 0f),
				new NodeMetricRule("devices", 3f, 4f, 3f, 1f, 0f),
				new NodeMetricRule("power", 3f, 2f, 1f, 1f, 0f),

				new NodeMetricRule("time", 0f, 1f, 1f, 0f, 0f),
				new NodeMetricRule("news", 0f, 1f, 1f, 0f, 0f),
				new NodeMetricRule("era_news", 0f, 1f, 1f, 0f, 0f),
				new NodeMetricRule("user_reports", 0f, 0f, 0f, 0f, 0f)
			};
		}

		public NodeMetricRule GetRule(string definitionId)
		{
			if (metricRules != null)
			{
				foreach (NodeMetricRule rule in metricRules)
				{
					if (rule.Matches(definitionId))
					{
						return rule;
					}
				}
			}

			return NodeMetricRule.Empty(definitionId);
		}
	}

	[Serializable]
	public struct OsRuntimeMetrics
	{
		public float StabilityPercent { get; }
		public float SystemLoadPercent { get; }
		public float MemoryUsagePercent { get; }
		public float StorageUsagePercent { get; }
		public float NetworkSpeedMbps { get; }
		public float NetworkEraScorePercent { get; }
		public string NetworkEraStatus { get; }

		public string StabilityText => $"{Mathf.RoundToInt(StabilityPercent)}%";
		public string SystemLoadText => $"{Mathf.RoundToInt(SystemLoadPercent)}%";
		public string MemoryUsageText => $"{Mathf.RoundToInt(MemoryUsagePercent)}%";
		public string StorageUsageText => $"{Mathf.RoundToInt(StorageUsagePercent)}%";
		public string NetworkSpeedText => $"{NetworkSpeedMbps:0.00}Mbps";

		public OsRuntimeMetrics(
			float stabilityPercent,
			float systemLoadPercent,
			float memoryUsagePercent,
			float storageUsagePercent,
			float networkSpeedMbps)
			: this(
				stabilityPercent,
				systemLoadPercent,
				memoryUsagePercent,
				storageUsagePercent,
				networkSpeedMbps,
				0f,
				string.Empty)
		{
		}

		public OsRuntimeMetrics(
			float stabilityPercent,
			float systemLoadPercent,
			float memoryUsagePercent,
			float storageUsagePercent,
			float networkSpeedMbps,
			float networkEraScorePercent,
			string networkEraStatus)
		{
			StabilityPercent = stabilityPercent;
			SystemLoadPercent = systemLoadPercent;
			MemoryUsagePercent = memoryUsagePercent;
			StorageUsagePercent = storageUsagePercent;
			NetworkSpeedMbps = networkSpeedMbps;
			NetworkEraScorePercent = networkEraScorePercent;
			NetworkEraStatus = networkEraStatus ?? string.Empty;
		}

		public static OsRuntimeMetrics Lerp(OsRuntimeMetrics from, OsRuntimeMetrics to, float t)
		{
			t = Mathf.Clamp01(t);

			return new OsRuntimeMetrics(
				Mathf.Lerp(from.StabilityPercent, to.StabilityPercent, t),
				Mathf.Lerp(from.SystemLoadPercent, to.SystemLoadPercent, t),
				Mathf.Lerp(from.MemoryUsagePercent, to.MemoryUsagePercent, t),
				Mathf.Lerp(from.StorageUsagePercent, to.StorageUsagePercent, t),
				Mathf.Lerp(from.NetworkSpeedMbps, to.NetworkSpeedMbps, t),
				Mathf.Lerp(from.NetworkEraScorePercent, to.NetworkEraScorePercent, t),
				to.NetworkEraStatus
			);
		}
	}

	[Serializable]
	public struct OsDataRuntimeMetrics
	{
		public string FileSystemName { get; }
		public float ReadSpeedMbPerSecond { get; }
		public float WriteSpeedMbPerSecond { get; }
		public float DataSafetyPercent { get; }
		public float FragmentationPercent { get; }
		public float IndexSizeMb { get; }

		public string FileSystemText => string.IsNullOrWhiteSpace(FileSystemName) ? "None" : FileSystemName;
		public string ReadSpeedText => $"{ReadSpeedMbPerSecond:0.0}MB/s";
		public string WriteSpeedText => $"{WriteSpeedMbPerSecond:0.0}MB/s";
		public string DataSafetyText => $"{Mathf.RoundToInt(DataSafetyPercent)}%";
		public string FragmentationText => $"{Mathf.RoundToInt(FragmentationPercent)}%";
		public string IndexSizeText => $"{Mathf.RoundToInt(IndexSizeMb)}Mb";

		public OsDataRuntimeMetrics(
			string fileSystemName,
			float readSpeedMbPerSecond,
			float writeSpeedMbPerSecond,
			float dataSafetyPercent,
			float fragmentationPercent,
			float indexSizeMb)
		{
			FileSystemName = fileSystemName;
			ReadSpeedMbPerSecond = readSpeedMbPerSecond;
			WriteSpeedMbPerSecond = writeSpeedMbPerSecond;
			DataSafetyPercent = dataSafetyPercent;
			FragmentationPercent = fragmentationPercent;
			IndexSizeMb = indexSizeMb;
		}

		public static OsDataRuntimeMetrics Lerp(OsDataRuntimeMetrics from, OsDataRuntimeMetrics to, float t)
		{
			t = Mathf.Clamp01(t);

			return new OsDataRuntimeMetrics(
				to.FileSystemName,
				Mathf.Lerp(from.ReadSpeedMbPerSecond, to.ReadSpeedMbPerSecond, t),
				Mathf.Lerp(from.WriteSpeedMbPerSecond, to.WriteSpeedMbPerSecond, t),
				Mathf.Lerp(from.DataSafetyPercent, to.DataSafetyPercent, t),
				Mathf.Lerp(from.FragmentationPercent, to.FragmentationPercent, t),
				Mathf.Lerp(from.IndexSizeMb, to.IndexSizeMb, t)
			);
		}
	}

	[Serializable]
	public struct OsKernelRuntimeMetrics
	{
		public string KernelType { get; }
		public int ProcessLimit { get; }
		public float CrashRiskPercent { get; }
		public float LeakRiskPercent { get; }
		public float ResponseTimeMs { get; }

		public string KernelTypeText => string.IsNullOrWhiteSpace(KernelType) ? "Unknown" : KernelType;
		public string ProcessLimitText => ProcessLimit.ToString();
		public string CrashRiskText => $"{Mathf.RoundToInt(CrashRiskPercent)}%";
		public string LeakRiskText => $"{Mathf.RoundToInt(LeakRiskPercent)}%";
		public string ResponseTimeText => $"{Mathf.RoundToInt(ResponseTimeMs)}ms";

		public OsKernelRuntimeMetrics(
			string kernelType,
			int processLimit,
			float crashRiskPercent,
			float leakRiskPercent,
			float responseTimeMs)
		{
			KernelType = kernelType;
			ProcessLimit = processLimit;
			CrashRiskPercent = crashRiskPercent;
			LeakRiskPercent = leakRiskPercent;
			ResponseTimeMs = responseTimeMs;
		}

		public static OsKernelRuntimeMetrics Lerp(OsKernelRuntimeMetrics from, OsKernelRuntimeMetrics to, float t)
		{
			t = Mathf.Clamp01(t);

			return new OsKernelRuntimeMetrics(
				to.KernelType,
				Mathf.RoundToInt(Mathf.Lerp(from.ProcessLimit, to.ProcessLimit, t)),
				Mathf.Lerp(from.CrashRiskPercent, to.CrashRiskPercent, t),
				Mathf.Lerp(from.LeakRiskPercent, to.LeakRiskPercent, t),
				Mathf.Lerp(from.ResponseTimeMs, to.ResponseTimeMs, t)
			);
		}
	}

	[Serializable]
	public struct OsSecurityRuntimeMetrics
	{
		public string ThreatLevel { get; }
		public string FirewallMode { get; }
		public int Vulnerabilities { get; }
		public float FalseBlocksPercent { get; }
		public float EncryptionPercent { get; }

		public string ThreatLevelText => string.IsNullOrWhiteSpace(ThreatLevel) ? "Unknown" : ThreatLevel;
		public string FirewallText => string.IsNullOrWhiteSpace(FirewallMode) ? "None" : FirewallMode;
		public string VulnerabilitiesText => Vulnerabilities.ToString();
		public string FalseBlocksText => $"{Mathf.RoundToInt(FalseBlocksPercent)}%";
		public string EncryptionText => $"{Mathf.RoundToInt(EncryptionPercent)}%";

		public OsSecurityRuntimeMetrics(
			string threatLevel,
			string firewallMode,
			int vulnerabilities,
			float falseBlocksPercent,
			float encryptionPercent)
		{
			ThreatLevel = threatLevel;
			FirewallMode = firewallMode;
			Vulnerabilities = vulnerabilities;
			FalseBlocksPercent = falseBlocksPercent;
			EncryptionPercent = encryptionPercent;
		}

		public static OsSecurityRuntimeMetrics Lerp(OsSecurityRuntimeMetrics from, OsSecurityRuntimeMetrics to, float t)
		{
			t = Mathf.Clamp01(t);

			return new OsSecurityRuntimeMetrics(
				to.ThreatLevel,
				to.FirewallMode,
				Mathf.RoundToInt(Mathf.Lerp(from.Vulnerabilities, to.Vulnerabilities, t)),
				Mathf.Lerp(from.FalseBlocksPercent, to.FalseBlocksPercent, t),
				Mathf.Lerp(from.EncryptionPercent, to.EncryptionPercent, t)
			);
		}
	}

	[Serializable]
	public struct OsControlRuntimeMetrics
	{
		public float DeviceSupportPercent { get; }
		public float InputDelayMs { get; }
		public string AudioLayer { get; }
		public float GpuUsagePercent { get; }
		public float PowerControlPercent { get; }

		public string DeviceSupportText => $"{Mathf.RoundToInt(DeviceSupportPercent)}%";
		public string InputDelayText => $"{Mathf.RoundToInt(InputDelayMs)}ms";
		public string AudioLayerText => string.IsNullOrWhiteSpace(AudioLayer) ? "None" : AudioLayer;
		public string GpuUsageText => $"{Mathf.RoundToInt(GpuUsagePercent)}%";
		public string PowerControlText => $"{Mathf.RoundToInt(PowerControlPercent)}%";

		public OsControlRuntimeMetrics(
			float deviceSupportPercent,
			float inputDelayMs,
			string audioLayer,
			float gpuUsagePercent,
			float powerControlPercent)
		{
			DeviceSupportPercent = deviceSupportPercent;
			InputDelayMs = inputDelayMs;
			AudioLayer = audioLayer;
			GpuUsagePercent = gpuUsagePercent;
			PowerControlPercent = powerControlPercent;
		}

		public static OsControlRuntimeMetrics Lerp(OsControlRuntimeMetrics from, OsControlRuntimeMetrics to, float t)
		{
			t = Mathf.Clamp01(t);

			return new OsControlRuntimeMetrics(
				Mathf.Lerp(from.DeviceSupportPercent, to.DeviceSupportPercent, t),
				Mathf.Lerp(from.InputDelayMs, to.InputDelayMs, t),
				to.AudioLayer,
				Mathf.Lerp(from.GpuUsagePercent, to.GpuUsagePercent, t),
				Mathf.Lerp(from.PowerControlPercent, to.PowerControlPercent, t)
			);
		}
	}

	[Serializable]
	public struct NodeMetricRule
	{
		[SerializeField] private string nodeDefinitionId;
		[SerializeField] private float stabilityBonus;
		[SerializeField] private float loadCost;
		[SerializeField] private float memoryCost;
		[SerializeField] private float storageCost;
		[SerializeField] private float networkSpeedMbpsBonus;

		public float StabilityBonus => stabilityBonus;
		public float LoadCost => loadCost;
		public float MemoryCost => memoryCost;
		public float StorageCost => storageCost;
		public float NetworkSpeedMbpsBonus => networkSpeedMbpsBonus;

		public NodeMetricRule(string nodeDefinitionId, float stabilityBonus, float loadCost, float memoryCost, float storageCost, float networkSpeedMbpsBonus)
		{
			this.nodeDefinitionId = nodeDefinitionId;
			this.stabilityBonus = stabilityBonus;
			this.loadCost = loadCost;
			this.memoryCost = memoryCost;
			this.storageCost = storageCost;
			this.networkSpeedMbpsBonus = networkSpeedMbpsBonus;
		}

		public bool Matches(string definitionId)
		{
			return string.Equals(nodeDefinitionId, definitionId, StringComparison.OrdinalIgnoreCase);
		}

		public static NodeMetricRule Empty(string nodeDefinitionId)
		{
			return new NodeMetricRule(nodeDefinitionId, 0f, 1f, 1f, 1f, 0f);
		}
	}
}
