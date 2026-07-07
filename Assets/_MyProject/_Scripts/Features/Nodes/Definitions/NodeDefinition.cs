using System;
using System.Collections.Generic;
using Game.Nodes.Domain;
using UnityEngine;

namespace Game.Nodes.Definitions
{
	[CreateAssetMenu(
		fileName = "NodeDefinition",
		menuName = "Graph Computer/Nodes/Node Definition"
	)]
	public sealed class NodeDefinition : ScriptableObject
	{
		[SerializeField] private string id = "node.id";
		[SerializeField] private NodeKind kind;
		[SerializeField] private string title = "Node";
		[SerializeField] private GameObject prefab;

		[Header("Json Visual")]
		[SerializeField] private NodeJsonDefinitionAsset jsonDefinition;

		[Header("Rules")]
		[SerializeField] private bool isUnique;
		[SerializeField] private bool canMove = true;
		[SerializeField] private bool canDelete = true;

		[Header("Legacy Visual")]
		[SerializeField] private Vector2 size = new(220f, 140f);
		[SerializeField] private Color backgroundColor = new(0.03f, 0.04f, 0.07f, 0.92f);
		[SerializeField] private Color headerColor = new(0.02f, 0.02f, 0.05f, 1f);
		[SerializeField] private Color accentColor = Color.cyan;

		[Header("Legacy Ports")]
		[SerializeField] private NodePortDefinition[] ports;

		public string Id => id;
		public NodeKind Kind => kind;
		public string Title => title;
		public GameObject Prefab => prefab;
		public NodeJsonDefinitionAsset JsonDefinition => jsonDefinition;

		public bool IsUnique => isUnique;
		public bool CanMove => canMove;
		public bool CanDelete => canDelete;

		public Vector2 Size => size;
		public Color BackgroundColor => backgroundColor;
		public Color HeaderColor => headerColor;
		public Color AccentColor => accentColor;

		public IReadOnlyList<NodePortDefinition> Ports => ports;

		public bool HasJsonDefinition => jsonDefinition != null;

		public bool TryReadJsonDefinition(out NodeJsonDefinition definition, out string error)
		{
			definition = null;
			error = string.Empty;

			if (jsonDefinition == null)
			{
				error = $"Node definition '{id}' has no JSON definition.";
				return false;
			}

			if (!jsonDefinition.TryRead(out definition, out error))
			{
				return false;
			}

			if (!string.Equals(definition.id, id, StringComparison.OrdinalIgnoreCase))
			{
				error = $"NodeDefinition id '{id}' does not match JSON id '{definition.id}'.";
				return false;
			}

			return true;
		}
	}
}