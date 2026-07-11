using System;
using Michsky.MUIP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.OS
{
	[Serializable]
	public sealed class OsWindowTabItem
	{
		[SerializeField] private string id;
		[SerializeField] private Button button;
		[SerializeField] private ButtonManager buttonManager;
		[SerializeField] private UIManagerButton uiManagerButton;
		[SerializeField] private Graphic buttonBackground;
		[SerializeField] private Graphic labelGraphic;
		[SerializeField] private RectTransform pageRoot;
		[SerializeField] private CanvasGroup pageGroup;

		private UnityAction cachedAction;

		public string Id => id ?? string.Empty;
		public RectTransform PageRoot => pageRoot;

		public bool Matches(string tabId)
		{
			return string.Equals(id, tabId, StringComparison.OrdinalIgnoreCase);
		}

		public void Initialize(UnityAction onClick)
		{
			ResolveButtonReferences();
			RemoveListener();
			cachedAction = onClick;

			if (buttonManager != null)
			{
				buttonManager.onClick.AddListener(cachedAction);
				return;
			}

			if (button != null)
			{
				button.onClick.AddListener(cachedAction);
			}
		}

		public void RemoveListener()
		{
			if (cachedAction == null)
			{
				return;
			}

			if (buttonManager != null)
			{
				buttonManager.onClick.RemoveListener(cachedAction);
			}

			if (button != null)
			{
				button.onClick.RemoveListener(cachedAction);
			}

			cachedAction = null;
		}

		public CanvasGroup GetOrCreatePageGroup()
		{
			if (pageGroup != null)
			{
				return pageGroup;
			}

			if (pageRoot == null)
			{
				return null;
			}

			pageGroup = pageRoot.GetComponent<CanvasGroup>();

			if (pageGroup == null)
			{
				pageGroup = pageRoot.gameObject.AddComponent<CanvasGroup>();
			}

			return pageGroup;
		}

		public void ApplyButtonVisual(
			bool selected,
			Color selectedButtonColor,
			Color normalButtonColor,
			Color selectedLabelColor,
			Color normalLabelColor)
		{
			ApplyButtonManagerVisual(
				selected,
				selectedButtonColor,
				normalButtonColor,
				selectedLabelColor,
				normalLabelColor
			);

			if (buttonBackground != null)
			{
				buttonBackground.color = selected ? selectedButtonColor : normalButtonColor;
			}

			if (labelGraphic != null)
			{
				labelGraphic.color = selected ? selectedLabelColor : normalLabelColor;
			}
		}

		private void ResolveButtonReferences()
		{
			if (buttonManager == null && button != null)
			{
				buttonManager = button.GetComponent<ButtonManager>();
			}

			if (button == null && buttonManager != null)
			{
				button = buttonManager.GetComponent<Button>();
			}

			if (uiManagerButton == null && buttonManager != null)
			{
				uiManagerButton = buttonManager.GetComponent<UIManagerButton>();
			}
		}

		private void ApplyButtonManagerVisual(
			bool selected,
			Color selectedButtonColor,
			Color normalButtonColor,
			Color selectedLabelColor,
			Color normalLabelColor)
		{
			ResolveButtonReferences();

			if (buttonManager == null)
			{
				return;
			}

			if (uiManagerButton != null)
			{
				uiManagerButton.overrideColors = true;
			}

			Color currentButtonColor = selected ? selectedButtonColor : normalButtonColor;
			Color currentLabelColor = selected ? selectedLabelColor : normalLabelColor;

			SetGraphicColor(buttonManager.normalText, currentLabelColor);
			SetGraphicColor(buttonManager.highlightedText, selectedLabelColor);
			SetGraphicColor(buttonManager.disabledText, normalLabelColor);
			SetGraphicColor(buttonManager.normalImage, currentLabelColor);
			SetGraphicColor(buttonManager.highlightImage, selectedLabelColor);
			SetGraphicColor(buttonManager.disabledImage, normalLabelColor);

			if (uiManagerButton == null)
			{
				return;
			}

			SetGraphicColor(uiManagerButton.normalBackground, currentButtonColor);
			SetGraphicColor(uiManagerButton.highlightedBackground, selectedButtonColor);
			SetGraphicColor(uiManagerButton.disabledBackground, normalButtonColor);
			SetGraphicColor(uiManagerButton.normalText, currentLabelColor);
			SetGraphicColor(uiManagerButton.highlightedText, selectedLabelColor);
			SetGraphicColor(uiManagerButton.disabledText, normalLabelColor);
			SetGraphicColor(uiManagerButton.normalIcon, currentLabelColor);
			SetGraphicColor(uiManagerButton.highlightedIcon, selectedLabelColor);
			SetGraphicColor(uiManagerButton.disabledIcon, normalLabelColor);
		}

		private static void SetGraphicColor(Graphic graphic, Color color)
		{
			if (graphic != null)
			{
				graphic.color = color;
			}
		}
	}
}
