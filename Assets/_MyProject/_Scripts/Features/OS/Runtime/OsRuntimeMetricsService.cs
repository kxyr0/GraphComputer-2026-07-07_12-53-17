using System;
using System.Collections.Generic;
using Game.Nodes.Presentation;
using UnityEngine;

namespace Game.Nodes.Runtime
{
	public sealed class OsRuntimeMetricsService : MonoBehaviour
	{
		[SerializeField] private OsRuntimeMetricsConfig config;
		[SerializeField] private NodeGraphConnectionModel connectionModel;

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
		[SerializeField] private Game.IDE.Presentation.NodeCodeRegistry codeRegistry;
		[SerializeField] private bool requireSuccessfulBuildForNodeBenefits = true;
		[SerializeField] private bool requireSystemCoreBuild = true;
		private void Awake()
		{
			if (connectionModel == null)
			{
				connectionModel = FindFirstObjectByType<NodeGraphConnectionModel>();
			}
			if (codeRegistry == null)
			{
				codeRegistry = FindFirstObjectByType<Game.IDE.Presentation.NodeCodeRegistry>();
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
		private OsSecurityRuntimeMetrics CalculateSecurityMetrics(OsRuntimeMetrics systemMetrics)
		{
			if (!HasReachableDefinition("security"))
			{
				return new OsSecurityRuntimeMetrics("Offline", "None", 99, 0f);
			}

			int vulnerabilities = config.BaseVulnerabilities;
			float encryption = config.BaseEncryptionPercent;
			string firewallMode = config.BaseFirewallMode;

			if (HasReachableDefinition("firewall"))
			{
				firewallMode = config.ActiveFirewallMode;
				vulnerabilities -= config.FirewallVulnerabilityReduction;
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
			encryption += MetricNoise(317f, config.SecurityEncryptionJitter);

			vulnerabilities = Mathf.Max(0, vulnerabilities);
			encryption = Mathf.Clamp(encryption, 0f, 100f);

			return new OsSecurityRuntimeMetrics(
				GetThreatLevel(vulnerabilities),
				firewallMode,
				vulnerabilities,
				encryption
			);
		}

		private OsControlRuntimeMetrics CalculateControlMetrics(OsRuntimeMetrics systemMetrics)
		{
			if (!HasReachableDefinition("control"))
			{
				return new OsControlRuntimeMetrics(0f, 999f, "Offline", 0f);
			}

			float deviceSupport = config.BaseDeviceSupport;
			float inputDelay = config.BaseInputDelayMs;
			string audioLayer = config.BaseAudioLayer;
			float powerControl = config.BasePowerControl;

			if (HasReachableDefinition("display_manager"))
			{
				deviceSupport += config.DisplayDeviceSupportBonus;
				inputDelay -= config.DisplayInputDelayReductionMs;
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
			powerControl += MetricNoise(431f, config.ControlPowerJitter);

			return new OsControlRuntimeMetrics(
				Mathf.Clamp(deviceSupport, 0f, 100f),
				Mathf.Max(1f, inputDelay),
				audioLayer,
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
				if (!IsNodeOperational(nodeView))
				{
					continue;
				}
				string definitionId = GetNodeDefinitionId(nodeView);
				NodeMetricRule rule = config.GetRule(definitionId);

				stability += rule.StabilityBonus;
				load += rule.LoadCost;
				memory += rule.MemoryCost;
				storage += rule.StorageCost;
				network += rule.NetworkSpeedMbpsBonus;

				if (!string.IsNullOrWhiteSpace(definitionId))
				{
					reachableNodeDefinitionIds.Add(definitionId);
				}
			}

			ApplyMainBranchPenalties(ref stability);
			ApplyPressurePenalties(ref stability, load, memory);

			OsRuntimeMetrics calculated = new OsRuntimeMetrics(
				Mathf.Clamp(stability, 0f, 100f),
				Mathf.Clamp(load, 0f, 100f),
				Mathf.Clamp(memory, 0f, 100f),
				Mathf.Clamp(storage, 0f, 100f),
				Mathf.Max(0f, network)
			);

			return ApplySystemVariation(calculated);
		}

		private OsDataRuntimeMetrics CalculateDataMetrics(OsRuntimeMetrics systemMetrics)
		{
			if (!HasReachableDefinition("data"))
			{
				return new OsDataRuntimeMetrics("Offline", 0f, 0f, 0f);
			}

			string fileSystem = config.DataFileSystemName;

			float readSpeed = config.BaseDataReadSpeedMb;
			float safety = config.BaseDataSafety;
			float indexSize = config.BaseDataIndexSizeMb + systemMetrics.StorageUsagePercent * 0.15f;

			if (HasReachableDefinition("file_system"))
			{
				readSpeed += config.FileSystemReadSpeedBonusMb;
				safety += config.FileSystemSafetyBonus;
			}

			if (HasReachableDefinition("backup"))
			{
				safety += config.BackupSafetyBonus;
				indexSize += 4f;
				readSpeed -= 0.05f;
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
			}

			readSpeed += MetricNoise(101f, config.DataReadSpeedJitterMb);
			safety += MetricNoise(113f, config.DataSafetyJitter);
			indexSize += MetricNoise(127f, config.DataIndexSizeJitterMb);

			return new OsDataRuntimeMetrics(
				fileSystem,
				Mathf.Max(0f, readSpeed),
				Mathf.Clamp(safety, 0f, 100f),
				Mathf.Max(0f, indexSize)
			);
		}

		private OsKernelRuntimeMetrics CalculateKernelMetrics(OsRuntimeMetrics systemMetrics)
		{
			if (!HasReachableDefinition("kernel"))
			{
				return new OsKernelRuntimeMetrics("Offline", 0, 100f, 0f);
			}

			int processLimit = config.BaseProcessLimit;
			float crashRisk = config.BaseCrashRisk;
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
				processLimit += config.MemoryManagerLimitBonus;
				crashRisk -= config.MemoryManagerCrashRiskReduction;
				responseTime -= config.MemoryManagerResponseReductionMs;
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
			responseTime += systemMetrics.SystemLoadPercent * 0.06f;

			crashRisk += MetricNoise(211f, config.KernelCrashRiskJitter);
			responseTime += MetricNoise(223f, config.KernelResponseTimeJitterMs);

			return new OsKernelRuntimeMetrics(
				config.KernelType,
				Mathf.Max(1, processLimit),
				Mathf.Clamp(crashRisk, 0f, 100f),
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
				Mathf.Max(0f, input.NetworkSpeedMbps + MetricNoise(67f, config.NetworkJitterMbps))
			);
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
			if (!requireSuccessfulBuildForNodeBenefits)
			{
				return true;
			}

			if (nodeView == null)
			{
				return false;
			}

			if (codeRegistry == null)
			{
				codeRegistry = FindFirstObjectByType<Game.IDE.Presentation.NodeCodeRegistry>();
			}

			if (codeRegistry == null)
			{
				return false;
			}

			string instanceId = GetNodeInstanceId(nodeView);
			string definitionId = GetNodeDefinitionId(nodeView);

			if (codeRegistry.IsBuiltSuccessfully(instanceId))
			{
				return true;
			}

			if (codeRegistry.IsBuiltSuccessfully(definitionId))
			{
				return true;
			}

			return false;
		}
	}
}