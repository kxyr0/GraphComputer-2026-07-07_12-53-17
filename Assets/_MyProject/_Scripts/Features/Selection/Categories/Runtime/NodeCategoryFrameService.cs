using System.Collections.Generic;
using Game.Selection.Categories.Presentation;
using Game.Selection.Runtime;
using UnityEngine;

namespace Game.Selection.Categories.Runtime
{
	[DisallowMultipleComponent]
	public sealed class NodeCategoryFrameService : MonoBehaviour
	{
		[SerializeField] private NodeSelectionService selectionService;
		[SerializeField] private RectTransform nodesRoot;
		[SerializeField] private RectTransform frameRoot;
		[SerializeField] private NodeCategoryFrameView framePrefab;
		[SerializeField, Min(2)] private int minNodes = 2;
		[SerializeField, Min(0f)] private float padding = 48f;
		[SerializeField] private string categoryTitlePrefix = "Category";
		[SerializeField] private bool placeBehindNodes;

		private readonly List<SelectableNode> selectedNodes = new();
		private int createdCount;

		public int MinNodes => minNodes;

		public bool CanCreateFromCurrentSelection
		{
			get
			{
				ResolveReferences();
				return selectionService != null &&
					   framePrefab != null &&
					   ResolveFrameParent() != null &&
					   selectionService.SelectedCount >= minNodes;
			}
		}

		private void Awake()
		{
			ResolveReferences();
		}

		public bool TryCreateFromCurrentSelection()
		{
			ResolveReferences();

			if (selectionService == null)
			{
				Debug.LogWarning($"[{nameof(NodeCategoryFrameService)}] Selection service is missing.", this);
				return false;
			}

			selectionService.CopySelected(selectedNodes);

			if (selectedNodes.Count < minNodes)
			{
				return false;
			}

			RectTransform parent = ResolveFrameParent();

			if (parent == null)
			{
				Debug.LogWarning($"[{nameof(NodeCategoryFrameService)}] Frame root is missing.", this);
				return false;
			}

			if (framePrefab == null)
			{
				Debug.LogWarning($"[{nameof(NodeCategoryFrameService)}] Frame prefab is missing.", this);
				return false;
			}

			if (!SelectedNodeBoundsCalculator.TryCalculate(selectedNodes, parent, padding, out Rect bounds))
			{
				return false;
			}

			NodeCategoryFrameView frame = Instantiate(framePrefab, parent);
			frame.gameObject.SetActive(true);
			frame.Bind(BuildTitle(), bounds, selectedNodes.Count);

			if (placeBehindNodes)
			{
				frame.transform.SetAsFirstSibling();
			}

			selectedNodes.Clear();
			return true;
		}

		private string BuildTitle()
		{
			createdCount++;
			return $"{categoryTitlePrefix} {createdCount}";
		}

		private RectTransform ResolveFrameParent()
		{
			if (frameRoot != null)
			{
				return frameRoot;
			}

			if (nodesRoot != null)
			{
				return nodesRoot;
			}

			foreach (SelectableNode node in selectedNodes)
			{
				if (node != null && node.RectTransform != null)
				{
					return node.RectTransform.parent as RectTransform;
				}
			}

			return null;
		}

		private void ResolveReferences()
		{
			if (selectionService == null)
			{
				selectionService = FindFirstObjectByType<NodeSelectionService>();
			}

			if (nodesRoot != null)
			{
				return;
			}

			if (selectionService != null)
			{
				nodesRoot = selectionService.transform as RectTransform;
			}
		}
	}
}
