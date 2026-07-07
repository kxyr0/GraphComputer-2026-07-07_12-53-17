using System;
using Game.Nodes.Domain;
using UnityEngine;

namespace Game.Nodes.Definitions
{
	[Serializable]
	public sealed class NodePortDefinition
	{
		[SerializeField] private string id = "port";
		[SerializeField] private string displayName = "Port";
		[SerializeField] private NodePortDirection direction;
		[SerializeField] private NodeSignalType signalType;
		[SerializeField] private bool allowMultipleConnections = true;
		[SerializeField] private Color color = Color.cyan;

		public string Id => id;
		public string DisplayName => displayName;
		public NodePortDirection Direction => direction;
		public NodeSignalType SignalType => signalType;
		public bool AllowMultipleConnections => allowMultipleConnections;
		public Color Color => color;
	}
}