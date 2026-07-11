using System;
using System.Collections.Generic;
using Game.IDE.Presentation;
using Game.IDE.Runtime;
using Game.Nodes.Presentation;
using Game.World.Time;
using UnityEngine;

namespace Game.Nodes.Runtime
{
	public sealed class OsRuntimeMetricsService : MonoBehaviour
	{
		[SerializeField] private OsRuntimeMetricsConfig config;
		[SerializeField] private NodeGraphConnectionModel connectionModel;
		[SerializeField] private GameTimeService timeService;

		private readonly Dictionary<string, NodeView> nodesByInstanceId =
			new Dictionary<string, NodeView>(StringComparer.OrdinalIgnoreCase);

		private readonly HashSet<string> reachableNodeInstanceIds =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private readonly HashSet<string> reachableNodeDefinitionIds =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private float refreshTimer;
		private bool hasCurrentMetrics;
		private int lastRecalculateFrame = -1;

		public OsRuntimeMetrics Current { get; private set; }
		public OsDataRuntimeMetrics CurrentData { get; private set; }
		public OsKernelRuntimeMetrics CurrentKernel { get; private set; }
		public OsSecurityRuntimeMetrics CurrentSecurity { get; private set; }
		public OsControlRuntimeMetrics CurrentControl { get; private set; }
		[SerializeField] private NodeCodeRegistry codeRegistry;
		[SerializeField] private bool requireSuccessfulBuildForNodeBenefits = true;
		[SerializeField] private bool requireSystemCoreBuild = true;
		private void Awake()
		{
			if (connectionModel == null)
			{
				connectionModel = FindFirstObjectByType<NodeGraphConnectionModel>();
			}
			if (timeService == null)
			{
				timeService = FindFirstObjectByType<GameTimeService>();
			}
			if (codeRegistry == null)
			{
				codeRegistry = FindFirstObjectByType<NodeCodeRegistry>();
			}
			Recalculate();
		}

		private void Update()
		{
			if (config == null)
			{
				return;
			}

			refreshTimer += Time.unscaledDeltaTime;

			if (refreshTimer < config.RefreshInterval)
			{
				return;
			}

			refreshTimer = 0f;
			Recalculate();
		}

		public void Recalculate()
		{
			if (lastRecalculateFrame == Time.frameCount)
			{
				return;
			}

			lastRecalculateFrame = Time.frameCount;

			if (config == null)
			{
				Debug.LogError($"[{nameof(OsRuntimeMetricsService)}] Config is missing.", this);
				return;
			}

			CollectNodes();
			CollectReachableNodes();

			OsRuntimeMetrics systemMetrics = CalculateSystemMetrics();
			OsDataRuntimeMetrics dataMetrics = CalculateDataMetrics(systemMetrics);
			OsKernelRuntimeMetrics kernelMetrics = CalculateKernelMetrics(systemMetrics);
			OsSecurityRuntimeMetrics securityMetrics = CalculateSecurityMetrics(systemMetrics);
			OsControlRuntimeMetrics controlMetrics = CalculateControlMetrics(systemMetrics);

			if (!hasCurrentMetrics)
			{
				Current = systemMetrics;
				CurrentData = dataMetrics;
				CurrentKernel = kernelMetrics;
				CurrentSecurity = securityMetrics;
				CurrentControl = controlMetrics;
				hasCurrentMetrics = true;
				return;
			}

			Current = OsRuntimeMetrics.Lerp(Current, systemMetrics, config.Smoothing);
			CurrentData = OsDataRuntimeMetrics.Lerp(CurrentData, dataMetrics, config.Smoothing);
			CurrentKernel = OsKernelRuntimeMetrics.Lerp(CurrentKernel, kernelMetrics, config.Smoothing);
			CurrentSecurity = OsSecurityRuntimeMetrics.Lerp(CurrentSecurity, securityMetrics, config.Smoothing);
			CurrentControl = OsControlRuntimeMetrics.Lerp(CurrentControl, controlMetrics, config.Smoothing);
		}

