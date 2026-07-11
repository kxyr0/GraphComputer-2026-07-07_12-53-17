using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Research
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	[RequireComponent(typeof(CanvasRenderer))]
	public sealed class ResearchLinkLineView : Graphic
	{
		[SerializeField, Min(1f)] private float thickness = 3f;
		[SerializeField] private bool trimEndpoints = true;
		[SerializeField, Min(0f)] private float endpointTrim = 34f;
		[SerializeField] private bool drawVisibilityHalo = true;
		[SerializeField, Min(1f)] private float haloThicknessMultiplier = 2.8f;
		[SerializeField, Range(0f, 1f)] private float haloAlpha = 0.32f;

		private RectTransform from;
		private RectTransform to;
		private RectTransform selfRect;

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
			raycastTarget = false;
		}

		private void LateUpdate()
		{
			if (from != null && to != null)
			{
				SetVerticesDirty();
			}
		}

		public void Bind(RectTransform fromTarget, RectTransform toTarget, Color lineColor)
		{
			from = fromTarget;
			to = toTarget;
			color = lineColor;

			SetVerticesDirty();
		}

		protected override void OnPopulateMesh(VertexHelper vertexHelper)
		{
			vertexHelper.Clear();

			if (from == null || to == null)
			{
				return;
			}

			Vector2 start = GetLocalCenter(from);
			Vector2 end = GetLocalCenter(to);
			Vector2 direction = end - start;

			if (direction.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			Vector2 normalizedDirection = direction.normalized;

			if (trimEndpoints)
			{
				float safeTrim = Mathf.Min(endpointTrim, direction.magnitude * 0.45f);
				start += normalizedDirection * safeTrim;
				end -= normalizedDirection * safeTrim;
				direction = end - start;
			}

			if (direction.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			if (drawVisibilityHalo && haloAlpha > 0f)
			{
				Color haloColor = color;
				haloColor.a *= haloAlpha;
				AddLineQuad(
					vertexHelper,
					start,
					end,
					Mathf.Max(thickness, thickness * haloThicknessMultiplier),
					haloColor
				);
			}

			AddLineQuad(vertexHelper, start, end, thickness, color);
		}

		private static void AddLineQuad(
			VertexHelper vertexHelper,
			Vector2 start,
			Vector2 end,
			float lineThickness,
			Color32 lineColor)
		{
			int vertexStartIndex = vertexHelper.currentVertCount;
			Vector2 direction = end - start;
			Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (lineThickness * 0.5f);

			UIVertex vertex = UIVertex.simpleVert;
			vertex.color = lineColor;

			vertex.position = start - normal;
			vertexHelper.AddVert(vertex);

			vertex.position = start + normal;
			vertexHelper.AddVert(vertex);

			vertex.position = end + normal;
			vertexHelper.AddVert(vertex);

			vertex.position = end - normal;
			vertexHelper.AddVert(vertex);

			vertexHelper.AddTriangle(vertexStartIndex, vertexStartIndex + 1, vertexStartIndex + 2);
			vertexHelper.AddTriangle(vertexStartIndex, vertexStartIndex + 2, vertexStartIndex + 3);
		}

		private Vector2 GetLocalCenter(RectTransform target)
		{
			Vector3 worldCenter = target.TransformPoint(target.rect.center);
			return SelfRect.InverseTransformPoint(worldCenter);
		}
	}
}
