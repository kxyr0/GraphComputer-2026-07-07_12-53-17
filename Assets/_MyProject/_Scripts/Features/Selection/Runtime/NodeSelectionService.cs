using System.Collections.Generic;
using UnityEngine;

namespace Game.Selection.Runtime
{
	[DisallowMultipleComponent]
	public sealed class NodeSelectionService : MonoBehaviour
	{
		[SerializeField] private RectTransform nodesRoot;

		private readonly List<SelectableNode> selectableNodes = new();
		private readonly NodeSelectionSet selectionSet = new();

		public IReadOnlyCollection<SelectableNode> SelectedNodes => selectionSet.SelectedNodes;

		public int SelectedCount => selectionSet.Count;

		private void Reset()
		{
			if (nodesRoot == null && transform is RectTransform rectTransform)
			{
				nodesRoot = rectTransform;
			}
		}

		public void CollectSelectableNodes(List<SelectableNode> buffer)
		{
			if (buffer == null)
			{
				return;
			}

			buffer.Clear();

			if (nodesRoot == null)
			{
				return;
			}

			nodesRoot.GetComponentsInChildren(false, buffer);

			for (int i = buffer.Count - 1; i >= 0; i--)
			{
				SelectableNode node = buffer[i];

				if (node == null || !node.CanSelect)
				{
					buffer.RemoveAt(i);
				}
			}
		}

		public void CaptureSelected(HashSet<SelectableNode> buffer)
		{
			if (buffer == null)
			{
				return;
			}

			buffer.Clear();
			CollectSelectableNodes(selectableNodes);

			foreach (SelectableNode node in selectableNodes)
			{
				if (node != null && node.IsSelected)
				{
					buffer.Add(node);
				}
			}
		}

		public void CopySelected(List<SelectableNode> buffer)
		{
			if (buffer == null)
			{
				return;
			}

			buffer.Clear();
			CollectSelectableNodes(selectableNodes);

			foreach (SelectableNode node in selectableNodes)
			{
				if (node != null && node.IsSelected && node.CanSelect)
				{
					buffer.Add(node);
				}
			}
		}

		public void ClearSelection()
		{
			CollectSelectableNodes(selectableNodes);
			selectionSet.Clear(selectableNodes);
		}

		public void ApplySelection(IEnumerable<SelectableNode> targetSelection)
		{
			CollectSelectableNodes(selectableNodes);
			selectionSet.Apply(targetSelection, selectableNodes);
		}

		public void SelectSingle(SelectableNode node)
		{
			if (node == null || !node.CanSelect)
			{
				return;
			}

			CollectSelectableNodes(selectableNodes);
			selectionSet.Clear(selectableNodes);
			selectionSet.SetSelected(node, true);
		}

		public void AddToSelection(SelectableNode node)
		{
			selectionSet.SetSelected(node, true);
		}

		public void ToggleSelection(SelectableNode node)
		{
			if (node == null || !node.CanSelect)
			{
				return;
			}

			selectionSet.SetSelected(node, !node.IsSelected);
		}

		public void RemoveFromSelection(SelectableNode node)
		{
			selectionSet.Remove(node);
		}
	}
}