		public bool HasOperationalReachableDefinition(string definitionId)
		{
			if (string.IsNullOrWhiteSpace(definitionId))
			{
				return false;
			}

			return reachableNodeDefinitionIds.Contains(definitionId);
		}

		private OsSecurityRuntimeMetrics CalculateSecurityMetrics(OsRuntimeMetrics systemMetrics)
		{
			if (!HasReachableDefinition("security"))
			{
				return new OsSecurityRuntimeMetrics("Offline", "None", 99, 0f, 0f);
			}

			int vulnerabilities = config.BaseVulnerabilities;
			float encryption = config.BaseEncryptionPercent;
			string firewallMode = config.BaseFirewallMode;
			float falseBlocks = 0f;

			if (HasReachableDefinition("firewall"))
			{
				ModuleBuildSettings firewallSettings = GetBuildSettingsForDefinition("firewall");

				firewallMode = ModuleOperatingModeUtility.ToDisplayName(firewallSettings.Mode);
				vulnerabilities -= config.FirewallVulnerabilityReduction;
				ApplyFirewallSecurityTradeoff(
					firewallSettings,
					ref vulnerabilities,
					ref falseBlocks);
			}

			if (HasReachableDefinition("permissions"))
			{
				vulnerabilities -= config.PermissionsVulnerabilityReduction;
			}

			if (HasReachableDefinition("antivirus"))
			{
				vulnerabilities -= config.AntivirusVulnerabilityReduction;
			}

			if (HasReachableDefinition("encryption"))
			{
				vulnerabilities -= config.EncryptionVulnerabilityReduction;
				encryption += config.EncryptionPercentBonus;
			}

			vulnerabilities += Mathf.RoundToInt(MetricNoise(311f, config.SecurityVulnerabilityJitter));
			falseBlocks += MetricNoise(313f, config.SecurityVulnerabilityJitter);
			encryption += MetricNoise(317f, config.SecurityEncryptionJitter);

			vulnerabilities = Mathf.Max(0, vulnerabilities);
			falseBlocks = Mathf.Clamp(falseBlocks, 0f, 100f);
			encryption = Mathf.Clamp(encryption, 0f, 100f);

			return new OsSecurityRuntimeMetrics(
				GetThreatLevel(vulnerabilities),
				firewallMode,
				vulnerabilities,
				falseBlocks,
				encryption
			);
		}

		private OsControlRuntimeMetrics CalculateControlMetrics(OsRuntimeMetrics systemMetrics)
		{
			if (!HasReachableDefinition("control"))
			{
				return new OsControlRuntimeMetrics(0f, 999f, "Offline", 0f, 0f);
			}

			float deviceSupport = config.BaseDeviceSupport;
			float inputDelay = config.BaseInputDelayMs;
			string audioLayer = config.BaseAudioLayer;
			float gpuUsage = systemMetrics.SystemLoadPercent * 0.12f;
			float powerControl = config.BasePowerControl;

			if (HasReachableDefinition("display_manager"))
			{
				ModuleBuildSettings displaySettings = GetBuildSettingsForDefinition("display_manager");

				deviceSupport += config.DisplayDeviceSupportBonus;
				inputDelay -= config.DisplayInputDelayReductionMs;
				gpuUsage += 16f;
				ApplyDisplayManagerControlTradeoff(
					displaySettings,
					ref deviceSupport,
					ref inputDelay,
					ref gpuUsage);
			}

			if (HasReachableDefinition("sound"))
			{
				deviceSupport += config.SoundDeviceSupportBonus;
				audioLayer = config.ActiveAudioLayer;
			}

			if (HasReachableDefinition("devices"))
			{
				deviceSupport += config.DevicesSupportBonus;
				inputDelay -= config.DevicesInputDelayReductionMs;
			}

			if (HasReachableDefinition("power"))
			{
				powerControl += config.PowerControlBonus;
				inputDelay -= config.PowerInputDelayReductionMs;
			}

			inputDelay += systemMetrics.SystemLoadPercent * 0.04f;

			deviceSupport += MetricNoise(411f, config.ControlDeviceSupportJitter);
			inputDelay += MetricNoise(419f, config.ControlInputDelayJitterMs);
			gpuUsage += MetricNoise(421f, config.ControlDeviceSupportJitter);
			powerControl += MetricNoise(431f, config.ControlPowerJitter);

			return new OsControlRuntimeMetrics(
				Mathf.Clamp(deviceSupport, 0f, 100f),
				Mathf.Max(1f, inputDelay),
				audioLayer,
				Mathf.Clamp(gpuUsage, 0f, 100f),
				Mathf.Clamp(powerControl, 0f, 100f)
			);
		}

