using System;
using UnityEngine;

namespace Game.UI.Notifications
{
	[Serializable]
	public sealed class NotificationStatusColorPalette
	{
		[SerializeField] private Color infoColor = new(0.28f, 0.68f, 1f, 1f);
		[SerializeField] private Color successColor = new(0.22f, 1f, 0.18f, 1f);
		[SerializeField] private Color warningColor = new(1f, 0.62f, 0.08f, 1f);
		[SerializeField] private Color errorColor = new(1f, 0.05f, 0.05f, 1f);

		public Color Resolve(NotificationStatus status)
		{
			return status switch
			{
				NotificationStatus.Success => successColor,
				NotificationStatus.Warning => warningColor,
				NotificationStatus.Error => errorColor,
				_ => infoColor
			};
		}
	}
}
