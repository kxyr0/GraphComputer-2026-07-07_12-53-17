using System.Collections.Generic;
using Game.Nodes.Presentation;
using Game.Runtime.Input;
using Game.Selection.Runtime;
using Game.UI.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Selection.Presentation
{
	[DisallowMultipleComponent]
	public sealed class NodeSelectBoxController : MonoBehaviour
	{
		private enum SelectionHitMode
		{
			Intersects,
			ContainsCenter,
			ContainsEntireRect
		}

		[Header("References")]
		[SerializeField] private NodeSelectionService selectionService;
		[SerializeField] private RectTransform selectionArea;
		[SerializeField] private SelectBoxView selectBoxView;
		[SerializeField] private Canvas canvas;

		[Header("Input")]
		[SerializeField, Min(0f)] private float minDragDistance = 6f;
		[SerializeField] private bool requirePointerInsideScreen = true;
		[SerializeField] private bool blockWhenGameplayInputBlocked = true;
		[SerializeField] private bool blockStartOverBlockingUi = true;
		[SerializeField] private bool blockStartOverNode = true;
		[SerializeField] private bool blockGameplayInputWhileSelecting = true;

		[Header("Selection")]
		[SerializeField] private SelectionHitMode selectionHitMode = SelectionHitMode.Intersects;
		[SerializeField] private bool previewSelectionWhileDragging = true;
		[SerializeField] private bool clearSelectionOnEmptyClick = true;
		[SerializeField] private bool useShiftForAdditiveSelection = true;
		[SerializeField] private bool useControlForToggleSelection = true;

		private static readonly List<RaycastResult> RaycastResults = new();

		private readonly List<SelectableNode> selectableNodes = new();
		private readonly HashSet<SelectableNode> initialSelection = new();
		private readonly HashSet<SelectableNode> hitSelection = new();
		private readonly HashSet<SelectableNode> targetSelection = new();

		private Camera selectionAreaCamera;
		private Vector2 dragStartScreenPosition;
		private bool isPointerTracking;
		private bool isSelecting;
		private bool isAdditiveSelection;
		private bool isToggleSelection;
		private bool isBlockingGameplayInput;
		private bool loggedMissingSelectionService;

		private void Reset()
		{
			canvas = GetComponentInParent<Canvas>();
			selectionService = FindFirstObjectByType<NodeSelectionService>();
			selectBoxView = GetComponentInChildren<SelectBoxView>(true);
			ResolveSelectionArea();
		}

		private void Awake()
		{
			if (canvas == null)
			{
				canvas = GetComponentInParent<Canvas>();
			}

			if (selectBoxView == null)
			{
				selectBoxView = GetComponentInChildren<SelectBoxView>(true);
			}

			if (selectionService == null)
			{
				selectionService = FindFirstObjectByType<NodeSelectionService>();
			}

			ResolveSelectionArea();
			selectBoxView?.Hide();
		}

		private void Update()
		{
			Mouse mouse = Mouse.current;

			if (mouse == null)
			{
				CancelSelection();
				return;
			}

			Vector2 pointerPosition = mouse.position.ReadValue();

			if (!isPointerTracking)
			{
				if (mouse.leftButton.wasPressedThisFrame)
				{
					TryStartPointerTracking(pointerPosition);
				}

				return;
			}

			if (mouse.leftButton.wasReleasedThisFrame)
			{
				CompletePointerTracking(pointerPosition);
				return;
			}

			if (mouse.leftButton.isPressed)
			{
				UpdatePointerTracking(pointerPosition);
				return;
			}

			CancelSelection();
		}

		private void OnDisable()
		{
			CancelSelection();
		}

		private void OnDestroy()
		{
			CancelSelection();
		}

		public void ClearSelection()
		{
			if (HasRequiredReferences())
			{
				selectionService.ClearSelection();
			}
		}

		private void TryStartPointerTracking(Vector2 screenPosition)
		{
			if (!HasRequiredReferences())
			{
				return;
			}

			if (blockWhenGameplayInputBlocked && GameInputBlocker.IsBlocked)
			{
				return;
			}

			if (requirePointerInsideScreen && !IsPointerInsideScreen(screenPosition))
			{
				return;
			}

			if (blockStartOverBlockingUi && UiPointerBlockUtility.IsPointerOverBlockingUi())
			{
				return;
			}

			if (blockStartOverNode && IsPointerOverNode(screenPosition))
			{
				return;
			}

			selectionAreaCamera = GetSelectionAreaCamera();
			dragStartScreenPosition = screenPosition;
			isPointerTracking = true;
			isSelecting = false;
			isAdditiveSelection = false;
			isToggleSelection = false;

			selectBoxView?.Hide();
		}

		private void UpdatePointerTracking(Vector2 screenPosition)
		{
			float dragDistance = Vector2.Distance(dragStartScreenPosition, screenPosition);

			if (!isSelecting && dragDistance < minDragDistance)
			{
				return;
			}

			if (!isSelecting)
			{
				BeginSelection();
			}

			UpdateSelectBoxView(screenPosition);

			if (previewSelectionWhileDragging)
			{
				ApplySelection(screenPosition);
			}
		}

		private void CompletePointerTracking(Vector2 screenPosition)
		{
			if (isSelecting)
			{
				ApplySelection(screenPosition);
				EndSelection();
				return;
			}

			if (clearSelectionOnEmptyClick && !IsAdditiveModifierPressed() && !IsToggleModifierPressed())
			{
				ClearSelection();
			}

			ResetPointerTracking();
		}

		private void BeginSelection()
		{
			isSelecting = true;
			isToggleSelection = IsToggleModifierPressed();
			isAdditiveSelection = !isToggleSelection && IsAdditiveModifierPressed();

			CaptureInitialSelection();

			if (blockGameplayInputWhileSelecting)
			{
				GameInputBlocker.Block(this);
				isBlockingGameplayInput = true;
			}
		}

		private void EndSelection()
		{
			selectBoxView?.Hide();
			ResetPointerTracking();
			ReleaseGameplayInputBlock();
		}

		private void CancelSelection()
		{
			if (isSelecting)
			{
				selectBoxView?.Hide();
			}

			ResetPointerTracking();
			ReleaseGameplayInputBlock();
		}

		private void ResetPointerTracking()
		{
			isPointerTracking = false;
			isSelecting = false;
			isAdditiveSelection = false;
			isToggleSelection = false;
			initialSelection.Clear();
			hitSelection.Clear();
			targetSelection.Clear();
		}

		private void ReleaseGameplayInputBlock()
		{
			if (!isBlockingGameplayInput)
			{
				return;
			}

			GameInputBlocker.Unblock(this);
			isBlockingGameplayInput = false;
		}

		private void CaptureInitialSelection()
		{
			initialSelection.Clear();
			selectionService.CaptureSelected(initialSelection);
		}

		private void ApplySelection(Vector2 screenPosition)
		{
			CollectHitSelection(screenPosition);
			BuildTargetSelection();
			selectionService.ApplySelection(targetSelection);
		}

		private void CollectHitSelection(Vector2 screenPosition)
		{
			hitSelection.Clear();
			selectionService.CollectSelectableNodes(selectableNodes);

			Rect selectionRect = RectTransformScreenUtility.CreateRect(
				dragStartScreenPosition,
				screenPosition);

			foreach (SelectableNode node in selectableNodes)
			{
				if (node == null || !node.CanSelect)
				{
					continue;
				}

				if (IsNodeInsideSelection(node, selectionRect))
				{
					hitSelection.Add(node);
				}
			}
		}

		private void BuildTargetSelection()
		{
			targetSelection.Clear();

			if (isToggleSelection)
			{
				foreach (SelectableNode node in initialSelection)
				{
					if (!hitSelection.Contains(node))
					{
						targetSelection.Add(node);
					}
				}

				foreach (SelectableNode node in hitSelection)
				{
					if (!initialSelection.Contains(node))
					{
						targetSelection.Add(node);
					}
				}

				return;
			}

			if (isAdditiveSelection)
			{
				foreach (SelectableNode node in initialSelection)
				{
					targetSelection.Add(node);
				}
			}

			foreach (SelectableNode node in hitSelection)
			{
				targetSelection.Add(node);
			}
		}

		private bool IsNodeInsideSelection(SelectableNode node, Rect selectionRect)
		{
			if (!RectTransformScreenUtility.TryGetScreenRect(
					node.RectTransform,
					selectionAreaCamera,
					out Rect nodeRect))
			{
				return false;
			}

			return selectionHitMode switch
			{
				SelectionHitMode.ContainsCenter =>
					RectTransformScreenUtility.ContainsCenter(selectionRect, nodeRect),
				SelectionHitMode.ContainsEntireRect =>
					RectTransformScreenUtility.ContainsRect(selectionRect, nodeRect),
				_ => selectionRect.Overlaps(nodeRect, true)
			};
		}

		private void UpdateSelectBoxView(Vector2 screenPosition)
		{
			if (selectBoxView == null)
			{
				return;
			}

			ResolveSelectionArea();

			if (selectionArea == null)
			{
				return;
			}

			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
					selectionArea,
					dragStartScreenPosition,
					selectionAreaCamera,
					out Vector2 localStart))
			{
				return;
			}

			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
					selectionArea,
					screenPosition,
					selectionAreaCamera,
					out Vector2 localEnd))
			{
				return;
			}

			selectBoxView.Show(RectTransformScreenUtility.CreateRect(localStart, localEnd));
		}

		private bool HasRequiredReferences()
		{
			if (selectionService != null)
			{
				return true;
			}

			selectionService = FindFirstObjectByType<NodeSelectionService>();

			if (selectionService != null)
			{
				return true;
			}

			if (!loggedMissingSelectionService)
			{
				Debug.LogWarning(
					$"[{nameof(NodeSelectBoxController)}] Selection service is missing.",
					this);

				loggedMissingSelectionService = true;
			}

			return false;
		}

		private void ResolveSelectionArea()
		{
			if (selectionArea != null)
			{
				return;
			}

			RectTransform boxRect = selectBoxView != null
				? selectBoxView.BoxRect
				: null;

			if (boxRect != null)
			{
				selectionArea = boxRect.parent as RectTransform;
			}
		}

		private bool IsPointerOverNode(Vector2 screenPosition)
		{
			if (EventSystem.current == null)
			{
				return false;
			}

			PointerEventData eventData = new PointerEventData(EventSystem.current)
			{
				position = screenPosition
			};

			RaycastResults.Clear();
			EventSystem.current.RaycastAll(eventData, RaycastResults);

			foreach (RaycastResult result in RaycastResults)
			{
				if (result.gameObject == null)
				{
					continue;
				}

				if (result.gameObject.GetComponentInParent<NodeView>() != null)
				{
					return true;
				}
			}

			return false;
		}

		private Camera GetSelectionAreaCamera()
		{
			ResolveSelectionArea();

			Canvas targetCanvas = selectionArea != null
				? selectionArea.GetComponentInParent<Canvas>()
				: null;

			if (targetCanvas == null && selectBoxView != null && selectBoxView.BoxRect != null)
			{
				targetCanvas = selectBoxView.BoxRect.GetComponentInParent<Canvas>();
			}

			if (targetCanvas == null)
			{
				if (canvas == null)
				{
					canvas = GetComponentInParent<Canvas>();
				}

				targetCanvas = canvas;
			}

			if (targetCanvas == null)
			{
				return Camera.main;
			}

			if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
			{
				return null;
			}

			if (targetCanvas.worldCamera != null)
			{
				return targetCanvas.worldCamera;
			}

			return Camera.main;
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

		private static bool IsPointerInsideScreen(Vector2 screenPosition)
		{
			return screenPosition.x >= 0f &&
				   screenPosition.y >= 0f &&
				   screenPosition.x <= Screen.width &&
				   screenPosition.y <= Screen.height;
		}
	}
}
