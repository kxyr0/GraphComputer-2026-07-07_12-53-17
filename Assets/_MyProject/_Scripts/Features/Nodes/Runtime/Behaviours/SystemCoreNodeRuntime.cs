using Game.Nodes.Presentation;
using UnityEngine;
using VContainer;

namespace Game.Nodes.Runtime
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NodeView))]
	public sealed class SystemCoreNodeRuntime : MonoBehaviour
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
			if (!IsSystemCoreNode())
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
			if (!IsSystemCoreNode())
			{
				return;
			}

			if (metricsService == null)
			{
				ResolveServices();
			}

			if (metricsService == null)
			{
				nodeView.TrySetStatValue("System Stability", "Missing");
				nodeView.TrySetStatValue("Network Speed", "Missing");
				nodeView.TrySetStatValue("System Load", "Missing");
				nodeView.TrySetStatValue("Memory Usage", "Missing");
				nodeView.TrySetStatValue("Storage Usage", "Missing");
				return;
			}

			metricsService.Recalculate();
			OsRuntimeMetrics metrics = metricsService.Current;

			nodeView.TrySetStatValue("System Stability", metrics.StabilityText);
			nodeView.TrySetStatValue(
				"Network Speed",
				metrics.NetworkSpeedText,
				metrics.NetworkEraStatus);
			nodeView.TrySetStatValue("System Load", metrics.SystemLoadText);
			nodeView.TrySetStatValue("Memory Usage", metrics.MemoryUsageText);
			nodeView.TrySetStatValue("Storage Usage", metrics.StorageUsageText);
		}

		private bool IsSystemCoreNode()
		{
			if (nodeView == null)
			{
				return false;
			}

			if (nodeView.Definition != null)
			{
				return nodeView.Definition.Id == "system_core";
			}

			if (nodeView.JsonDefinition != null)
			{
				return nodeView.JsonDefinition.id == "system_core";
			}

			return false;
		}
	}
}
