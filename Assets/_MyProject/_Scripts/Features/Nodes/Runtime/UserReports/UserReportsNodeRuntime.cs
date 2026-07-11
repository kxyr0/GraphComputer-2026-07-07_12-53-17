using Game.Nodes.Presentation;
using Game.World.Reports;
using UnityEngine;
using VContainer;

namespace Game.Nodes.Runtime
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NodeView))]
	public sealed class UserReportsNodeRuntime : MonoBehaviour
	{
		[SerializeField] private UserReportsService reportsService;
		[SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

		private NodeView nodeView;
		private float refreshTimer;

		[Inject]
		private void Construct(UserReportsService service)
		{
			if (reportsService == null)
			{
				reportsService = service;
			}
		}

		private void Awake()
		{
			nodeView = GetComponent<NodeView>();
		}

		private void Start()
		{
			if (!IsUserReportsNode())
			{
				return;
			}

			ResolveServices();
			Refresh();
		}

		private void Update()
		{
			if (!IsUserReportsNode())
			{
				return;
			}

			refreshTimer += UnityEngine.Time.unscaledDeltaTime;

			if (refreshTimer < refreshInterval)
			{
				return;
			}

			refreshTimer = 0f;
			Refresh();
		}

		private void ResolveServices()
		{
			if (reportsService == null)
			{
				reportsService = UserReportsService.GetOrCreate();
			}
		}

		private void Refresh()
		{
			if (!IsUserReportsNode())
			{
				return;
			}

			if (reportsService == null)
			{
				ResolveServices();
			}

			if (reportsService == null)
			{
				nodeView.TrySetStatValue("Active Users", "0");
				nodeView.TrySetStatValue("Mood", "Unknown");
				nodeView.TrySetStatValue("Reports", "0");
				nodeView.TrySetStatValue("Critical", "0");
				return;
			}

			reportsService.EnsureReport();

			nodeView.TrySetStatValue("Active Users", reportsService.ActiveUsers.ToString());
			nodeView.TrySetStatValue("Mood", reportsService.Mood.ToString());
			nodeView.TrySetStatValue("Reports", reportsService.ReportCount.ToString());
			nodeView.TrySetStatValue("Critical", reportsService.CriticalCount.ToString());
		}

		private bool IsUserReportsNode()
		{
			return GetNodeId() == "user_reports";
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
