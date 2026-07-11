using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	[AddComponentMenu("Graph Computer/Nodes/Node Spawn Category Item View")]
	[DisallowMultipleComponent]
	public sealed class NodeSpawnCategoryItemView : MonoBehaviour
	{
		[Header("UI")]
		[SerializeField] private Button button;
		[SerializeField] private Image backgroundImage;
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text countText;
		[SerializeField] private TMP_Text arrowText;

		[Header("Visual")]
		[SerializeField] private Color normalColor = new(0.06f, 0.07f, 0.11f, 0.96f);
		[SerializeField] private Color selectedColor = new(0.11f, 0.14f, 0.22f, 1f);

		private string categoryId;
		private Action<string> clicked;

		private void Reset()
		{
			button = GetComponent<Button>();
			backgroundImage = GetComponent<Image>();
			titleText = GetComponentInChildren<TMP_Text>(true);
		}

		private void Awake()
		{
			if (button == null)
			{
				button = GetComponent<Button>();
			}

			if (backgroundImage == null)
			{
				backgroundImage = GetComponent<Image>();
			}

			if (titleText == null)
			{
				titleText = GetComponentInChildren<TMP_Text>(true);
			}
		}

		private void OnDestroy()
		{
			if (button != null)
			{
				button.onClick.RemoveListener(HandleClick);
			}
		}

		public void Bind(
			string id,
			string title,
			int count,
			bool selected,
			Action<string> onClicked)
		{
			categoryId = id;
			clicked = onClicked;

			name = $"Category_{categoryId}";

			bool isBack = categoryId == "__back";

			if (titleText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(titleText, title);
			}

			if (countText != null)
			{
				countText.gameObject.SetActive(!isBack && count >= 0);
				Game.UI.RuntimeTextRefresh.SetText(countText, count.ToString());
			}

			if (arrowText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(arrowText, isBack ? "←" : "›");
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
		}

		private void HandleClick()
		{
			if (string.IsNullOrWhiteSpace(categoryId))
			{
				return;
			}

			clicked?.Invoke(categoryId);
		}
	}
}
