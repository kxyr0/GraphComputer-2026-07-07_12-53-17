using UnityEngine;
using UnityEngine.UI;

namespace Game.Grid.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(CanvasRenderer))]
	public sealed class CanvasGridView : MaskableGraphic
	{
		[Header("Grid")]
		[SerializeField, Min(4f)] private float cellSize = 80f;
		[SerializeField, Min(1)] private int majorLineEvery = 5;
		[SerializeField, Min(0.1f)] private float lineThickness = 1f;
		[SerializeField, Min(0.1f)] private float majorLineThickness = 1.5f;

		[Header("Zoom")]
		[SerializeField, Min(0.05f)] private float minZoom = 0.15f;
		[SerializeField, Min(0.05f)] private float maxZoom = 5f;

		[Header("Safety")]
		[SerializeField, Min(4f)] private float minVisibleCellSize = 12f;
		[SerializeField, Min(32)] private int maxLinesPerAxis = 700;

		[Header("Colors")]
		[SerializeField] private bool drawBackground = true;
		[SerializeField] private Color backgroundColor = new(0.035f, 0.035f, 0.07f, 1f);
		[SerializeField] private Color minorLineColor = new(1f, 1f, 1f, 0.08f);
		[SerializeField] private Color majorLineColor = new(1f, 1f, 1f, 0.18f);

		[Header("View")]
		[SerializeField] private Vector2 panOffset;
		[SerializeField, Min(0.05f)] private float zoom = 1f;
		[Header("Legacy Grid Size")]
		[SerializeField, Min(1)] private int columns = 12;
		[SerializeField, Min(1)] private int rows = 8;

		public int Columns => columns;
		public int Rows => rows;
		public Vector2 PanOffset => panOffset;
		public float Zoom => zoom;
		public float CellSize => cellSize;

		private void Reset()
		{
			raycastTarget = false;
		}
		public void SetGridSize(int columns, int rows)
		{
			this.columns = Mathf.Max(1, columns);
			this.rows = Mathf.Max(1, rows);

			SetVerticesDirty();
		}
		public void Pan(Vector2 localDelta)
		{
			if (localDelta.sqrMagnitude <= 0.000001f)
			{
				return;
			}

			panOffset += localDelta;
			SetVerticesDirty();
		}

		public void SetPanOffset(Vector2 value)
		{
			if ((panOffset - value).sqrMagnitude <= 0.000001f)
			{
				return;
			}

			panOffset = value;
			SetVerticesDirty();
		}

		public void SetZoomAtLocalPoint(float requestedZoom, Vector2 localPoint)
		{
			float oldZoom = Mathf.Max(0.0001f, zoom);
			float newZoom = Mathf.Clamp(requestedZoom, minZoom, maxZoom);

			if (Mathf.Approximately(oldZoom, newZoom))
			{
				return;
			}

			Vector2 graphPointUnderCursor = (localPoint - panOffset) / oldZoom;

			zoom = newZoom;
			panOffset = localPoint - graphPointUnderCursor * zoom;

			SetVerticesDirty();
		}

		public Vector2 LocalToGraph(Vector2 localPoint)
		{
			return (localPoint - panOffset) / Mathf.Max(0.0001f, zoom);
		}

		public Vector2 GraphToLocal(Vector2 graphPoint)
		{
			return panOffset + graphPoint * zoom;
		}

		public void ResetView()
		{
			panOffset = Vector2.zero;
			zoom = 1f;
			SetVerticesDirty();
		}

		protected override void OnPopulateMesh(VertexHelper vertexHelper)
		{
			vertexHelper.Clear();

			Rect rect = rectTransform.rect;

			if (rect.width <= 0f || rect.height <= 0f)
			{
				return;
			}

			if (drawBackground)
			{
				AddQuad(vertexHelper, rect, backgroundColor);
			}

			float step = Mathf.Max(0.0001f, cellSize * zoom);
			int skipMultiplier = 1;

			int safety = 0;

			while ((step < minVisibleCellSize ||
					rect.width / step > maxLinesPerAxis ||
					rect.height / step > maxLinesPerAxis) &&
				   safety < 24)
			{
				step *= 2f;
				skipMultiplier *= 2;
				safety++;
			}

			DrawVerticalLines(vertexHelper, rect, step, skipMultiplier, false);
			DrawHorizontalLines(vertexHelper, rect, step, skipMultiplier, false);

			DrawVerticalLines(vertexHelper, rect, step, skipMultiplier, true);
			DrawHorizontalLines(vertexHelper, rect, step, skipMultiplier, true);
		}

		private void DrawVerticalLines(
			VertexHelper vertexHelper,
			Rect rect,
			float step,
			int skipMultiplier,
			bool majorOnly)
		{
			int firstIndex = Mathf.CeilToInt((rect.xMin - panOffset.x) / step);
			int lastIndex = Mathf.FloorToInt((rect.xMax - panOffset.x) / step);

			for (int index = firstIndex; index <= lastIndex; index++)
			{
				long baseLineIndex = (long)index * skipMultiplier;
				bool isMajor = IsMajorLine(baseLineIndex);

				if (isMajor != majorOnly)
				{
					continue;
				}

				float x = panOffset.x + index * step;
				float thickness = isMajor ? majorLineThickness : lineThickness;
				Color color = isMajor ? majorLineColor : minorLineColor;

				Rect lineRect = Rect.MinMaxRect(
					x - thickness * 0.5f,
					rect.yMin,
					x + thickness * 0.5f,
					rect.yMax
				);

				AddQuad(vertexHelper, lineRect, color);
			}
		}

		private void DrawHorizontalLines(
			VertexHelper vertexHelper,
			Rect rect,
			float step,
			int skipMultiplier,
			bool majorOnly)
		{
			int firstIndex = Mathf.CeilToInt((rect.yMin - panOffset.y) / step);
			int lastIndex = Mathf.FloorToInt((rect.yMax - panOffset.y) / step);

			for (int index = firstIndex; index <= lastIndex; index++)
			{
				long baseLineIndex = (long)index * skipMultiplier;
				bool isMajor = IsMajorLine(baseLineIndex);

				if (isMajor != majorOnly)
				{
					continue;
				}

				float y = panOffset.y + index * step;
				float thickness = isMajor ? majorLineThickness : lineThickness;
				Color color = isMajor ? majorLineColor : minorLineColor;

				Rect lineRect = Rect.MinMaxRect(
					rect.xMin,
					y - thickness * 0.5f,
					rect.xMax,
					y + thickness * 0.5f
				);

				AddQuad(vertexHelper, lineRect, color);
			}
		}

		private bool IsMajorLine(long lineIndex)
		{
			return majorLineEvery > 0 && lineIndex % majorLineEvery == 0;
		}

		private static void AddQuad(VertexHelper vertexHelper, Rect rect, Color color)
		{
			int startIndex = vertexHelper.currentVertCount;

			UIVertex vertex = UIVertex.simpleVert;
			vertex.color = color;

			vertex.position = new Vector3(rect.xMin, rect.yMin);
			vertexHelper.AddVert(vertex);

			vertex.position = new Vector3(rect.xMin, rect.yMax);
			vertexHelper.AddVert(vertex);

			vertex.position = new Vector3(rect.xMax, rect.yMax);
			vertexHelper.AddVert(vertex);

			vertex.position = new Vector3(rect.xMax, rect.yMin);
			vertexHelper.AddVert(vertex);

			vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		protected override void OnRectTransformDimensionsChange()
		{
			base.OnRectTransformDimensionsChange();

			if (isActiveAndEnabled)
			{
				SetVerticesDirty();
			}
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			cellSize = Mathf.Max(4f, cellSize);
			majorLineEvery = Mathf.Max(1, majorLineEvery);
			lineThickness = Mathf.Max(0.1f, lineThickness);
			majorLineThickness = Mathf.Max(0.1f, majorLineThickness);

			if (maxZoom < minZoom)
			{
				maxZoom = minZoom;
			}

			zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

			if (isActiveAndEnabled)
			{
				SetVerticesDirty();
			}
		}
	}
}