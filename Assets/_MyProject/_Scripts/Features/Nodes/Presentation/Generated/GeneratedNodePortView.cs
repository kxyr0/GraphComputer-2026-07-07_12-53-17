using Game.Nodes.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	public enum GeneratedNodePortDirection
	{
		Input,
		Output
	}

	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public sealed class GeneratedNodePortView : MonoBehaviour,
		IPointerEnterHandler,
		IPointerExitHandler,
		IPointerDownHandler,
		IPointerUpHandler,
		IDragHandler,
		IEndDragHandler
	{

		[SerializeField] private TMP_Text labelText;
		[SerializeField] private Image connectorImage;
		[SerializeField] private HorizontalLayoutGroup layoutGroup;

		[Header("Layout")]
		[SerializeField] private bool connectorFirst;
		[SerializeField] private TextAnchor childAlignment = TextAnchor.MiddleRight;
		[SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Right;
		[SerializeField] private float spacing = 8f;
		public Color PortColor { get; private set; } = Color.cyan;

		private Color defaultConnectorColor = Color.cyan;
		private NodeConnectionDragController dragController;

		public NodeView Owner { get; private set; }
		public string PortId { get; private set; }
		public string Label { get; private set; }
		public GeneratedNodePortDirection Direction { get; private set; }

		public bool IsInput => Direction == GeneratedNodePortDirection.Input;
		public bool IsOutput => Direction == GeneratedNodePortDirection.Output;
		public NodeJsonPort PortDefinition { get; private set; }
		public RectTransform ConnectorRect
		{
			get
			{
				if (connectorImage != null && connectorImage.transform is RectTransform connectorRect)
				{
					return connectorRect;
				}

				return transform as RectTransform;
			}
		}

		private void Reset()
		{
			layoutGroup = GetComponent<HorizontalLayoutGroup>();
			labelText = GetComponentInChildren<TMP_Text>(true);

			Image[] images = GetComponentsInChildren<Image>(true);

			foreach (Image image in images)
			{
				if (image.transform != transform)
				{
					connectorImage = image;
					break;
				}
			}
		}

		public void Initialize(NodeView owner)
		{
			Owner = owner;

			if (dragController == null)
			{
				dragController = FindFirstObjectByType<NodeConnectionDragController>();
			}
		}

		public void Bind(NodeJsonPort port)
		{
			PortDefinition = port;
			PortId = string.IsNullOrWhiteSpace(port.id) ? port.label : port.id;
			Label = port.label;
			Direction = ParseDirection(port.direction);

			name = $"Port_{Direction}_{PortId}";

			if (labelText != null)
			{
				labelText.text = port.label;
				labelText.alignment = textAlignment;
				labelText.raycastTarget = true;
			}

			PortColor = NodeColorParser.ParseColor(port.color, Color.cyan);
			defaultConnectorColor = PortColor;

			if (connectorImage != null)
			{
				connectorImage.color = PortColor;
			}

			ApplyLayout();
		}
		public void SetConnectionColor(Color color)
		{
			if (connectorImage != null)
			{
				connectorImage.color = color;
			}
		}

		public void ResetConnectionColor()
		{
			if (connectorImage != null)
			{
				connectorImage.color = defaultConnectorColor;
			}
		}
		public void OnPointerEnter(PointerEventData eventData)
		{
			dragController?.SetHoveredPort(this);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			dragController?.ClearHoveredPort(this);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			dragController?.BeginDrag(this, eventData);
		}

		public void OnDrag(PointerEventData eventData)
		{
			dragController?.UpdateDrag(eventData);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			dragController?.EndDrag(eventData);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			dragController?.EndDrag(eventData);
		}

		private void ApplyLayout()
		{
			if (layoutGroup != null)
			{
				layoutGroup.childAlignment = childAlignment;
				layoutGroup.spacing = spacing;
			}

			if (labelText == null || connectorImage == null)
			{
				return;
			}

			if (connectorFirst)
			{
				connectorImage.transform.SetSiblingIndex(0);
				labelText.transform.SetSiblingIndex(1);
			}
			else
			{
				labelText.transform.SetSiblingIndex(0);
				connectorImage.transform.SetSiblingIndex(1);
			}
		}

		private static GeneratedNodePortDirection ParseDirection(string value)
		{
			return string.Equals(value, "Input", System.StringComparison.OrdinalIgnoreCase)
				? GeneratedNodePortDirection.Input
				: GeneratedNodePortDirection.Output;
		}

	}
}
