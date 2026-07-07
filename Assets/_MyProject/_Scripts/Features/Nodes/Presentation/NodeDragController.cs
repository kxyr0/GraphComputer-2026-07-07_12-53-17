using Game.Grid.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Nodes.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public sealed class NodeDragController : MonoBehaviour,
		IPointerDownHandler,
		IBeginDragHandler,
		IDragHandler,
		IEndDragHandler
	{
		[Header("References")]
		[SerializeField] private RectTransform nodeRectTransform;
		[SerializeField] private Canvas canvas;

		[Tooltip("Твоя CanvasGridView-сетка. Нужна, чтобы снапиться именно к видимому гриду.")]
		[SerializeField] private CanvasGridView gridView;

		[Header("Drag")]
		[SerializeField] private bool bringToFrontOnPointerDown = true;
		[SerializeField] private bool onlyLeftMouseButton = true;

		[Header("Grid Snap")]
		[SerializeField] private bool snapToGrid = true;
		[SerializeField] private bool snapWhileDragging = false;
		[SerializeField] private bool useVisualGrid = true;
		[SerializeField, Min(1f)] private float fallbackGridCellSize = 80f;
		[SerializeField] private Vector2 fallbackGridOrigin = Vector2.zero;

		[Header("Bounds")]
		[SerializeField] private bool clampInsideParent;
		[SerializeField] private bool canDrag = true;
		private Vector2 pointerOffset;
		private bool isDragging;

		private RectTransform NodeParent => nodeRectTransform.parent as RectTransform;

		private void Reset()
		{
			nodeRectTransform = transform as RectTransform;
			canvas = GetComponentInParent<Canvas>();
			gridView = GetComponentInParent<CanvasGridView>();
		}
		public void SetCanDrag(bool value)
		{
			canDrag = value;
		}
		private void Awake()
		{
			ResolveReferences();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!IsAllowedButton(eventData))
			{
				return;
			}

			if (bringToFrontOnPointerDown)
			{
				transform.SetAsLastSibling();
			}
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!canDrag)
			{
				return;
			}
			if (!IsAllowedButton(eventData))
			{
				return;
			}

			ResolveReferences();

			if (!TryGetPointerLocalPosition(eventData, out Vector2 pointerLocalPosition))
			{
				return;
			}

			pointerOffset = pointerLocalPosition - nodeRectTransform.anchoredPosition;
			isDragging = true;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!canDrag)
			{
				return;
			}
			if (!isDragging)
			{
				return;
			}

			if (!TryGetPointerLocalPosition(eventData, out Vector2 pointerLocalPosition))
			{
				return;
			}

			Vector2 targetPosition = pointerLocalPosition - pointerOffset;

			if (snapToGrid && snapWhileDragging)
			{
				targetPosition = SnapPosition(targetPosition);
			}

			if (clampInsideParent)
			{
				targetPosition = ClampInsideParent(targetPosition);
			}

			// Важно: без SmoothDamp. Нода должна быть прямо под курсором.
			nodeRectTransform.anchoredPosition = targetPosition;
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (!isDragging)
			{
				return;
			}

			isDragging = false;

			if (!snapToGrid)
			{
				return;
			}

			Vector2 finalPosition = SnapPosition(nodeRectTransform.anchoredPosition);

			if (clampInsideParent)
			{
				finalPosition = ClampInsideParent(finalPosition);
			}

			// Важно: мгновенно. Без плавного отъезда назад.
			nodeRectTransform.anchoredPosition = finalPosition;
		}

		private void ResolveReferences()
		{
			if (nodeRectTransform == null)
			{
				nodeRectTransform = transform as RectTransform;
			}

			if (canvas == null)
			{
				canvas = GetComponentInParent<Canvas>();
			}

			if (gridView == null)
			{
				gridView = GetComponentInParent<CanvasGridView>();
			}
		}

		private bool IsAllowedButton(PointerEventData eventData)
		{
			return !onlyLeftMouseButton || eventData.button == PointerEventData.InputButton.Left;
		}

		private bool TryGetPointerLocalPosition(PointerEventData eventData, out Vector2 localPosition)
		{
			localPosition = Vector2.zero;

			RectTransform nodeParent = NodeParent;

			if (nodeParent == null)
			{
				return false;
			}

			Camera eventCamera = eventData.pressEventCamera != null
				? eventData.pressEventCamera
				: GetEventCamera();

			return RectTransformUtility.ScreenPointToLocalPointInRectangle(
				nodeParent,
				eventData.position,
				eventCamera,
				out localPosition
			);
		}

		private Camera GetEventCamera()
		{
			if (canvas == null)
			{
				return Camera.main;
			}

			if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
			{
				return null;
			}

			if (canvas.worldCamera != null)
			{
				return canvas.worldCamera;
			}

			return Camera.main;
		}

		private Vector2 SnapPosition(Vector2 position)
		{
			if (useVisualGrid && gridView != null)
			{
				return SnapToVisualGrid(position);
			}

			return SnapToFallbackGrid(position);
		}

		private Vector2 SnapToFallbackGrid(Vector2 position)
		{
			float step = Mathf.Max(1f, fallbackGridCellSize);

			return new Vector2(
				fallbackGridOrigin.x + Mathf.Round((position.x - fallbackGridOrigin.x) / step) * step,
				fallbackGridOrigin.y + Mathf.Round((position.y - fallbackGridOrigin.y) / step) * step
			);
		}

		private Vector2 SnapToVisualGrid(Vector2 positionInNodeParent)
		{
			RectTransform nodeParent = NodeParent;
			RectTransform gridRectTransform = gridView.transform as RectTransform;

			if (nodeParent == null || gridRectTransform == null)
			{
				return SnapToFallbackGrid(positionInNodeParent);
			}

			float step = Mathf.Max(1f, gridView.CellSize * gridView.Zoom);
			Vector2 gridOrigin = gridView.PanOffset;

			Vector3 worldPosition = nodeParent.TransformPoint(
				new Vector3(positionInNodeParent.x, positionInNodeParent.y, 0f)
			);

			Vector3 gridLocalPosition3 = gridRectTransform.InverseTransformPoint(worldPosition);
			Vector2 gridLocalPosition = new Vector2(gridLocalPosition3.x, gridLocalPosition3.y);

			Vector2 snappedGridLocalPosition = new Vector2(
				gridOrigin.x + Mathf.Round((gridLocalPosition.x - gridOrigin.x) / step) * step,
				gridOrigin.y + Mathf.Round((gridLocalPosition.y - gridOrigin.y) / step) * step
			);

			Vector3 snappedWorldPosition = gridRectTransform.TransformPoint(
				new Vector3(snappedGridLocalPosition.x, snappedGridLocalPosition.y, 0f)
			);

			Vector3 snappedNodeParentLocal3 = nodeParent.InverseTransformPoint(snappedWorldPosition);

			return new Vector2(snappedNodeParentLocal3.x, snappedNodeParentLocal3.y);
		}

		private Vector2 ClampInsideParent(Vector2 position)
		{
			RectTransform nodeParent = NodeParent;

			if (nodeParent == null || nodeRectTransform == null)
			{
				return position;
			}

			Rect parentRect = nodeParent.rect;
			Rect nodeRect = nodeRectTransform.rect;

			float halfWidth = nodeRect.width * 0.5f;
			float halfHeight = nodeRect.height * 0.5f;

			position.x = Mathf.Clamp(
				position.x,
				parentRect.xMin + halfWidth,
				parentRect.xMax - halfWidth
			);

			position.y = Mathf.Clamp(
				position.y,
				parentRect.yMin + halfHeight,
				parentRect.yMax - halfHeight
			);

			return position;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (nodeRectTransform == null)
			{
				nodeRectTransform = transform as RectTransform;
			}

			fallbackGridCellSize = Mathf.Max(1f, fallbackGridCellSize);
		}
#endif
	}
}