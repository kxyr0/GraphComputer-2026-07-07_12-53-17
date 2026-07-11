using System;
using Game.Research;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Research
{
	[Serializable]
	public sealed class ResearchScrollContentLayout
	{
		[SerializeField] private bool resizeContent = true;
		[SerializeField] private bool centerScrollOnRebuild = true;
		[SerializeField] private Vector2 padding = new(240f, 180f);
		[SerializeField] private Vector2 minContentSize = new(900f, 520f);

		public void Apply(
			RectTransform contentRoot,
			RectTransform buttonsRoot,
			RectTransform linkLinesRoot,
			ScrollRect scrollRect,
			ResearchConfig config,
			string tabId,
			ResearchGridLayout gridLayout,
			Vector2 itemSize,
			Action<string> logWarning)
		{
			if (!resizeContent || contentRoot == null || config == null || gridLayout == null)
			{
				return;
			}

			Vector2 requiredSize = CalculateRequiredSize(config, tabId, gridLayout, itemSize);

			if (scrollRect != null)
			{
				scrollRect.content = contentRoot;
				requiredSize = Vector2.Max(requiredSize, GetViewportSize(scrollRect));
			}

			requiredSize = Vector2.Max(requiredSize, minContentSize);
			ApplySize(contentRoot, requiredSize);
			StretchToContent(contentRoot, buttonsRoot);
			StretchToContent(contentRoot, linkLinesRoot);
			WarnIfRootIsOutsideContent(contentRoot, buttonsRoot, "Buttons Root", logWarning);
			WarnIfRootIsOutsideContent(contentRoot, linkLinesRoot, "Link Lines Root", logWarning);

			if (scrollRect != null && centerScrollOnRebuild)
			{
				scrollRect.normalizedPosition = new Vector2(0.5f, 0.5f);
			}
		}

		private Vector2 CalculateRequiredSize(
			ResearchConfig config,
			string tabId,
			ResearchGridLayout gridLayout,
			Vector2 itemSize)
		{
			bool hasAnyResearch = false;
			float maxAbsX = 0f;
			float maxAbsY = 0f;
			Vector2 halfItemSize = itemSize * 0.5f;

			foreach (ResearchDefinition research in config.Researches)
			{
				if (research == null || !research.IsInTab(tabId))
				{
					continue;
				}

				hasAnyResearch = true;
				Vector2 position = gridLayout.GetAnchoredPosition(research.GridPosition);
				maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(position.x) + halfItemSize.x);
				maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(position.y) + halfItemSize.y);
			}

			if (!hasAnyResearch)
			{
				return minContentSize;
			}

			return new Vector2(
				maxAbsX * 2f + padding.x * 2f,
				maxAbsY * 2f + padding.y * 2f
			);
		}

		private static Vector2 GetViewportSize(ScrollRect scrollRect)
		{
			if (scrollRect.viewport != null)
			{
				return scrollRect.viewport.rect.size;
			}

			return scrollRect.transform is RectTransform rectTransform
				? rectTransform.rect.size
				: Vector2.zero;
		}

		private static void ApplySize(RectTransform target, Vector2 size)
		{
			target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
			target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
		}

		private static void StretchToContent(RectTransform contentRoot, RectTransform target)
		{
			if (target == null || target == contentRoot)
			{
				return;
			}

			if (!target.IsChildOf(contentRoot))
			{
				return;
			}

			target.anchorMin = Vector2.zero;
			target.anchorMax = Vector2.one;
			target.pivot = new Vector2(0.5f, 0.5f);
			target.offsetMin = Vector2.zero;
			target.offsetMax = Vector2.zero;
			target.localScale = Vector3.one;
			target.localRotation = Quaternion.identity;
		}

		private static void WarnIfRootIsOutsideContent(
			RectTransform contentRoot,
			RectTransform target,
			string label,
			Action<string> logWarning)
		{
			if (target == null || target == contentRoot || target.IsChildOf(contentRoot))
			{
				return;
			}

			logWarning?.Invoke(
				$"{label} is outside Scroll Content Root. It will not move with the scroll content.");
		}
	}
}
