using Game.Nodes.Definitions;

namespace Game.Nodes.Presentation
{
	public static class NodePortCompatibilityUtility
	{
		private const string Any = "Any";
		private const string Wildcard = "*";

		public static bool CanConnect(
			GeneratedNodePortView source,
			GeneratedNodePortView target)
		{
			if (source == null || target == null)
			{
				return false;
			}

			if (!source.IsOutput || !target.IsInput)
			{
				return false;
			}

			if (source.Owner == target.Owner)
			{
				return false;
			}

			return CanConnectToInput(
				source,
				GetNodeId(target.Owner),
				target.PortId,
				target.PortDefinition
			);
		}

		public static bool CanConnectToNode(
			GeneratedNodePortView source,
			NodeDefinition targetNodeDefinition)
		{
			if (source == null || targetNodeDefinition == null)
			{
				return false;
			}

			if (!targetNodeDefinition.HasJsonDefinition)
			{
				return false;
			}

			if (!targetNodeDefinition.TryReadJsonDefinition(
					out NodeJsonDefinition jsonDefinition,
					out _))
			{
				return false;
			}

			if (jsonDefinition.ports == null)
			{
				return false;
			}

			foreach (NodeJsonPort port in jsonDefinition.ports)
			{
				if (!IsInput(port))
				{
					continue;
				}

				string targetPortId = string.IsNullOrWhiteSpace(port.id)
					? port.label
					: port.id;

				if (CanConnectToInput(
						source,
						targetNodeDefinition.Id,
						targetPortId,
						port))
				{
					return true;
				}
			}

			return false;
		}

		public static bool TryFindCompatibleInput(
			NodeView targetNode,
			GeneratedNodePortView source,
			out GeneratedNodePortView inputPort)
		{
			inputPort = null;

			if (targetNode == null || source == null)
			{
				return false;
			}

			foreach (GeneratedNodePortView port in targetNode.GeneratedPorts)
			{
				if (port == null || !port.IsInput)
				{
					continue;
				}

				if (CanConnect(source, port))
				{
					inputPort = port;
					return true;
				}
			}

			return false;
		}

		private static bool CanConnectToInput(
			GeneratedNodePortView source,
			string targetNodeId,
			string targetPortId,
			NodeJsonPort targetInputPort)
		{
			if (source == null || targetInputPort == null)
			{
				return false;
			}

			NodeJsonPort sourcePort = source.PortDefinition;

			if (sourcePort == null)
			{
				return false;
			}

			string sourceNodeId = GetNodeId(source.Owner);
			string sourcePortId = source.PortId;
			string signalType = Normalize(sourcePort.signalType);

			if (!Matches(sourcePort.allowedTargetNodeIds, targetNodeId))
			{
				return false;
			}

			if (!Matches(sourcePort.allowedTargetPortIds, targetPortId))
			{
				return false;
			}

			if (!Matches(targetInputPort.allowedSourceNodeIds, sourceNodeId))
			{
				return false;
			}

			if (!Matches(targetInputPort.allowedSourcePortIds, sourcePortId))
			{
				return false;
			}

			if (!MatchesSignal(targetInputPort.acceptedSignalTypes, signalType))
			{
				return false;
			}

			return true;
		}

		private static bool IsInput(NodeJsonPort port)
		{
			return port != null &&
				   string.Equals(
					   port.direction,
					   "Input",
					   System.StringComparison.OrdinalIgnoreCase);
		}

		private static bool MatchesSignal(string[] acceptedSignals, string signal)
		{
			if (acceptedSignals == null || acceptedSignals.Length == 0)
			{
				return true;
			}

			return Matches(acceptedSignals, signal);
		}

		private static bool Matches(string[] allowedValues, string value)
		{
			if (allowedValues == null || allowedValues.Length == 0)
			{
				return true;
			}

			string normalizedValue = Normalize(value);

			foreach (string allowedValue in allowedValues)
			{
				string normalizedAllowedValue = Normalize(allowedValue);

				if (normalizedAllowedValue == Wildcard || normalizedAllowedValue == Any)
				{
					return true;
				}

				if (string.Equals(
						normalizedAllowedValue,
						normalizedValue,
						System.StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value)
				? Any
				: value.Trim();
		}

		private static string GetNodeId(NodeView nodeView)
		{
			if (nodeView == null)
			{
				return string.Empty;
			}

			if (nodeView.Definition != null)
			{
				return nodeView.Definition.Id;
			}

			if (nodeView.JsonDefinition != null)
			{
				return nodeView.JsonDefinition.id;
			}

			return nodeView.InstanceId;
		}
	}
}