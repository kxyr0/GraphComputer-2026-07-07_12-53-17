using System;
using UnityEngine;

namespace Game.Nodes.Runtime
{
	[Serializable]
	public sealed class NodeConnectionData
	{
		[SerializeField] private string id;

		[SerializeField] private string fromNodeId;
		[SerializeField] private string fromNodeDefinitionId;
		[SerializeField] private string fromPortId;

		[SerializeField] private string toNodeId;
		[SerializeField] private string toNodeDefinitionId;
		[SerializeField] private string toPortId;

		public string Id => id;

		public string FromNodeId => fromNodeId;
		public string FromNodeDefinitionId => fromNodeDefinitionId;
		public string FromPortId => fromPortId;

		public string ToNodeId => toNodeId;
		public string ToNodeDefinitionId => toNodeDefinitionId;
		public string ToPortId => toPortId;

		public NodeConnectionData(
			string fromNodeId,
			string fromNodeDefinitionId,
			string fromPortId,
			string toNodeId,
			string toNodeDefinitionId,
			string toPortId)
		{
			this.fromNodeId = Normalize(fromNodeId);
			this.fromNodeDefinitionId = Normalize(fromNodeDefinitionId);
			this.fromPortId = Normalize(fromPortId);

			this.toNodeId = Normalize(toNodeId);
			this.toNodeDefinitionId = Normalize(toNodeDefinitionId);
			this.toPortId = Normalize(toPortId);

			id = BuildId(
				this.fromNodeId,
				this.fromPortId,
				this.toNodeId,
				this.toPortId);
		}

		public bool Matches(
			string sourceNodeId,
			string sourcePortId,
			string targetNodeId,
			string targetPortId)
		{
			return
				string.Equals(fromNodeId, Normalize(sourceNodeId), StringComparison.OrdinalIgnoreCase) &&
				string.Equals(fromPortId, Normalize(sourcePortId), StringComparison.OrdinalIgnoreCase) &&
				string.Equals(toNodeId, Normalize(targetNodeId), StringComparison.OrdinalIgnoreCase) &&
				string.Equals(toPortId, Normalize(targetPortId), StringComparison.OrdinalIgnoreCase);
		}

		public static string BuildId(
			string sourceNodeId,
			string sourcePortId,
			string targetNodeId,
			string targetPortId)
		{
			return $"{Normalize(sourceNodeId)}.{Normalize(sourcePortId)}->{Normalize(targetNodeId)}.{Normalize(targetPortId)}";
		}

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value)
				? string.Empty
				: value.Trim();
		}
	}
}