		private static string GetThreatLevel(int vulnerabilities)
		{
			if (vulnerabilities <= 2)
			{
				return "Low";
			}

			if (vulnerabilities <= 5)
			{
				return "Guarded";
			}

			if (vulnerabilities <= 8)
			{
				return "Elevated";
			}

			return "Critical";
		}

		private static void ApplyFirewallSecurityTradeoff(
			ModuleBuildSettings settings,
			ref int vulnerabilities,
			ref float falseBlocks)
		{
			switch (settings.Mode)
			{
				case ModuleOperatingMode.Strict:
					vulnerabilities -= 2;
					falseBlocks += 18f;
					break;

				case ModuleOperatingMode.Permissive:
					vulnerabilities += 2;
					falseBlocks += 2f;
					break;

				case ModuleOperatingMode.Aggressive:
					vulnerabilities -= 3;
					falseBlocks += 26f;
					break;

				case ModuleOperatingMode.Safe:
					vulnerabilities -= 1;
					falseBlocks += 8f;
					break;

				default:
					falseBlocks += 6f;
					break;
			}

			if (settings.ScanTraffic)
			{
				vulnerabilities -= 1;
				falseBlocks += 4f;
			}

			if (settings.AllowUnknownPorts)
			{
				vulnerabilities += 2;
				falseBlocks -= 6f;
			}
		}

		private static void ApplyDisplayManagerControlTradeoff(
			ModuleBuildSettings settings,
			ref float deviceSupport,
			ref float inputDelay,
			ref float gpuUsage)
		{
			switch (settings.Mode)
			{
				case ModuleOperatingMode.Performance:
				case ModuleOperatingMode.Aggressive:
					deviceSupport += 10f;
					inputDelay -= 2f;
					gpuUsage += 24f;
					break;

				case ModuleOperatingMode.Safe:
					deviceSupport += 3f;
					inputDelay += 1f;
					gpuUsage += 6f;
					break;

				case ModuleOperatingMode.Permissive:
					deviceSupport += 5f;
					gpuUsage += 10f;
					break;
			}
		}

		private OsRuntimeMetrics CalculateSystemMetrics()
		{
			if (requireSystemCoreBuild)
			{
				if (!TryGetRootNode(out NodeView rootNode) || !IsNodeOperational(rootNode))
				{
					return new OsRuntimeMetrics(
						0f,
						0f,
						0f,
						0f,
						0f
					);
				}
			}
			float stability = config.BaseStability;
			float load = config.BaseSystemLoad;
			float memory = config.BaseMemoryUsage;
			float storage = config.BaseStorageUsage;
			float network = config.BaseNetworkSpeedMbps;

			foreach (string nodeInstanceId in reachableNodeInstanceIds)
			{
				if (!nodesByInstanceId.TryGetValue(nodeInstanceId, out NodeView nodeView))
				{
					continue;
				}
				if (!TryGetOperationalState(nodeView, out NodeCodeState nodeCodeState))
				{
					continue;
				}
				string definitionId = GetNodeDefinitionId(nodeView);
				NodeMetricRule rule = config.GetRule(definitionId);
				ModuleBuildSettings settings = GetSettingsOrDefault(nodeCodeState);

				stability += rule.StabilityBonus;
				load += rule.LoadCost;
				memory += rule.MemoryCost;
				storage += rule.StorageCost;
				network += rule.NetworkSpeedMbpsBonus;

				ApplySystemModeTradeoffs(
					definitionId,
					settings,
					ref stability,
					ref load,
					ref memory,
					ref storage,
					ref network);

				if (!string.IsNullOrWhiteSpace(definitionId))
				{
					reachableNodeDefinitionIds.Add(definitionId);
				}
			}

			ApplyMainBranchPenalties(ref stability);
			OsNetworkEraAssessment networkAssessment =
				OsNetworkEraEvaluator.Evaluate(
					network,
					GetCurrentAbsoluteDay(),
					config.NetworkEraBenchmarks);

			stability -= networkAssessment.StabilityPenalty;
			load += networkAssessment.LoadPenalty;

			ApplyPressurePenalties(ref stability, load, memory);

			OsRuntimeMetrics calculated = new OsRuntimeMetrics(
				Mathf.Clamp(stability, 0f, 100f),
				Mathf.Clamp(load, 0f, 100f),
				Mathf.Clamp(memory, 0f, 100f),
				Mathf.Clamp(storage, 0f, 100f),
				Mathf.Max(0f, network),
				networkAssessment.ScorePercent,
				networkAssessment.QualityText
			);

			return ApplySystemVariation(calculated);
		}

