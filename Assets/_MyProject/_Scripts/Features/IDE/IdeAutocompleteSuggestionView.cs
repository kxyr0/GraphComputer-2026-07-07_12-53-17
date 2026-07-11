using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	public sealed class IdeAutocompleteSuggestionView : MonoBehaviour
	{
		[SerializeField] private Button button;
		[SerializeField] private Image backgroundImage;
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text kindText;

		[SerializeField] private Color normalColor = new(0.08f, 0.09f, 0.13f, 0.96f);
		[SerializeField] private Color selectedColor = new(0.16f, 0.20f, 0.31f, 1f);

		private IdeAutocompleteEntry entry;
		private Action<IdeAutocompleteEntry> clicked;

		private void Reset()
		{
			button = GetComponent<Button>();
			backgroundImage = GetComponent<Image>();
			titleText = GetComponentInChildren<TMP_Text>(true);
		}

		private void OnDestroy()
		{
			if (button != null)
			{
				button.onClick.RemoveListener(HandleClick);
			}
		}

		public void Bind(
			IdeAutocompleteEntry autocompleteEntry,
			bool selected,
			Action<IdeAutocompleteEntry> onClicked)
		{
			entry = autocompleteEntry;
			clicked = onClicked;

			if (titleText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(titleText, entry.DisplayText);
			}

			if (kindText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(kindText, entry.Kind);
			}

			if (backgroundImage != null)
			{
				backgroundImage.color = selected ? selectedColor : normalColor;
			}

			if (button != null)
			{
				button.onClick.RemoveListener(HandleClick);
				button.onClick.AddListener(HandleClick);
			}

			gameObject.SetActive(true);
		}

		private void HandleClick()
		{
			clicked?.Invoke(entry);
		}
	}
}
