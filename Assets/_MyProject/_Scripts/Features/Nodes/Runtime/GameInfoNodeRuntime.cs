using Game.Nodes.Presentation;
using Game.World.News;
using Game.World.Time;
using UnityEngine;

namespace Game.Nodes.Runtime
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NodeView))]
	public sealed class GameInfoNodeRuntime : MonoBehaviour
	{
		[SerializeField] private GameTimeService timeService;
		[SerializeField] private EraNewsService newsService;

		[SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

		private NodeView nodeView;
		private float refreshTimer;

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
			if (timeService == null)
			{
				timeService = FindFirstObjectByType<GameTimeService>();
			}

			if (newsService == null)
			{
				newsService = FindFirstObjectByType<EraNewsService>();
			}
		}

		private void Refresh()
		{
			string nodeId = GetNodeId();

			switch (nodeId)
			{
				case "time":
					RefreshTimeNode();
					break;

				case "news":
				case "era_news":
					RefreshNewsNode();
					break;
			}
		}

		private void RefreshTimeNode()
		{
			if (timeService == null)
			{
				ResolveServices();
			}

			if (timeService == null)
			{
				nodeView.TrySetStatValue("Clock", "Missing");
				nodeView.TrySetStatValue("Date", "Missing");
				nodeView.TrySetStatValue("Speed", "Missing");
				nodeView.TrySetStatValue("Status", "No Service");
				return;
			}

			nodeView.TrySetStatValue("Clock", timeService.GetClockText());
			nodeView.TrySetStatValue("Date", timeService.GetDateText());
			nodeView.TrySetStatValue("Speed", timeService.GetSpeedText());
			nodeView.TrySetStatValue("Status", timeService.GetStatusText());
		}

		private void RefreshNewsNode()
		{
			if (newsService == null)
			{
				ResolveServices();
			}

			if (newsService == null)
			{
				nodeView.TrySetStatValue("Date", "Missing");
				nodeView.TrySetStatValue("Headline", "NO SIGNAL");
				nodeView.TrySetStatValue("Description", "News service is missing.");
				return;
			}

			EraNewsEntry news = newsService.GetCurrentNews();

			nodeView.TrySetStatValue("Date", news.DateText);
			nodeView.TrySetStatValue("Headline", news.Headline);
			nodeView.TrySetStatValue("Description", news.Description);
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