		private OsDataRuntimeMetrics CalculateDataMetrics(OsRuntimeMetrics systemMetrics)
		{
			if (!HasReachableDefinition("data"))
			{
				return new OsDataRuntimeMetrics("Offline", 0f, 0f, 0f, 0f, 0f);
			}

			string fileSystem = config.DataFileSystemName;

			float readSpeed = config.BaseDataReadSpeedMb;
			float writeSpeed = config.BaseDataReadSpeedMb * 0.75f;
			float safety = config.BaseDataSafety;
			float fragmentation = 12f + systemMetrics.StorageUsagePercent * 0.08f;
			float indexSize = config.BaseDataIndexSizeMb + systemMetrics.StorageUsagePercent * 0.15f;

			if (HasReachableDefinition("file_system"))
			{
				ModuleBuildSettings fileSystemSettings = GetBuildSettingsForDefinition("file_system");

				readSpeed += config.FileSystemReadSpeedBonusMb;
				writeSpeed += config.FileSystemReadSpeedBonusMb * 0.8f;
				safety += config.FileSystemSafetyBonus;
				fragmentation += 8f;
				indexSize += 3f;
				ApplyFileSystemDataTradeoff(
					fileSystemSettings,
					ref readSpeed,
					ref writeSpeed,
					ref safety,
					ref fragmentation,
					ref indexSize);
			}

			if (HasReachableDefinition("backup"))
			{
				safety += config.BackupSafetyBonus;
				indexSize += 4f;
				readSpeed -= 0.05f;
				writeSpeed -= 0.05f;
			}

			if (HasReachableDefinition("recovery"))
			{
				safety += config.RecoverySafetyBonus;
				indexSize += 2f;
			}

			if (HasReachableDefinition("compression"))
			{
				indexSize *= config.CompressionIndexMultiplier;
				readSpeed -= 0.1f;
				writeSpeed -= 0.08f;
				fragmentation -= 3f;
			}

			readSpeed += MetricNoise(101f, config.DataReadSpeedJitterMb);
			writeSpeed += MetricNoise(107f, config.DataReadSpeedJitterMb);
			safety += MetricNoise(113f, config.DataSafetyJitter);
			fragmentation += MetricNoise(119f, config.DataSafetyJitter);
			indexSize += MetricNoise(127f, config.DataIndexSizeJitterMb);

			return new OsDataRuntimeMetrics(
				fileSystem,
				Mathf.Max(0f, readSpeed),
				Mathf.Max(0f, writeSpeed),
				Mathf.Clamp(safety, 0f, 100f),
				Mathf.Clamp(fragmentation, 0f, 100f),
				Mathf.Max(0f, indexSize)
			);
		}

