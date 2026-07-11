using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Game.UI.Notifications
{
	public sealed class NotificationButtonBinder
	{
		private readonly ButtonManager buttonManager;
		private readonly UIManagerButton uiManagerButton;
		private readonly UnityAction clicked;

		public NotificationButtonBinder(ButtonManager buttonManager, UnityAction clicked)
		{
			this.buttonManager = buttonManager;
			this.clicked = clicked;

			if (buttonManager != null)
			{
				uiManagerButton = buttonManager.GetComponent<UIManagerButton>();
			}
		}

		public void Bind(NotificationAction action)
		{
			if (buttonManager == null)
			{
				return;
			}

			Unbind();

			bool hasAction = action != null && action.IsSpecified;
			buttonManager.gameObject.SetActive(hasAction);

			if (!hasAction)
			{
				return;
			}

			if (uiManagerButton != null)
			{
				uiManagerButton.overrideColors = true;
			}

			buttonManager.enableText = true;
			Game.UI.RuntimeTextRefresh.SetButtonText(buttonManager, action.Label);
			buttonManager.onClick.AddListener(clicked);
		}

		public void Unbind()
		{
			if (buttonManager != null && clicked != null)
			{
				buttonManager.onClick.RemoveListener(clicked);
			}
		}

		public static void SetTextColor(ButtonManager manager, Color color)
		{
			if (manager == null)
			{
				return;
			}

			SetColor(manager.normalText, color);
			SetColor(manager.highlightedText, color);
			SetColor(manager.disabledText, color);
		}

		private static void SetColor(TMP_Text text, Color color)
		{
			if (text != null)
			{
				Game.UI.RuntimeTextRefresh.SetColor(text, color);
			}
		}
	}
}
