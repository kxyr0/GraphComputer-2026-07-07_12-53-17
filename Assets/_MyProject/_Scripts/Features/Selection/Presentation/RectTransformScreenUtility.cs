using UnityEngine;

namespace Game.Selection.Presentation
{
	internal static class RectTransformScreenUtility
	{
		private static readonly Vector3[] WorldCorners = new Vector3[4];

		public static Rect CreateRect(Vector2 firstPoint, Vector2 secondPoint)
		{
			float xMin = Mathf.Min(firstPoint.x, secondPoint.x);
			float yMin = Mathf.Min(firstPoint.y, secondPoint.y);
			float xMax = Mathf.Max(firstPoint.x, secondPoint.x);
			float yMax = Mathf.Max(firstPoint.y, secondPoint.y);

			return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
		}

		public static bool TryGetScreenRect(
			RectTransform rectTransform,
			Camera fallbackCamera,
			out Rect screenRect)
		{
			screenRect = default;

			if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
			{
				return false;
			}

			Camera screenCamera = ResolveScreenCamera(rectTransform, fallbackCamera);

			rectTransform.GetWorldCorners(WorldCorners);

			Vector2 min = RectTransformUtility.WorldToScreenPoint(screenCamera, WorldCorners[0]);
			Vector2 max = min;

			for (int i = 1; i < WorldCorners.Length; i++)
			{
				Vector2 point = RectTransformUtility.WorldToScreenPoint(screenCamera, WorldCorners[i]);
				min = Vector2.Min(min, point);
				max = Vector2.Max(max, point);
			}

			screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
			return screenRect.width > 0f && screenRect.height > 0f;
		}

		private static Camera ResolveScreenCamera(
			RectTransform rectTransform,
			Camera fallbackCamera)
		{
			Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

			if (canvas == null)
			{
				return fallbackCamera != null ? fallbackCamera : Camera.main;
			}

			if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
			{
				return null;
			}

			if (canvas.worldCamera != null)
			{
				return canvas.worldCamera;
			}

			return fallbackCamera != null ? fallbackCamera : Camera.main;
		}

		public static bool ContainsCenter(Rect selectionRect, Rect targetRect)
		{
			return selectionRect.Contains(targetRect.center, true);
		}

		public static bool ContainsRect(Rect selectionRect, Rect targetRect)
		{
			return selectionRect.Contains(targetRect.min, true) &&
				   selectionRect.Contains(targetRect.max, true);
		}
	}
}
