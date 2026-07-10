using Game.Nodes.Presentation;
using Game.Selection.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Selection.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(SelectableNode))]
	public sealed class NodeSelectionClickController : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField] private SelectableNode selectableNode;
		[SerializeField] private NodeSelectionService selectionService;
		[SerializeField] private bool ignoreClicksOnPorts = true;
		[SerializeField] private bool useShiftForAdditiveSelection = true;
		[SerializeField] private bool useControlForToggleSelection = true;

		private void Reset()
		{
			selectableNode = GetComponent<SelectableNode>();
		}

		private void Awake()
		{
			if (selectableNode == null)
			{
				selectableNode = GetComponent<SelectableNode>();
			}

			if (selectionService == null)
			{
				selectionService = FindFirstObjectByType<NodeSelectionService>();
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}

			if (ignoreClicksOnPorts && IsClickOnPort(eventData))
			{
				return;
			}

			if (selectableNode == null || !selectableNode.CanSelect)
			{
				return;
			}

			if (selectionService == null)
			{
				Debug.LogWarning(
					$"[{nameof(NodeSelectionClickController)}] Selection service is missing.",
					this);

				return;
			}

			if (IsToggleModifierPressed())
			{
				selectionService.ToggleSelection(selectableNode);
				return;
			}

			if (IsAdditiveModifierPressed())
			{
				selectionService.AddToSelection(selectableNode);
				return;
			}

			selectionService.SelectSingle(selectableNode);
		}

		private static bool IsClickOnPort(PointerEventData eventData)
		{
			GameObject clickedObject = eventData.pointerPressRaycast.gameObject;

			if (clickedObject == null)
			{
				clickedObject = eventData.pointerCurrentRaycast.gameObject;
			}

			return clickedObject != null &&
				   clickedObject.GetComponentInParent<GeneratedNodePortView>() != null;
		}

		private bool IsAdditiveModifierPressed()
		{
			if (!useShiftForAdditiveSelection || Keyboard.current == null)
			{
				return false;
			}

			return Keyboard.current.leftShiftKey.isPressed ||
				   Keyboard.current.rightShiftKey.isPressed;
		}

		private bool IsToggleModifierPressed()
		{
			if (!useControlForToggleSelection || Keyboard.current == null)
			{
				return false;
			}

			return Keyboard.current.leftCtrlKey.isPressed ||
				   Keyboard.current.rightCtrlKey.isPressed;
		}
	}
}
