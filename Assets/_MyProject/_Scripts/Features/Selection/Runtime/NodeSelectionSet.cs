using System.Collections.Generic;

namespace Game.Selection.Runtime
{
	public sealed class NodeSelectionSet
	{
		private readonly HashSet<SelectableNode> selectedNodes = new();
		private readonly HashSet<SelectableNode> targetNodes = new();

		public IReadOnlyCollection<SelectableNode> SelectedNodes => selectedNodes;

		public int Count => selectedNodes.Count;

		public bool Contains(SelectableNode node)
		{
			return node != null && selectedNodes.Contains(node);
		}

		public void Clear(IEnumerable<SelectableNode> allNodes)
		{
			selectedNodes.Clear();

			if (allNodes == null)
			{
				return;
			}

			foreach (SelectableNode node in allNodes)
			{
				if (node != null)
				{
					node.SetSelected(false);
				}
			}
		}

		public void SetSelected(SelectableNode node, bool selected)
		{
			if (node == null || !node.CanSelect)
			{
				return;
			}

			if (selected)
			{
				selectedNodes.Add(node);
			}
			else
			{
				selectedNodes.Remove(node);
			}

			node.SetSelected(selected);
		}

		public void Remove(SelectableNode node)
		{
			if (node == null)
			{
				return;
			}

			selectedNodes.Remove(node);
			node.SetSelected(false);
		}

		public void Apply(
			IEnumerable<SelectableNode> targetSelection,
			IEnumerable<SelectableNode> allNodes)
		{
			targetNodes.Clear();

			if (targetSelection != null)
			{
				foreach (SelectableNode node in targetSelection)
				{
					if (node != null && node.CanSelect)
					{
						targetNodes.Add(node);
					}
				}
			}

			selectedNodes.Clear();

			if (allNodes == null)
			{
				foreach (SelectableNode node in targetNodes)
				{
					node.SetSelected(true);
					selectedNodes.Add(node);
				}

				return;
			}

			foreach (SelectableNode node in allNodes)
			{
				if (node == null)
				{
					continue;
				}

				bool shouldSelect = targetNodes.Contains(node);
				node.SetSelected(shouldSelect);

				if (shouldSelect)
				{
					selectedNodes.Add(node);
				}
			}
		}
	}
}
