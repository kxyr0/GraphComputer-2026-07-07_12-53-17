using System;
using UnityEngine;

namespace Game.Nodes.Definitions
{
	[CreateAssetMenu(
		fileName = "NodeJsonDefinition",
		menuName = "Game/Nodes/Node Json Definition")]
	public sealed class NodeJsonDefinitionAsset : ScriptableObject
	{
		[SerializeField, TextArea(12, 40)]
		private string json;

		public string Json => json;

		public bool TryRead(out NodeJsonDefinition definition, out string error)
		{
			definition = null;
			error = string.Empty;

			if (string.IsNullOrWhiteSpace(json))
			{
				error = "Node JSON is empty.";
				return false;
			}

			try
			{
				definition = JsonUtility.FromJson<NodeJsonDefinition>(json);

				if (definition == null)
				{
					error = "Failed to parse node JSON.";
					return false;
				}

				if (string.IsNullOrWhiteSpace(definition.id))
				{
					error = "Node id is empty.";
					return false;
				}

				if (string.IsNullOrWhiteSpace(definition.title))
				{
					error = "Node title is empty.";
					return false;
				}

				definition.Normalize();
				return true;
			}
			catch (Exception exception)
			{
				error = exception.Message;
				return false;
			}
		}
	}

	[Serializable]
	public sealed class NodeJsonDefinition
	{
		public string id;
		public string title;

		public Vector2 size = new Vector2(560f, 460f);

		public bool canMove = true;

		public string backgroundColor = "#102333DD";
		public string headerColor = "#050813EE";
		public string accentColor = "#19FFF5";

		public string initialCode;
		public string mode = "balanced";

		public NodeJsonStat[] stats;
		public NodeJsonPort[] ports;

		public void Normalize()
		{
			initialCode ??= string.Empty;

			if (string.IsNullOrWhiteSpace(mode))
			{
				mode = "balanced";
			}

			stats ??= Array.Empty<NodeJsonStat>();
			ports ??= Array.Empty<NodeJsonPort>();

			foreach (NodeJsonPort port in ports)
			{
				port?.Normalize();
			}
		}
	}

	[Serializable]
	public sealed class NodeJsonStat
	{
		public string label;
		public string value;
	}

	[Serializable]
	public sealed class NodeJsonPort
	{
		public string id;
		public string label;

		// Input / Output
		public string direction = "Output";

		public string color = "#19FFF5";

		// Any / System / Data / Security / Storage / Control / Compute / Network / Sound / Power
		public string signalType = "Any";

		// Input rules
		public string[] acceptedSignalTypes;
		public string[] allowedSourceNodeIds;
		public string[] allowedSourcePortIds;

		// Output rules
		public string[] allowedTargetNodeIds;
		public string[] allowedTargetPortIds;

		public void Normalize()
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				id = label;
			}

			if (string.IsNullOrWhiteSpace(label))
			{
				label = id;
			}

			if (string.IsNullOrWhiteSpace(direction))
			{
				direction = "Output";
			}

			if (string.IsNullOrWhiteSpace(color))
			{
				color = "#19FFF5";
			}

			if (string.IsNullOrWhiteSpace(signalType))
			{
				signalType = "Any";
			}

			acceptedSignalTypes ??= Array.Empty<string>();
			allowedSourceNodeIds ??= Array.Empty<string>();
			allowedSourcePortIds ??= Array.Empty<string>();
			allowedTargetNodeIds ??= Array.Empty<string>();
			allowedTargetPortIds ??= Array.Empty<string>();
		}
	}
}