		private static void ApplyFileSystemDataTradeoff(
			ModuleBuildSettings settings,
			ref float readSpeed,
			ref float writeSpeed,
			ref float safety,
			ref float fragmentation,
			ref float indexSize)
		{
			switch (settings.Mode)
			{
				case ModuleOperatingMode.Performance:
				case ModuleOperatingMode.Aggressive:
					readSpeed += 0.65f;
					writeSpeed += 0.55f;
					safety -= 4f;
					fragmentation += 14f;
					indexSize += 5f;
					break;

				case ModuleOperatingMode.Safe:
					readSpeed -= 0.15f;
					writeSpeed -= 0.1f;
					safety += 9f;
					fragmentation -= 5f;
					indexSize += 2f;
					break;

				case ModuleOperatingMode.Permissive:
					readSpeed += 0.25f;
					writeSpeed += 0.2f;
					fragmentation += 8f;
					break;
			}
		}

		private static void ApplyMemoryManagerKernelTradeoff(
			ModuleBuildSettings settings,
			NodeCodeState state,
			ref int processLimit,
			ref float crashRisk,
			ref float leakRisk,
			ref float responseTime)
		{
			if (state != null)
			{
				leakRisk += state.WarningCount * 3f;
				leakRisk += Mathf.Max(0, state.TechnicalDebtDelta) * 0.45f;
			}

			switch (settings.Mode)
			{
				case ModuleOperatingMode.Aggressive:
				case ModuleOperatingMode.Performance:
					processLimit += 20;
					responseTime -= 2.5f;
					crashRisk += 5f;
					leakRisk += 18f;
					break;

				case ModuleOperatingMode.Safe:
					processLimit += 4;
					responseTime += 0.5f;
					crashRisk -= 2f;
					leakRisk -= 6f;
					break;

				case ModuleOperatingMode.Permissive:
					processLimit += 8;
					leakRisk += 8f;
					break;
			}
		}

		private OsKernelRuntimeMetrics CalculateKernelMetrics(OsRuntimeMetrics systemMetrics)
		{
			if (!HasReachableDefinition("kernel"))
			{
				return new OsKernelRuntimeMetrics("Offline", 0, 100f, 100f, 0f);
			}

			int processLimit = config.BaseProcessLimit;
			float crashRisk = config.BaseCrashRisk;
			float leakRisk = 8f;
			float responseTime = config.BaseResponseTimeMs;

			if (HasReachableDefinition("process_manager"))
			{
				processLimit += config.ProcessManagerLimitBonus;
				responseTime -= config.ProcessManagerResponseReductionMs;
			}

			if (HasReachableDefinition("scheduler"))
			{
				processLimit += config.SchedulerLimitBonus;
				crashRisk -= config.SchedulerCrashRiskReduction;
				responseTime -= config.SchedulerResponseReductionMs;
			}

			if (HasReachableDefinition("memory_manager"))
			{
				ModuleBuildSettings memorySettings = GetBuildSettingsForDefinition("memory_manager");
				NodeCodeState memoryState = GetBuildStateForDefinition("memory_manager");

				processLimit += config.MemoryManagerLimitBonus;
				crashRisk -= config.MemoryManagerCrashRiskReduction;
				leakRisk += 5f;
				responseTime -= config.MemoryManagerResponseReductionMs;
				ApplyMemoryManagerKernelTradeoff(
					memorySettings,
					memoryState,
					ref processLimit,
					ref crashRisk,
					ref leakRisk,
					ref responseTime);
			}

			if (HasReachableDefinition("kernel_recovery") || HasReachableDefinition("recovery"))
			{
				crashRisk -= config.RecoveryCrashRiskReduction;
			}

			if (!HasReachableDefinition("memory_manager"))
			{
				crashRisk += 3f;
			}

			if (!HasReachableDefinition("scheduler"))
			{
				crashRisk += 2f;
			}

			crashRisk += Mathf.Max(0f, systemMetrics.SystemLoadPercent - 60f) * 0.15f;
			leakRisk += Mathf.Max(0f, systemMetrics.MemoryUsagePercent - 65f) * 0.12f;
			responseTime += systemMetrics.SystemLoadPercent * 0.06f;

			crashRisk += MetricNoise(211f, config.KernelCrashRiskJitter);
			leakRisk += MetricNoise(217f, config.KernelCrashRiskJitter);
			responseTime += MetricNoise(223f, config.KernelResponseTimeJitterMs);

			return new OsKernelRuntimeMetrics(
				config.KernelType,
				Mathf.Max(1, processLimit),
				Mathf.Clamp(crashRisk, 0f, 100f),
				Mathf.Clamp(leakRisk, 0f, 100f),
				Mathf.Max(1f, responseTime)
			);
		}

