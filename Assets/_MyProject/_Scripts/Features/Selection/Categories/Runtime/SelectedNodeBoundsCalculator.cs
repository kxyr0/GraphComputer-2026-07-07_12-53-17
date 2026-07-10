using System.Collections.Generic;
using Game.Selection.Runtime;
using UnityEngine;

namespace Game.Selection.Categories.Runtime
{
	public static class SelectedNodeBoundsCalculator
	{
		private static readonly Vector3[] WorldCorners = new Vector3[4];

		public static bool TryCalculate(
			IReadOnlyList<SelectableNode> selectedNodes,
			RectTransform boundsRoot,
			float padding,
			out Rect bounds)
		{
			bounds = default;

			if (selectedNodes == null || selectedNodes.Count == 0 || boundsRoot == null)
			{
				return false;
			}

			bool hasAnyCorner = false;
			Vector2 min = Vector2.zero;
			Vector2 max = Vector2.zero;

			foreach (SelectableNode node in selectedNodes)
			{
				RectTransform rectTransform = node != null ? node.RectTransform : null;

				if (rectTransform == null)
				{
					continue;
				}

				rectTransform.GetWorldCorners(WorldCorners);

				for (int i = 0; i < WorldCorners.Length; i++)
				{
					Vector2 localPoint = boundsRoot.InverseTransformPoint(WorldCorners[i]);

					if (!hasAnyCorner)
					{
						min = localPoint;
						max = localPoint;
						hasAnyCorner = true;
						continue;
					}

					min = Vector2.Min(min, localPoint);
					max = Vector2.Max(max, localPoint);
				}
			}

			if (!hasAnyCorner)
			{
				return false;
			}

			float safePadding = Mathf.Max(0f, padding);
			min -= Vector2.one * safePadding;
			max += Vector2.one * safePadding;

			bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
			return true;
		}
	}
}
