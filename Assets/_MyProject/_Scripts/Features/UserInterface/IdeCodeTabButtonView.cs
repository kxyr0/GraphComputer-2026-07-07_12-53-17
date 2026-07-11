using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeCodeTabButtonView : MonoBehaviour
	{
		[SerializeField] private Button selectButton;
		[SerializeField] private Button closeButton;
		[SerializeField] private Image backgroundImage;
		[SerializeField] private TMP_Text titleText;

		public string DocumentId { get; private set; }

		private UnityAction selectAction;
		private UnityAction closeAction;

		private void Reset()
		{
			selectButton = GetComponent<Button>();
			backgroundImage = GetComponent<Image>();
			titleText = GetComponentInChildren<TMP_Text>(true);

			Button[] buttons = GetComponentsInChildren<Button>(true);

			foreach (Button button in buttons)
			{
				if (button != selectButton)
				{
					closeButton = button;
					break;
				}
			}
		}

		private void OnDestroy()
		{
			ClearListeners();
		}

		public void Bind(
			string documentId,
			string title,
			UnityAction onSelect,
			UnityAction onClose)
		{
			ClearListeners();

			DocumentId = documentId;
			name = $"CodeTab_{documentId}";

			if (titleText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(titleText, title);
			}

			selectAction = onSelect;
			closeAction = onClose;

			if (selectButton != null)
			{
				selectButton.onClick.AddListener(selectAction);
			}

			if (closeButton != null)
			{
				closeButton.onClick.AddListener(closeAction);
			}

			RefreshVisual();
		}

		public void SetSelected(
			bool selected,
			Color selectedBackground,
			Color normalBackground,
			Color selectedText,
			Color normalText)
		{
			if (backgroundImage != null)
			{
				backgroundImage.color = selected
					? selectedBackground
					: normalBackground;
			}

			if (titleText != null)
			{
				titleText.color = selected
					? selectedText
					: normalText;
			}

			RefreshVisual();
		}

		public void RefreshVisual()
		{
			if (titleText != null)
			{
				Game.UI.RuntimeTextRefresh.Refresh(titleText);
			}

			if (transform is RectTransform rectTransform)
			{
				LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
			}
		}

		private void ClearListeners()
		{
			if (selectButton != null && selectAction != null)
			{
				selectButton.onClick.RemoveListener(selectAction);
			}

			if (closeButton != null && closeAction != null)
			{
				closeButton.onClick.RemoveListener(closeAction);
			}

			selectAction = null;
			closeAction = null;
		}
	}
}
