using System;
using UnityEngine;

namespace Game.UI.Notifications
{
	[Serializable]
	public sealed class NotificationEvent
	{
		[SerializeField] private string title;
		[SerializeField] private string body;
		[SerializeField] private NotificationStatus status;
		[SerializeField] private NotificationAction primaryAction;
		[SerializeField] private NotificationAction secondaryAction;

		public string Title => title ?? string.Empty;
		public string Body => body ?? string.Empty;
		public NotificationStatus Status => status;
		public NotificationAction PrimaryAction => primaryAction;
		public NotificationAction SecondaryAction => secondaryAction;

		public NotificationEvent()
		{
		}

		public NotificationEvent(
			string title,
			string body,
			NotificationStatus status = NotificationStatus.Info,
			NotificationAction primaryAction = null,
			NotificationAction secondaryAction = null)
		{
			this.title = title;
			this.body = body;
			this.status = status;
			this.primaryAction = primaryAction;
			this.secondaryAction = secondaryAction;
		}
	}
}
