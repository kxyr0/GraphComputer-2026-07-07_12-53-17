using System;
using Game.Nodes.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	[RequireComponent(typeof(CanvasRenderer))]
	public sealed class NodeConnectionView : Graphic, IPointerClickHandler
	{
		[SerializeField, Min(1f)] private float thickness = 4f;
		[SerializeField, Range(4, 64)] private int segments = 24;

		[SerializeField, Min(0f)] private float minTangent = 80f;
		[SerializeField, Min(0f)] private float maxTangent = 260f;

		[Header("Input")]
		[SerializeField, Min(1f)] private float clickHitPadding = 12f;

		private RectTransform from;
		private RectTransform to;

		private bool useManualEnd;
		private Vector2 manualEndLocal;

		private RectTransform selfRect;

		public NodeConnectionData Data { get; private set; }

		public event Action<NodeConnectionView, PointerEventData> Clicked;

		private RectTransform SelfRect
		{
			get
			{
				if (selfRect == null)
				{
					selfRect = transform as RectTransform;
				}

				return selfRect;
			}
		}

		protected override void Awake()
		{
			base.Awake();

			// Важно: включаем raycast, но ниже через Raycast()
			// пропускаем клики, если мышь не рядом с линией.
			raycastTarget = true;
		}

		private void LateUpdate()
		{
			if (from != null && (to != null || useManualEnd))
			{
				SetVerticesDirty();
			}
		}

		public void Bind(RectTransform fromConnector, RectTransform toConnector, Color lineColor)
		{
			Data = null;
			from = fromConnector;
			to = toConnector;
			useManualEnd = false;
			color = lineColor;

			SetVerticesDirty();
		}

		public void Bind(
			NodeConnectionData connectionData,
			RectTransform fromConnector,
			RectTransform toConnector,
			Color lineColor)
		{
			Data = connectionData;
			from = fromConnector;
			to = toConnector;
			useManualEnd = false;
			color = lineColor;

			SetVerticesDirty();
		}

		public void BindPreview(RectTransform fromConnector, Color lineColor)
		{
			Data = null;
			from = fromConnector;
			to = null;
			useManualEnd = true;
			color = lineColor;

			if (fromConnector != null)
			{
				manualEndLocal = GetLocalPoint(fromConnector);
			}

			SetVerticesDirty();
		}

		public void SetPreviewEndScreen(Vector2 screenPosition, Camera eventCamera)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				SelfRect,
				screenPosition,
				eventCamera,
				out manualEndLocal
			);

			SetVerticesDirty();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			Clicked?.Invoke(this, eventData);
		}

		public override bool Raycast(Vector2 screenPosition, Camera eventCamera)
		{
			if (!base.Raycast(screenPosition, eventCamera))
			{
				return false;
			}

			if (from == null || (to == null && !useManualEnd))
			{
				return false;
			}

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				SelfRect,
				screenPosition,
				eventCamera,
				out Vector2 localMousePosition
			);

			Vector2 start = GetLocalPoint(from);
			Vector2 end = to != null ? GetLocalPoint(to) : manualEndLocal;

			float distance = Mathf.Abs(end.x - start.x);
			float tangent = Mathf.Clamp(distance * 0.5f, minTangent, maxTangent);

			Vector2 controlA = start + Vector2.right * tangent;
			Vector2 controlB = end + Vector2.left * tangent;

			int safeSegments = Mathf.Max(4, segments);
			Vector2 previousPoint = start;

			float allowedDistance = thickness * 0.5f + clickHitPadding;

			for (int i = 1; i <= safeSegments; i++)
			{
				float t = i / (float)safeSegments;
				Vector2 currentPoint = EvaluateBezier(start, controlA, controlB, end, t);

				float pointDistance = DistancePointToSegment(
					localMousePosition,
					previousPoint,
					currentPoint
				);

				if (pointDistance <= allowedDistance)
				{
					return true;
				}

				previousPoint = currentPoint;
			}

			return false;
		}

		protected override void OnPopulateMesh(VertexHelper vertexHelper)
		{
			vertexHelper.Clear();

			if (from == null)
			{
				return;
			}

			Vector2 start = GetLocalPoint(from);
			Vector2 end = to != null ? GetLocalPoint(to) : manualEndLocal;

			float distance = Mathf.Abs(end.x - start.x);
			float tangent = Mathf.Clamp(distance * 0.5f, minTangent, maxTangent);

			Vector2 controlA = start + Vector2.right * tangent;
			Vector2 controlB = end + Vector2.left * tangent;

			int safeSegments = Mathf.Max(4, segments);
			Vector2 previousPoint = start;

			for (int i = 1; i <= safeSegments; i++)
			{
				float t = i / (float)safeSegments;
				Vector2 currentPoint = EvaluateBezier(start, controlA, controlB, end, t);

				AddLineSegment(vertexHelper, previousPoint, currentPoint);
				previousPoint = currentPoint;
			}
		}

		private Vector2 GetLocalPoint(RectTransform target)
		{
			Vector3 worldCenter = target.TransformPoint(target.rect.center);
			return SelfRect.InverseTransformPoint(worldCenter);
		}

		private void AddLineSegment(VertexHelper vertexHelper, Vector2 start, Vector2 end)
		{
			Vector2 direction = end - start;

			if (direction.sqrMagnitude <= 0.001f)
			{
				return;
			}

			Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (thickness * 0.5f);

			int index = vertexHelper.currentVertCount;

			UIVertex vertex = UIVertex.simpleVert;
			vertex.color = color;

			vertex.position = start - normal;
			vertexHelper.AddVert(vertex);

			vertex.position = start + normal;
			vertexHelper.AddVert(vertex);

			vertex.position = end + normal;
			vertexHelper.AddVert(vertex);

			vertex.position = end - normal;
			vertexHelper.AddVert(vertex);

			vertexHelper.AddTriangle(index, index + 1, index + 2);
			vertexHelper.AddTriangle(index, index + 2, index + 3);
		}

		private static Vector2 EvaluateBezier(
			Vector2 start,
			Vector2 controlA,
			Vector2 controlB,
			Vector2 end,
			float t)
		{
			float oneMinusT = 1f - t;

			return
				oneMinusT * oneMinusT * oneMinusT * start +
				3f * oneMinusT * oneMinusT * t * controlA +
				3f * oneMinusT * t * t * controlB +
				t * t * t * end;
		}

		private static float DistancePointToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
		{
			Vector2 segment = segmentEnd - segmentStart;
			float segmentLengthSqr = segment.sqrMagnitude;

			if (segmentLengthSqr <= 0.0001f)
			{
				return Vector2.Distance(point, segmentStart);
			}

			float t = Vector2.Dot(point - segmentStart, segment) / segmentLengthSqr;
			t = Mathf.Clamp01(t);

			Vector2 projection = segmentStart + segment * t;
			return Vector2.Distance(point, projection);
		}
	}
}