		private OsRuntimeMetrics ApplySystemVariation(OsRuntimeMetrics input)
		{
			return new OsRuntimeMetrics(
				Mathf.Clamp(input.StabilityPercent + MetricNoise(11f, config.StabilityJitter), 0f, 100f),
				Mathf.Clamp(input.SystemLoadPercent + MetricNoise(23f, config.LoadJitter), 0f, 100f),
				Mathf.Clamp(input.MemoryUsagePercent + MetricNoise(37f, config.MemoryJitter), 0f, 100f),
				Mathf.Clamp(input.StorageUsagePercent + MetricNoise(51f, config.StorageJitter), 0f, 100f),
				Mathf.Max(0f, input.NetworkSpeedMbps + MetricNoise(67f, config.NetworkJitterMbps)),
				input.NetworkEraScorePercent,
				input.NetworkEraStatus
			);
		}

		private int GetCurrentAbsoluteDay()
		{
			if (timeService == null)
			{
				timeService = FindFirstObjectByType<GameTimeService>();
			}

			return timeService != null
				? timeService.GetAbsoluteDay()
				: OsNetworkEraBenchmark.ToAbsoluteDay(1995, 1, 1);
		}

		private float MetricNoise(float channel, float amount)
		{
			if (amount <= 0f || config.VariationMode == OsMetricVariationMode.None)
			{
				return 0f;
			}

			if (config.VariationMode == OsMetricVariationMode.RandomOnRefresh)
			{
				return UnityEngine.Random.Range(-amount, amount);
			}

			float time = Time.unscaledTime * config.NoiseSpeed;
			float x = time + config.VariationSeed * 0.137f;
			float y = channel + config.VariationSeed * 0.071f;

			return (Mathf.PerlinNoise(x, y) * 2f - 1f) * amount;
		}

		private bool HasReachableDefinition(string definitionId)
		{
			return reachableNodeDefinitionIds.Contains(definitionId);
		}

		private void CollectNodes()
		{
			nodesByInstanceId.Clear();

			NodeView[] nodeViews = FindObjectsByType<NodeView>(
				FindObjectsInactive.Exclude,
				FindObjectsSortMode.None
			);

			foreach (NodeView nodeView in nodeViews)
			{
				if (nodeView == null)
				{
					continue;
				}

				string instanceId = GetNodeInstanceId(nodeView);

				if (string.IsNullOrWhiteSpace(instanceId))
				{
					continue;
				}

				nodesByInstanceId[instanceId] = nodeView;
			}
		}

