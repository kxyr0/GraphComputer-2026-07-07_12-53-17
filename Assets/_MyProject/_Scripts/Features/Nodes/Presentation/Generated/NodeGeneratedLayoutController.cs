using Game.Nodes.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	internal sealed class NodeGeneratedLayoutController
	{
		private readonly RectTransform nodeRectTransform;
		private readonly RectTransform statsRoot;
		private readonly RectTransform inputPortsRoot;
		private readonly RectTransform outputPortsRoot;
		private readonly NodeViewVisualPresenter visualPresenter;
		private readonly bool isAutoCompactLayout;
		private readonly bool isJsonOnlySizeMode;
		private readonly bool manualApplyJsonSize;
		private readonly bool manualApplyHeaderLayout;
		private readonly bool manualForceLayoutRebuild;
		private readonly Vector2 minimumNodeSize;
		private readonly float headerHeight;
		private readonly float safeTopPadding;
		private readonly float safeBottomPadding;
		private readonly float safeSidePadding;
		private readonly float columnGap;
		private readonly float statsToPortsGap;
		private readonly float generatedRowHeight;
		private readonly float generatedRowSpacing;

		public NodeGeneratedLayoutController(
			RectTransform nodeRectTransform,
			RectTransform statsRoot,
			RectTransform inputPortsRoot,
			RectTransform outputPortsRoot,
			NodeViewVisualPresenter visualPresenter,
			bool isAutoCompactLayout,
			bool isJsonOnlySizeMode,
			bool manualApplyJsonSize,
			bool manualApplyHeaderLayout,
			bool manualForceLayoutRebuild,
			Vector2 minimumNodeSize,
			float headerHeight,
			float safeTopPadding,
			float safeBottomPadding,
			float safeSidePadding,
			float columnGap,
			float statsToPortsGap,
			float generatedRowHeight,
			float generatedRowSpacing)
		{
			this.nodeRectTransform = nodeRectTransform;
			this.statsRoot = statsRoot;
			this.inputPortsRoot = inputPortsRoot;
			this.outputPortsRoot = outputPortsRoot;
			this.visualPresenter = visualPresenter;
			this.isAutoCompactLayout = isAutoCompactLayout;
			this.isJsonOnlySizeMode = isJsonOnlySizeMode;
			this.manualApplyJsonSize = manualApplyJsonSize;
			this.manualApplyHeaderLayout = manualApplyHeaderLayout;
			this.manualForceLayoutRebuild = manualForceLayoutRebuild;
			this.minimumNodeSize = minimumNodeSize;
			this.headerHeight = headerHeight;
			this.safeTopPadding = safeTopPadding;
			this.safeBottomPadding = safeBottomPadding;
			this.safeSidePadding = safeSidePadding;
			this.columnGap = columnGap;
			this.statsToPortsGap = statsToPortsGap;
			this.generatedRowHeight = generatedRowHeight;
			this.generatedRowSpacing = generatedRowSpacing;
		}

		public void UpdateGeneratedLayoutAndSize(NodeJsonDefinition definition)
		{
			if (definition == null)
			{
				return;
			}

			if (!isAutoCompactLayout)
			{
				ApplyManualPrefabMode(definition);
				return;
			}

			ApplyAutoCompactMode(definition);
		}

		public void ApplyManualPrefabMode(NodeJsonDefinition definition)
		{
			if (manualApplyJsonSize)
			{
				SetNodeSize(ResolveJsonSize(definition));
			}

			if (manualApplyHeaderLayout)
			{
				visualPresenter?.ApplyHeaderLayout();
			}

			if (manualForceLayoutRebuild)
			{
				RebuildGeneratedLayouts();
			}
		}

		public void ApplyAutoCompactMode(NodeJsonDefinition definition)
		{
			Vector2 jsonSize = ResolveJsonSize(definition);
			float width = jsonSize.x;

			if (isJsonOnlySizeMode)
			{
				SetNodeSize(jsonSize);
				visualPresenter?.ApplyHeaderLayout();
				PrepareGeneratedRootWidths(width);
				RebuildGeneratedLayouts();
				ApplyGeneratedContentLayout();
				RebuildGeneratedLayouts();

				return;
			}

			float provisionalHeight = Mathf.Max(minimumNodeSize.y, jsonSize.y);

			SetNodeSize(new Vector2(width, provisionalHeight));
			visualPresenter?.ApplyHeaderLayout();
			PrepareGeneratedRootWidths(width);
			RebuildGeneratedLayouts();

			float statsHeight = GetGeneratedRootPreferredHeight(statsRoot);
			float inputPortsHeight = GetGeneratedRootPreferredHeight(inputPortsRoot);
			float outputPortsHeight = GetGeneratedRootPreferredHeight(outputPortsRoot);

			float portsHeight = Mathf.Max(inputPortsHeight, outputPortsHeight);

			float gap = statsHeight > 0f && portsHeight > 0f
				? statsToPortsGap
				: 0f;

			float autoHeight =
				headerHeight +
				safeTopPadding +
				statsHeight +
				gap +
				portsHeight +
				safeBottomPadding;

			float height = Mathf.Max(minimumNodeSize.y, jsonSize.y, autoHeight);

			SetNodeSize(new Vector2(width, height));
			visualPresenter?.ApplyHeaderLayout();
			PrepareGeneratedRootWidths(width);
			RebuildGeneratedLayouts();
			ApplyGeneratedContentLayout();
			RebuildGeneratedLayouts();
		}

		public Vector2 ResolveJsonSize(NodeJsonDefinition definition)
		{
			Vector2 currentSize = nodeRectTransform != null
				? nodeRectTransform.rect.size
				: minimumNodeSize;

			if (currentSize.x <= 0f)
			{
				currentSize.x = minimumNodeSize.x;
			}

			if (currentSize.y <= 0f)
			{
				currentSize.y = minimumNodeSize.y;
			}

			float width = definition != null && definition.size.x > 0f
				? definition.size.x
				: currentSize.x;

			float height = definition != null && definition.size.y > 0f
				? definition.size.y
				: currentSize.y;

			return new Vector2(
				Mathf.Max(1f, width),
				Mathf.Max(1f, height));
		}

		public void SetNodeSize(Vector2 size)
		{
			if (nodeRectTransform == null)
			{
				return;
			}

			nodeRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
			nodeRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
		}

		public void PrepareGeneratedRootWidths(float nodeWidth)
		{
			float fullWidth = Mathf.Max(1f, nodeWidth - safeSidePadding * 2f);
			float portColumnWidth = Mathf.Max(1f, (fullWidth - columnGap) * 0.5f);

			PrepareGeneratedRoot(statsRoot, fullWidth);
			PrepareGeneratedRoot(inputPortsRoot, portColumnWidth);
			PrepareGeneratedRoot(outputPortsRoot, portColumnWidth);
		}

		public void PrepareGeneratedRoot(RectTransform root, float width)
		{
			if (root == null)
			{
				return;
			}

			root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

			VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();

			if (layout == null)
			{
				layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
			}

			layout.padding = new RectOffset(0, 0, 0, 0);
			layout.childAlignment = TextAnchor.UpperLeft;
			layout.spacing = generatedRowSpacing;

			layout.childControlWidth = true;
			layout.childControlHeight = true;

			layout.childForceExpandWidth = false;
			layout.childForceExpandHeight = false;
		}

		public void ApplyGeneratedContentLayout()
		{
			Rect safeRect = GetNodeSafeLocalRect();

			float statsHeight = GetGeneratedRootPreferredHeight(statsRoot);
			float inputPortsHeight = GetGeneratedRootPreferredHeight(inputPortsRoot);
			float outputPortsHeight = GetGeneratedRootPreferredHeight(outputPortsRoot);

			float portsHeight = Mathf.Max(inputPortsHeight, outputPortsHeight);

			float gap = statsHeight > 0f && portsHeight > 0f
				? statsToPortsGap
				: 0f;

			float portsTop = safeRect.yMax - statsHeight - gap;
			float portColumnWidth = Mathf.Max(1f, (safeRect.width - columnGap) * 0.5f);

			PlaceTopLeft(
				statsRoot,
				safeRect.xMin,
				safeRect.yMax,
				safeRect.width,
				statsHeight);

			PlaceTopLeft(
				inputPortsRoot,
				safeRect.xMin,
				portsTop,
				portColumnWidth,
				inputPortsHeight);

			PlaceTopRight(
				outputPortsRoot,
				safeRect.xMax,
				portsTop,
				portColumnWidth,
				outputPortsHeight);
		}

		public Rect GetNodeSafeLocalRect()
		{
			Rect nodeRect = nodeRectTransform != null
				? nodeRectTransform.rect
				: Rect.MinMaxRect(0f, 0f, 1f, 1f);

			float left = nodeRect.xMin + safeSidePadding;
			float right = nodeRect.xMax - safeSidePadding;

			float top = nodeRect.yMax - headerHeight - safeTopPadding;
			float bottom = nodeRect.yMin + safeBottomPadding;

			if (right <= left)
			{
				float center = (left + right) * 0.5f;
				left = center - 0.5f;
				right = center + 0.5f;
			}

			if (top <= bottom)
			{
				top = bottom + 1f;
			}

			return Rect.MinMaxRect(left, bottom, right, top);
		}

		public void PlaceTopLeft(
			RectTransform rect,
			float localLeft,
			float localTop,
			float width,
			float height)
		{
			if (rect == null || nodeRectTransform == null)
			{
				return;
			}

			Rect parentRect = nodeRectTransform.rect;

			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(0f, 1f);
			rect.pivot = new Vector2(0f, 1f);

			rect.anchoredPosition = new Vector2(
				localLeft - parentRect.xMin,
				localTop - parentRect.yMax);

			rect.sizeDelta = new Vector2(width, height);
		}

		public void PlaceTopRight(
			RectTransform rect,
			float localRight,
			float localTop,
			float width,
			float height)
		{
			if (rect == null || nodeRectTransform == null)
			{
				return;
			}

			Rect parentRect = nodeRectTransform.rect;

			rect.anchorMin = new Vector2(1f, 1f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.pivot = new Vector2(1f, 1f);

			rect.anchoredPosition = new Vector2(
				localRight - parentRect.xMax,
				localTop - parentRect.yMax);

			rect.sizeDelta = new Vector2(width, height);
		}

		public float GetGeneratedRootPreferredHeight(RectTransform root)
		{
			int visibleChildren = CountVisibleGeneratedChildren(root);

			if (visibleChildren == 0)
			{
				return 0f;
			}

			float height = visibleChildren * generatedRowHeight;
			height += Mathf.Max(0, visibleChildren - 1) * generatedRowSpacing;

			VerticalLayoutGroup layout = root != null
				? root.GetComponent<VerticalLayoutGroup>()
				: null;

			if (layout != null)
			{
				height += layout.padding.vertical;
			}

			return height;
		}

		public int CountVisibleGeneratedChildren(RectTransform root)
		{
			if (root == null)
			{
				return 0;
			}

			int count = 0;

			for (int i = 0; i < root.childCount; i++)
			{
				Transform child = root.GetChild(i);

				if (!child.gameObject.activeSelf)
				{
					continue;
				}

				if (child.GetComponent<NodeGeneratedElement>() == null)
				{
					continue;
				}

				count++;
			}

			return count;
		}

		public void RebuildGeneratedLayouts()
		{
			RebuildLayout(statsRoot);
			RebuildLayout(inputPortsRoot);
			RebuildLayout(outputPortsRoot);
			RebuildLayout(nodeRectTransform);
		}

		private static void RebuildLayout(RectTransform root)
		{
			if (root == null)
			{
				return;
			}

			LayoutRebuilder.ForceRebuildLayoutImmediate(root);
		}
	}
}
