using System;
using Game.Nodes.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	[DisallowMultipleComponent]
	public sealed class NodeSpawnMenuItemView : MonoBehaviour
	{
		[Header("UI")]
		[SerializeField] private Button button;
		[SerializeField] private Image backgroundImage;
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text idText;

		[Header("Fallback")]
		[SerializeField] private bool showIdWhenTitleMissing = true;

		private NodeDefinition definition;
		private Action<NodeDefinition> clicked;

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

		public void Bind(NodeDefinition nodeDefinition, Action<NodeDefinition> onClicked)
		{
			definition = nodeDefinition;
			clicked = onClicked;

			if (definition == null)
			{
				name = "SpawnItem_Null";
				SetTitle("Missing Node");
				SetId(string.Empty);
				SetInteractable(false);
				return;
			}

			name = $"SpawnItem_{definition.Id}";

			string title = string.IsNullOrWhiteSpace(definition.Title)
				? definition.Id
				: definition.Title;

			if (string.IsNullOrWhiteSpace(title) && showIdWhenTitleMissing)
			{
				title = definition.Id;
			}

			SetTitle(title);
			SetId(definition.Id);
			SetInteractable(true);

			if (button != null)
			{
				button.onClick.RemoveListener(HandleClick);
				button.onClick.AddListener(HandleClick);
			}
		}

		public void SetInteractable(bool value)
		{
			if (button != null)
			{
				button.interactable = value;
			}
		}

		private void SetTitle(string value)
		{
			if (titleText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(titleText, value);
			}
		}

		private void SetId(string value)
		{
			if (idText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(idText, value);
			}
		}

		private void HandleClick()
		{
			if (definition == null)
			{
				return;
			}

			clicked?.Invoke(definition);
		}
	}
}
