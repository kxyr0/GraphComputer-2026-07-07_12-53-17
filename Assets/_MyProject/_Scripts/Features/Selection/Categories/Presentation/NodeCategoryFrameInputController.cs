using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Selection.Categories.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NodeCategoryFrameView))]
	public sealed class NodeCategoryFrameInputController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		[SerializeField] private NodeCategoryFrameView frameView;
		[SerializeField] private bool restoreCategoryHitOnPointerExit = true;
		[SerializeField] private bool categoryUsesRightClick = true;
		[SerializeField] private bool passLeftClickToNodes = true;
		[SerializeField] private bool disableRaycastsAfterCategoryClick = true;
		[SerializeField] private bool forwardPassThroughClickToUnderlyingObject = true;

		private readonly List<RaycastResult> raycastResults = new();
		private Camera lastEventCamera;
		private bool isPassingThroughToNodes;

		private void Reset()
		{
			frameView = GetComponent<NodeCategoryFrameView>();
		}

		private void Awake()
		{
			ResolveView();
		}

		private void Update()
		{
			if (!isPassingThroughToNodes || !restoreCategoryHitOnPointerExit)
			{
				return;
			}

			Mouse mouse = Mouse.current;

			if (mouse == null || frameView == null)
			{
				return;
			}

			Vector2 pointerPosition = mouse.position.ReadValue();

			if (!RectTransformUtility.RectangleContainsScreenPoint(frameView.RectTransform, pointerPosition, lastEventCamera))
			{
				RestoreCategoryHit();
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			lastEventCamera = eventData.enterEventCamera;
			ResolveView();
			frameView.SetHovered(true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			ResolveView();
			frameView.SetHovered(false);

			if (restoreCategoryHitOnPointerExit)
			{
				RestoreCategoryHit();
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			lastEventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
			ResolveView();

			if (IsCategoryClick(eventData))
			{
				frameView.SetSelected(true);

				if (disableRaycastsAfterCategoryClick)
				{
					BeginPassThroughToNodes();
				}

				return;
			}

			if (eventData.button != PointerEventData.InputButton.Left || !passLeftClickToNodes)
			{
				return;
			}

			BeginPassThroughToNodes();

			if (forwardPassThroughClickToUnderlyingObject)
			{
				ForwardClickToUnderlyingObject(eventData);
			}
		}

		private void RestoreCategoryHit()
		{
			isPassingThroughToNodes = false;
			frameView.SetRaycastBlocking(true);
		}

		private void BeginPassThroughToNodes()
		{
			isPassingThroughToNodes = true;
			frameView.SetRaycastBlocking(false);
		}

		private bool IsCategoryClick(PointerEventData eventData)
		{
			return categoryUsesRightClick && eventData.button == PointerEventData.InputButton.Right;
		}

		private void ResolveView()
		{
			if (frameView == null)
			{
				frameView = GetComponent<NodeCategoryFrameView>();
			}
		}

		private void ForwardClickToUnderlyingObject(PointerEventData sourceEventData)
		{
			EventSystem eventSystem = EventSystem.current;

			if (eventSystem == null)
			{
				return;
			}

			PointerEventData forwardedEventData = new(eventSystem)
			{
				button = sourceEventData.button,
				clickCount = 1,
				clickTime = sourceEventData.clickTime,
				delta = sourceEventData.delta,
				position = sourceEventData.position,
				pressPosition = sourceEventData.pressPosition,
				scrollDelta = sourceEventData.scrollDelta
			};

			raycastResults.Clear();
			eventSystem.RaycastAll(forwardedEventData, raycastResults);

			for (int i = 0; i < raycastResults.Count; i++)
			{
				GameObject target = raycastResults[i].gameObject;

				if (target == null || target.transform.IsChildOf(transform))
				{
					continue;
				}

				ExecuteEvents.ExecuteHierarchy(target, forwardedEventData, ExecuteEvents.pointerClickHandler);
				break;
			}
		}
	}
}
