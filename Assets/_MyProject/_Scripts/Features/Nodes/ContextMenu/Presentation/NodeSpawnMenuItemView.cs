using System;
using Game.Nodes.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	public sealed class NodeSpawnMenuItemView : MonoBehaviour
	{
		[SerializeField] private Button button;
		[SerializeField] private TMP_Text titleText;

		private NodeDefinition definition;
		private Action<NodeDefinition> clicked;

		private void Reset()
		{
			button = GetComponent<Button>();
			titleText = GetComponentInChildren<TMP_Text>(true);
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

			name = $"SpawnItem_{definition.Id}";

			if (titleText != null)
			{
				titleText.text = string.IsNullOrWhiteSpace(definition.Title)
					? definition.Id
					: definition.Title;
			}

			if (button != null)
			{
				button.onClick.RemoveListener(HandleClick);
				button.onClick.AddListener(HandleClick);
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