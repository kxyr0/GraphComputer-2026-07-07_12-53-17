using System;
using Game.Nodes.Composition;
using UnityEngine;

namespace Game.Nodes.Runtime
{
	public sealed class NodeGraphBootstrapper : MonoBehaviour
	{
		[SerializeField] private NodeFactory nodeFactory;
		[SerializeField] private InitialNodeSpawn[] initialNodes;

		private void Start()
		{
			if (nodeFactory == null)
			{
				Debug.LogError($"[{nameof(NodeGraphBootstrapper)}] Node factory is missing.", this);
				return;
			}

			foreach (InitialNodeSpawn node in initialNodes)
			{
				if (string.IsNullOrWhiteSpace(node.NodeId))
				{
					continue;
				}

				nodeFactory.SpawnById(node.NodeId, node.Position);
			}
		}

		[Serializable]
		private struct InitialNodeSpawn
		{
			[SerializeField] private string nodeId;
			[SerializeField] private Vector2 position;

			public string NodeId => nodeId;
			public Vector2 Position => position;
		}
	}
}