		private void CollectReachableNodes()
		{
			reachableNodeInstanceIds.Clear();
			reachableNodeDefinitionIds.Clear();

			string rootInstanceId = FindRootInstanceId();

			if (string.IsNullOrWhiteSpace(rootInstanceId))
			{
				return;
			}

			Queue<string> queue = new Queue<string>();
			queue.Enqueue(rootInstanceId);
			reachableNodeInstanceIds.Add(rootInstanceId);

			while (queue.Count > 0)
			{
				string currentNodeId = queue.Dequeue();

				if (connectionModel == null || connectionModel.Connections == null)
				{
					continue;
				}

				foreach (NodeConnectionData connection in connectionModel.Connections)
				{
					if (connection == null)
					{
						continue;
					}

					if (!string.Equals(
							connection.FromNodeId,
							currentNodeId,
							StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					if (string.IsNullOrWhiteSpace(connection.ToNodeId))
					{
						continue;
					}

					if (reachableNodeInstanceIds.Contains(connection.ToNodeId))
					{
						continue;
					}

					reachableNodeInstanceIds.Add(connection.ToNodeId);
					queue.Enqueue(connection.ToNodeId);
				}
			}
		}
		private bool TryGetRootNode(out NodeView rootNode)
		{
			rootNode = null;

			string rootInstanceId = FindRootInstanceId();

			if (string.IsNullOrWhiteSpace(rootInstanceId))
			{
				return false;
			}

			return nodesByInstanceId.TryGetValue(rootInstanceId, out rootNode);
		}
		private string FindRootInstanceId()
		{
			foreach (KeyValuePair<string, NodeView> pair in nodesByInstanceId)
			{
				NodeView nodeView = pair.Value;

				if (nodeView == null)
				{
					continue;
				}

				string definitionId = GetNodeDefinitionId(nodeView);

				if (string.Equals(
						definitionId,
						config.RootNodeDefinitionId,
						StringComparison.OrdinalIgnoreCase))
				{
					return pair.Key;
				}
			}

			return string.Empty;
		}

		private void ApplyMainBranchPenalties(ref float stability)
		{
			ApplyMissingBranchPenalty(ref stability, "data");
			ApplyMissingBranchPenalty(ref stability, "kernel");
			ApplyMissingBranchPenalty(ref stability, "security");
			ApplyMissingBranchPenalty(ref stability, "control");
		}

		private void ApplyMissingBranchPenalty(ref float stability, string nodeDefinitionId)
		{
			if (reachableNodeDefinitionIds.Contains(nodeDefinitionId))
			{
				return;
			}

			stability -= config.MissingMainBranchPenalty;
		}

		private void ApplyPressurePenalties(ref float stability, float load, float memory)
		{
			if (load > 70f)
			{
				stability -= (load - 70f) * config.HighLoadStabilityPenalty;
			}

			if (memory > 80f)
			{
				stability -= (memory - 80f) * config.HighMemoryStabilityPenalty;
			}
		}

		private static string GetNodeInstanceId(NodeView nodeView)
		{
			if (nodeView == null)
			{
				return string.Empty;
			}

			if (!string.IsNullOrWhiteSpace(nodeView.InstanceId))
			{
				return nodeView.InstanceId;
			}

			if (nodeView.Definition != null)
			{
				return nodeView.Definition.Id;
			}

			if (nodeView.JsonDefinition != null)
			{
				return nodeView.JsonDefinition.id;
			}

			return nodeView.name;
		}

		private static string GetNodeDefinitionId(NodeView nodeView)
		{
			if (nodeView == null)
			{
				return string.Empty;
			}

			if (nodeView.Definition != null)
			{
				return nodeView.Definition.Id;
			}

			if (nodeView.JsonDefinition != null)
			{
				return nodeView.JsonDefinition.id;
			}

			return string.Empty;
		}
		private bool IsNodeOperational(NodeView nodeView)
		{
			return TryGetOperationalState(nodeView, out _);
		}

		private bool TryGetOperationalState(NodeView nodeView, out NodeCodeState state)
		{
			state = null;

			if (nodeView == null)
			{
				return false;
			}

			if (!requireSuccessfulBuildForNodeBenefits)
			{
				return true;
			}

			if (codeRegistry == null)
			{
				codeRegistry = FindFirstObjectByType<NodeCodeRegistry>();
			}

			if (codeRegistry == null)
			{
				return false;
			}

			string instanceId = GetNodeInstanceId(nodeView);
			string definitionId = GetNodeDefinitionId(nodeView);

			if (codeRegistry.TryGetState(instanceId, out state) &&
				state != null &&
				state.LastBuildSucceeded)
			{
				return true;
			}

			if (codeRegistry.TryGetState(definitionId, out state) &&
				state != null &&
				state.LastBuildSucceeded)
			{
				return true;
			}

			state = null;
			return false;
		}

		private ModuleBuildSettings GetBuildSettingsForDefinition(string definitionId)
		{
			NodeCodeState state = GetBuildStateForDefinition(definitionId);

			return state != null
				? state.ModuleSettings
				: ModuleBuildSettings.Balanced();
		}

		private NodeCodeState GetBuildStateForDefinition(string definitionId)
		{
			if (codeRegistry == null)
			{
				codeRegistry = FindFirstObjectByType<NodeCodeRegistry>();
			}

			if (codeRegistry == null ||
				!codeRegistry.TryGetState(definitionId, out NodeCodeState state) ||
				state == null ||
				!state.LastBuildSucceeded)
			{
				return null;
			}

			return state;
		}

		private static ModuleBuildSettings GetSettingsOrDefault(NodeCodeState state)
		{
			return state != null
				? state.ModuleSettings
				: ModuleBuildSettings.Balanced();
		}

		private static void ApplySystemModeTradeoffs(
			string definitionId,
			ModuleBuildSettings settings,
			ref float stability,
			ref float load,
			ref float memory,
			ref float storage,
			ref float network)
		{
			switch ((definitionId ?? string.Empty).ToLowerInvariant())
			{
				case "firewall":
					ApplyFirewallSystemTradeoff(settings, ref stability, ref load, ref network);
					break;

				case "file_system":
					ApplyFileSystemSystemTradeoff(settings, ref stability, ref load, ref storage);
					break;

				case "memory_manager":
					ApplyMemoryManagerSystemTradeoff(settings, ref stability, ref load, ref memory);
					break;

				case "display_manager":
					ApplyDisplayManagerSystemTradeoff(settings, ref stability, ref load, ref memory);
					break;
			}
		}

		private static void ApplyFirewallSystemTradeoff(
			ModuleBuildSettings settings,
			ref float stability,
			ref float load,
			ref float network)
		{
			switch (settings.Mode)
			{
				case ModuleOperatingMode.Strict:
					load += 2f;
					network -= 0.25f;
					break;

				case ModuleOperatingMode.Permissive:
					stability -= 1f;
					network += 0.18f;
					break;

				case ModuleOperatingMode.Aggressive:
					load += 4f;
					network -= 0.35f;
					stability -= 1f;
					break;

				case ModuleOperatingMode.Safe:
					load += 1f;
					network -= 0.12f;
					stability += 1f;
					break;
			}

			if (settings.ScanTraffic)
			{
				load += 2f;
				network -= 0.08f;
			}

			if (settings.AllowUnknownPorts)
			{
				network += 0.12f;
				stability -= 1f;
			}
		}

		private static void ApplyFileSystemSystemTradeoff(
			ModuleBuildSettings settings,
			ref float stability,
			ref float load,
			ref float storage)
		{
			storage += 2f;

			switch (settings.Mode)
			{
				case ModuleOperatingMode.Performance:
				case ModuleOperatingMode.Aggressive:
					load += 2f;
					storage += 4f;
					stability -= 1f;
					break;

				case ModuleOperatingMode.Safe:
					load += 1f;
					storage += 1f;
					stability += 1f;
					break;

				case ModuleOperatingMode.Permissive:
					storage += 3f;
					break;
			}
		}

		private static void ApplyMemoryManagerSystemTradeoff(
			ModuleBuildSettings settings,
			ref float stability,
			ref float load,
			ref float memory)
		{
			memory -= 2f;

			switch (settings.Mode)
			{
				case ModuleOperatingMode.Aggressive:
				case ModuleOperatingMode.Performance:
					load += 3f;
					memory -= 4f;
					stability -= 4f;
					break;

				case ModuleOperatingMode.Safe:
					load += 1f;
					memory -= 1f;
					stability += 2f;
					break;
			}
		}

		private static void ApplyDisplayManagerSystemTradeoff(
			ModuleBuildSettings settings,
			ref float stability,
			ref float load,
			ref float memory)
		{
			load += 2f;
			memory += 1f;

			switch (settings.Mode)
			{
				case ModuleOperatingMode.Performance:
				case ModuleOperatingMode.Aggressive:
					load += 4f;
					memory += 2f;
					stability -= 1f;
					break;

				case ModuleOperatingMode.Safe:
					load -= 1f;
					break;
			}
		}
	}
}
