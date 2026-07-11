using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	internal sealed class NodeViewVisualPresenter
	{
		private readonly Image backgroundImage;
		private readonly Image headerImage;
		private readonly Image accentImage;
		private readonly TMP_Text titleText;
		private readonly NodeDragController dragController;
		private readonly float headerHeight;

		public NodeViewVisualPresenter(
			Image backgroundImage,
			Image headerImage,
			Image accentImage,
			TMP_Text titleText,
			NodeDragController dragController,
			float headerHeight)
		{
			this.backgroundImage = backgroundImage;
			this.headerImage = headerImage;
			this.accentImage = accentImage;
			this.titleText = titleText;
			this.dragController = dragController;
			this.headerHeight = headerHeight;
		}

		public void SetTitle(string title)
		{
			if (titleText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(titleText, title);
			}
		}

		public void SetVisual(Color backgroundColor, Color headerColor, Color accentColor)
		{
			if (backgroundImage != null)
			{
				backgroundImage.color = backgroundColor;
			}

			if (headerImage != null)
			{
				headerImage.color = headerColor;
			}

			if (accentImage != null)
			{
				accentImage.color = accentColor;
			}
		}

		public void SetCanDrag(bool canDrag)
		{
			if (dragController != null)
			{
				dragController.SetCanDrag(canDrag);
			}
		}

		public void ApplyHeaderLayout()
		{
			if (headerImage == null)
			{
				return;
			}

			RectTransform rect = headerImage.rectTransform;

			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.pivot = new Vector2(0.5f, 1f);
			rect.anchoredPosition = Vector2.zero;
			rect.sizeDelta = new Vector2(0f, headerHeight);
		}
	}
}
