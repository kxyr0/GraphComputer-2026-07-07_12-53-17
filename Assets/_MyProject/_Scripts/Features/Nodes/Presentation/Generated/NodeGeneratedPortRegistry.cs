using System;
using System.Collections.Generic;

namespace Game.Nodes.Presentation
{
	internal sealed class NodeGeneratedPortRegistry
	{
		private readonly Dictionary<string, NodeStatRowView> statsByLabel =
			new Dictionary<string, NodeStatRowView>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, GeneratedNodePortView> portsById =
			new Dictionary<string, GeneratedNodePortView>(StringComparer.OrdinalIgnoreCase);

		private readonly List<GeneratedNodePortView> generatedPorts = new();

		public IReadOnlyList<GeneratedNodePortView> GeneratedPorts => generatedPorts;

		public void RegisterGeneratedPort(GeneratedNodePortView portView)
		{
			if (portView == null || string.IsNullOrWhiteSpace(portView.PortId))
			{
				return;
			}

			string portId = portView.PortId.Trim();

			generatedPorts.Add(portView);
			portsById[portId] = portView;
		}

		public void RegisterGeneratedStat(NodeStatRowView row)
		{
			if (row == null || string.IsNullOrWhiteSpace(row.Label))
			{
				return;
			}

			statsByLabel[row.Label.Trim()] = row;
		}

		public bool TryGetPort(string portId, out GeneratedNodePortView portView)
		{
			if (string.IsNullOrWhiteSpace(portId))
			{
				portView = null;
				return false;
			}

			return portsById.TryGetValue(portId.Trim(), out portView);
		}

		public bool TryGetFirstInputPort(out GeneratedNodePortView portView)
		{
			foreach (GeneratedNodePortView generatedPort in generatedPorts)
			{
				if (generatedPort != null && generatedPort.IsInput)
				{
					portView = generatedPort;
					return true;
				}
			}

			portView = null;
			return false;
		}

		public bool TryGetFirstOutputPort(out GeneratedNodePortView portView)
		{
			foreach (GeneratedNodePortView generatedPort in generatedPorts)
			{
				if (generatedPort != null && generatedPort.IsOutput)
				{
					portView = generatedPort;
					return true;
				}
			}

			portView = null;
			return false;
		}

		public bool TrySetStatValue(string label, string value)
		{
			if (string.IsNullOrWhiteSpace(label))
			{
				return false;
			}

			if (!statsByLabel.TryGetValue(label.Trim(), out NodeStatRowView row))
			{
				return false;
			}

			row.SetValue(value);
			return true;
		}

		public bool TrySetStatValue(string label, string value, string stateHint)
		{
			if (string.IsNullOrWhiteSpace(label))
			{
				return false;
			}

			if (!statsByLabel.TryGetValue(label.Trim(), out NodeStatRowView row))
			{
				return false;
			}

			row.SetValue(value, stateHint);
			return true;
		}

		public void Clear()
		{
			portsById.Clear();
			generatedPorts.Clear();
			statsByLabel.Clear();
		}
	}
}
