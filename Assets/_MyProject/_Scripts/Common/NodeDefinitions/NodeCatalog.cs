using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Nodes.Definitions
{
	[CreateAssetMenu(
		fileName = "NodeCatalog",
		menuName = "Graph Computer/Nodes/Node Catalog"
	)]
	public sealed class NodeCatalog : ScriptableObject
	{
		[SerializeField] private NodeDefinition[] definitions;

		private Dictionary<string, NodeDefinition> definitionsById;

		public IReadOnlyList<NodeDefinition> Definitions => definitions;

		private void OnEnable()
		{
			RebuildCache();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			RebuildCache();
		}
#endif

		public bool TryGet(string id, out NodeDefinition definition)
		{
			if (definitionsById == null)
			{
				RebuildCache();
			}

			string key = NormalizeId(id);

			if (string.IsNullOrWhiteSpace(key))
			{
				definition = null;
				return false;
			}

			return definitionsById.TryGetValue(key, out definition);
		}

		public NodeDefinition GetRequired(string id)
		{
			if (TryGet(id, out NodeDefinition definition))
			{
				return definition;
			}

			throw new InvalidOperationException(
				$"Node definition '{id}' was not found in catalog.\n" +
				$"Available ids:\n{BuildAvailableIdsText()}"
			);
		}

		[ContextMenu("Rebuild Cache")]
		public void RebuildCache()
		{
			definitionsById = new Dictionary<string, NodeDefinition>(
				StringComparer.OrdinalIgnoreCase
			);

			if (definitions == null)
			{
				return;
			}

			foreach (NodeDefinition definition in definitions)
			{
				if (definition == null)
				{
					continue;
				}

				string id = NormalizeId(definition.Id);

				if (string.IsNullOrWhiteSpace(id))
				{
					Debug.LogError(
						$"[{nameof(NodeCatalog)}] Node definition '{definition.name}' has empty id.",
						this
					);
					continue;
				}

				if (definitionsById.ContainsKey(id))
				{
					Debug.LogError(
						$"[{nameof(NodeCatalog)}] Duplicate node id '{id}'. Definition: '{definition.name}'.",
						this
					);
					continue;
				}

				definitionsById.Add(id, definition);
			}
		}

		[ContextMenu("Print Registered Node Ids")]
		private void PrintRegisteredNodeIds()
		{
			RebuildCache();
			Debug.Log(BuildAvailableIdsText(), this);
		}

		private string BuildAvailableIdsText()
		{
			if (definitionsById == null)
			{
				RebuildCache();
			}

			if (definitionsById.Count == 0)
			{
				return "- empty catalog";
			}

			StringBuilder builder = new StringBuilder();

			foreach (KeyValuePair<string, NodeDefinition> pair in definitionsById)
			{
				builder.Append("- ");
				builder.Append(pair.Key);
				builder.Append(" -> ");
				builder.Append(pair.Value != null ? pair.Value.name : "null");
				builder.AppendLine();
			}

			return builder.ToString();
		}

		private static string NormalizeId(string id)
		{
			return string.IsNullOrWhiteSpace(id)
				? string.Empty
				: id.Trim();
		}
	}
}