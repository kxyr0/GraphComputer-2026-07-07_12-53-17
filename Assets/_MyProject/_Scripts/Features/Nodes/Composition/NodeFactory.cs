using System;
using System.Collections.Generic;
using Game.IDE.Presentation;
using Game.Nodes.Definitions;
using Game.Nodes.Presentation;
using UnityEngine;

namespace Game.Nodes.Composition
{
	public sealed class NodeFactory : MonoBehaviour
	{
		[SerializeField] private NodeCatalog nodeCatalog;
		[SerializeField] private RectTransform nodesRoot;

		private readonly Dictionary<string, NodeView> uniqueNodes = new();

		public NodeView SpawnById(string nodeId, Vector2 position)
		{
			if (nodeCatalog == null)
			{
				throw new InvalidOperationException("Node catalog is missing.");
			}

			NodeDefinition definition = nodeCatalog.GetRequired(nodeId);
			return Spawn(definition, position);
		}

		public NodeView Spawn(NodeDefinition definition, Vector2 position)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			if (definition.Prefab == null)
			{
				throw new InvalidOperationException($"Node definition '{definition.Id}' has no prefab.");
			}

			if (nodesRoot == null)
			{
				throw new InvalidOperationException("Nodes root is missing.");
			}

			if (definition.IsUnique && uniqueNodes.TryGetValue(definition.Id, out NodeView existingNode))
			{
				if (existingNode != null)
				{
					EnsureIdeDoubleClickOpen(existingNode.gameObject);
					return existingNode;
				}

				uniqueNodes.Remove(definition.Id);
			}

			GameObject instance = Instantiate(definition.Prefab, nodesRoot);

			if (!instance.TryGetComponent(out NodeView nodeView))
			{
				throw new InvalidOperationException(
					$"Node prefab '{definition.Prefab.name}' has no NodeView component."
				);
			}

			EnsureIdeDoubleClickOpen(instance);

			RectTransform rectTransform = instance.transform as RectTransform;

			if (rectTransform != null)
			{
				rectTransform.anchoredPosition = position;
				rectTransform.localScale = Vector3.one;
				rectTransform.localRotation = Quaternion.identity;
			}

			string instanceId = definition.IsUnique
				? definition.Id
				: Guid.NewGuid().ToString("N");

			if (definition.HasJsonDefinition)
			{
				if (!definition.TryReadJsonDefinition(out NodeJsonDefinition jsonDefinition, out string error))
				{
					throw new InvalidOperationException(
						$"Failed to read JSON for node '{definition.Id}': {error}"
					);
				}

				nodeView.Bind(instanceId, definition, jsonDefinition);
			}
			else
			{
				nodeView.Bind(instanceId, definition);
			}

			if (definition.IsUnique)
			{
				uniqueNodes[definition.Id] = nodeView;
			}

			return nodeView;
		}

		private static void EnsureIdeDoubleClickOpen(GameObject instance)
		{
			if (instance == null)
			{
				return;
			}

			if (!instance.TryGetComponent<IdeOpenNodeOnDoubleClick>(out _))
			{
				instance.AddComponent<IdeOpenNodeOnDoubleClick>();
			}
		}
	}
}
