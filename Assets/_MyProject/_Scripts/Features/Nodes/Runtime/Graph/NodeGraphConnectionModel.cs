using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Nodes.Runtime
{
	public sealed class NodeGraphConnectionModel : MonoBehaviour
	{
		[SerializeField] private List<NodeConnectionData> connections = new();

		public IReadOnlyList<NodeConnectionData> Connections => connections;

		public event Action<NodeConnectionData> ConnectionAdded;
		public event Action<NodeConnectionData> ConnectionRemoved;

		public bool TryAddConnection(NodeConnectionData connection)
		{
			if (connection == null)
			{
				return false;
			}

			if (ContainsConnection(
					connection.FromNodeId,
					connection.FromPortId,
					connection.ToNodeId,
					connection.ToPortId))
			{
				return false;
			}

			connections.Add(connection);
			ConnectionAdded?.Invoke(connection);

			Debug.Log(
				$"[{nameof(NodeGraphConnectionModel)}] Added connection: {connection.Id}",
				this);

			return true;
		}

		public bool ContainsConnection(
			string fromNodeId,
			string fromPortId,
			string toNodeId,
			string toPortId)
		{
			foreach (NodeConnectionData connection in connections)
			{
				if (connection == null)
				{
					continue;
				}

				if (connection.Matches(fromNodeId, fromPortId, toNodeId, toPortId))
				{
					return true;
				}
			}

			return false;
		}

		public bool TryRemoveConnection(
			string fromNodeId,
			string fromPortId,
			string toNodeId,
			string toPortId)
		{
			for (int i = connections.Count - 1; i >= 0; i--)
			{
				NodeConnectionData connection = connections[i];

				if (connection == null)
				{
					connections.RemoveAt(i);
					continue;
				}

				if (!connection.Matches(fromNodeId, fromPortId, toNodeId, toPortId))
				{
					continue;
				}

				connections.RemoveAt(i);
				ConnectionRemoved?.Invoke(connection);

				Debug.Log(
					$"[{nameof(NodeGraphConnectionModel)}] Removed connection: {connection.Id}",
					this);

				return true;
			}

			return false;
		}

		public int RemoveConnectionsForNode(string nodeId)
		{
			if (string.IsNullOrWhiteSpace(nodeId))
			{
				return 0;
			}

			int removedCount = 0;

			for (int i = connections.Count - 1; i >= 0; i--)
			{
				NodeConnectionData connection = connections[i];

				if (connection == null)
				{
					connections.RemoveAt(i);
					continue;
				}

				if (!connection.IsAttachedToNode(nodeId))
				{
					continue;
				}

				connections.RemoveAt(i);
				removedCount++;
				ConnectionRemoved?.Invoke(connection);

				Debug.Log(
					$"[{nameof(NodeGraphConnectionModel)}] Removed connection: {connection.Id}",
					this);
			}

			return removedCount;
		}

		[ContextMenu("Clear Connections")]
		public void Clear()
		{
			connections.Clear();
		}

		[ContextMenu("Print Connections")]
		private void PrintConnections()
		{
			if (connections.Count == 0)
			{
				Debug.Log("[NodeGraphConnectionModel] No connections.", this);
				return;
			}

			foreach (NodeConnectionData connection in connections)
			{
				if (connection == null)
				{
					continue;
				}

				Debug.Log(connection.Id, this);
			}
		}
	}
}
