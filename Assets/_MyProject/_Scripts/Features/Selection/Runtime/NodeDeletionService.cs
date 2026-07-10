using System.Collections.Generic;
using Game.Nodes.Presentation;
using UnityEngine;

namespace Game.Selection.Runtime
{
	[DisallowMultipleComponent]
	public sealed class NodeDeletionService : MonoBehaviour
	{
		[SerializeField] private NodeSelectionService selectionService;
		[SerializeField] private NodeConnectionDragController connectionDragController;

		private readonly List<SelectableNode> nodesToDelete = new();

		private void Awake()
		{
			ResolveReferences();
		}

		public void DeleteSelected()
		{
			ResolveReferences();

			if (selectionService == null)
			{
				Debug.LogWarning(
					$"[{nameof(NodeDeletionService)}] Selection service is missing.",
					this);

				return;
			}

			selectionService.CopySelected(nodesToDelete);

			foreach (SelectableNode selectableNode in nodesToDelete)
			{
				Delete(selectableNode);
			}

			nodesToDelete.Clear();
		}

		public bool Delete(SelectableNode selectableNode)
		{
			if (selectableNode == null || !CanDelete(selectableNode))
			{
				return false;
			}

			NodeView nodeView = selectableNode.NodeView;

			if (connectionDragController != null && nodeView != null)
			{
				connectionDragController.DeleteConnectionsForNode(nodeView);
			}

			selectionService?.RemoveFromSelection(selectableNode);
			DestroyNode(selectableNode.gameObject);
			return true;
		}

		private void ResolveReferences()
		{
			if (selectionService == null)
			{
				selectionService = FindFirstObjectByType<NodeSelectionService>();
			}

			if (connectionDragController == null)
			{
				connectionDragController = FindFirstObjectByType<NodeConnectionDragController>();
			}
		}

		private static bool CanDelete(SelectableNode selectableNode)
		{
			NodeView nodeView = selectableNode.NodeView;

			if (nodeView == null || nodeView.Definition == null)
			{
				return true;
			}

			return nodeView.Definition.CanDelete;
		}

		private static void DestroyNode(GameObject nodeObject)
		{
			if (nodeObject == null)
			{
				return;
			}

			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(nodeObject);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(nodeObject);
			}
		}
	}
}
