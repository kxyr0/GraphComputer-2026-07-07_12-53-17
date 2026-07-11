using Game.Nodes.Definitions;
using Game.Nodes.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	[DisallowMultipleComponent]
	public sealed class NodePortView : MonoBehaviour
	{
		[Header("Обязательные элементы")]
		[SerializeField] private Image pointImage;

		[Header("Необязательные элементы")]
		[SerializeField] private TMP_Text labelText;

		public string PortId { get; private set; }
		public NodePortDirection Direction { get; private set; }
		public NodeSignalType SignalType { get; private set; }

		private void Reset()
		{
			pointImage = GetComponent<Image>();
		}

		public void Bind(NodePortDefinition definition)
		{
			if (definition == null)
			{
				Debug.LogError("NodePortView.Bind failed: definition is null.", this);
				return;
			}

			PortId = definition.Id;
			Direction = definition.Direction;
			SignalType = definition.SignalType;

			if (pointImage != null)
			{
				pointImage.color = definition.Color;
			}

			if (labelText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(labelText, definition.DisplayName);
			}

			gameObject.SetActive(true);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}
	}
}
