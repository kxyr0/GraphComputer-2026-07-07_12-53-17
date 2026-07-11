using Game.Nodes.Presentation;
using UnityEngine;
using VContainer;

namespace Game.Nodes.Runtime
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NodeView))]
	public sealed class OsBranchNodeRuntime : MonoBehaviour
	{
		[SerializeField] private OsRuntimeMetricsService metricsService;
		[SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

		private NodeView nodeView;
		private float refreshTimer;

		[Inject]
		private void Construct(OsRuntimeMetricsService runtimeMetricsService)
		{
			if (metricsService == null)
			{
				metricsService = runtimeMetricsService;
			}
		}

		private void Awake()
		{
			nodeView = GetComponent<NodeView>();
		}

		private void Start()
		{
			ResolveServices();
			Refresh();
		}

		private void Update()
		{
			if (!IsSupportedNode())
			{
				return;
			}

			refreshTimer += Time.unscaledDeltaTime;

			if (refreshTimer < refreshInterval)
			{
				return;
			}

			refreshTimer = 0f;
			Refresh();
		}

		private void ResolveServices()
		{
			if (metricsService == null)
			{
				metricsService = FindFirstObjectByType<OsRuntimeMetricsService>();
			}
		}

		private void Refresh()
		{
			if (metricsService == null)
			{
				ResolveServices();
			}

			if (metricsService == null)
			{
				return;
			}

			metricsService.Recalculate();

			switch (GetNodeId())
			{
				case "data":
				case "file_system":
					RefreshDataNode();
					break;

				case "kernel":
				case "memory_manager":
					RefreshKernelNode();
					break;

				case "security":
				case "firewall":
					RefreshSecurityNode();
					break;

				case "control":
				case "display_manager":
					RefreshControlNode();
					break;
			}
		}

		private void RefreshDataNode()
		{
			OsDataRuntimeMetrics data = metricsService.CurrentData;

			nodeView.TrySetStatValue("File System", data.FileSystemText);
			nodeView.TrySetStatValue("Read Speed", data.ReadSpeedText);
			nodeView.TrySetStatValue("Write Speed", data.WriteSpeedText);
			nodeView.TrySetStatValue("Data Safety", data.DataSafetyText);
			nodeView.TrySetStatValue("Fragmentation", data.FragmentationText);
			nodeView.TrySetStatValue("Index Size", data.IndexSizeText);
		}

		private void RefreshKernelNode()
		{
			OsKernelRuntimeMetrics kernel = metricsService.CurrentKernel;

			nodeView.TrySetStatValue("Kernel Type", kernel.KernelTypeText);
			nodeView.TrySetStatValue("Processes Limit", kernel.ProcessLimitText);
			nodeView.TrySetStatValue("Process Limit", kernel.ProcessLimitText);
			nodeView.TrySetStatValue("Crash Risk", kernel.CrashRiskText);
			nodeView.TrySetStatValue("Leak Risk", kernel.LeakRiskText);
			nodeView.TrySetStatValue("Response Time", kernel.ResponseTimeText);
		}

		private void RefreshSecurityNode()
		{
			OsSecurityRuntimeMetrics security = metricsService.CurrentSecurity;

			nodeView.TrySetStatValue("Threat Level", security.ThreatLevelText);
			nodeView.TrySetStatValue("Firewall", security.FirewallText);
			nodeView.TrySetStatValue("Vulnerabilities", security.VulnerabilitiesText);
			nodeView.TrySetStatValue("False Blocks", security.FalseBlocksText);
			nodeView.TrySetStatValue("Encryption", security.EncryptionText);
		}

		private void RefreshControlNode()
		{
			OsControlRuntimeMetrics control = metricsService.CurrentControl;

			nodeView.TrySetStatValue("Device Support", control.DeviceSupportText);
			nodeView.TrySetStatValue("Usability", control.DeviceSupportText);
			nodeView.TrySetStatValue("Input Delay", control.InputDelayText);
			nodeView.TrySetStatValue("Audio Layer", control.AudioLayerText);
			nodeView.TrySetStatValue("GPU Usage", control.GpuUsageText);
			nodeView.TrySetStatValue("Power Control", control.PowerControlText);
		}

		private bool IsSupportedNode()
		{
			string nodeId = GetNodeId();

			return
				nodeId == "data" ||
				nodeId == "file_system" ||
				nodeId == "kernel" ||
				nodeId == "memory_manager" ||
				nodeId == "security" ||
				nodeId == "firewall" ||
				nodeId == "control" ||
				nodeId == "display_manager";
		}

		private string GetNodeId()
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
	}
}
