using System;
using UnityEngine.Events;
using UnityEngine;

namespace Game.UI.Notifications
{
	[Serializable]
	public sealed class NotificationAction
	{
		[SerializeField] private string label;
		[SerializeField] private bool closeAfterClick = true;
		[SerializeField] private UnityEvent clicked = new();

		public string Label => label ?? string.Empty;
		public bool CloseAfterClick => closeAfterClick;
		public bool IsSpecified => !string.IsNullOrWhiteSpace(Label);

		public NotificationAction()
		{
		}

		public NotificationAction(string label, bool closeAfterClick = true)
		{
			this.label = label;
			this.closeAfterClick = closeAfterClick;
		}

		public void Invoke()
		{
			clicked?.Invoke();
		}
	}
}
