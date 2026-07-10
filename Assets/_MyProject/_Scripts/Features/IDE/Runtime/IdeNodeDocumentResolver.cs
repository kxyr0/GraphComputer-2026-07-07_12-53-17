using Game.Nodes.Presentation;

namespace Game.IDE.Runtime
{
	public readonly struct IdeNodeDocument
	{
		public readonly string DocumentId;
		public readonly string DefinitionId;
		public readonly string Title;
		public readonly string Code;

		public IdeNodeDocument(
			string documentId,
			string definitionId,
			string title,
			string code)
		{
			DocumentId = documentId;
			DefinitionId = definitionId;
			Title = title;
			Code = code;
		}
	}

	public static class IdeNodeDocumentResolver
	{
		public static IdeNodeDocument Resolve(NodeView nodeView)
		{
			string definitionId = GetNodeDefinitionId(nodeView);
			string documentId = GetDocumentId(nodeView, definitionId);
			string title = GetNodeTitle(nodeView, definitionId);
			string code = GetInitialCode(nodeView, definitionId, title);

			return new IdeNodeDocument(
				documentId,
				definitionId,
				title,
				code
			);
		}

		private static string GetDocumentId(NodeView nodeView, string definitionId)
		{
			if (nodeView == null)
			{
				return "unknown_node";
			}

			if (!string.IsNullOrWhiteSpace(nodeView.InstanceId))
			{
				return nodeView.InstanceId;
			}

			return string.IsNullOrWhiteSpace(definitionId)
				? "unknown_node"
				: definitionId;
		}

		private static string GetNodeDefinitionId(NodeView nodeView)
		{
			if (nodeView == null)
			{
				return "unknown";
			}

			if (nodeView.Definition != null)
			{
				return nodeView.Definition.Id;
			}

			if (nodeView.JsonDefinition != null)
			{
				return nodeView.JsonDefinition.id;
			}

			return "unknown";
		}

		private static string GetNodeTitle(NodeView nodeView, string definitionId)
		{
			if (nodeView == null)
			{
				return "Unknown";
			}

			if (nodeView.Definition != null &&
				!string.IsNullOrWhiteSpace(nodeView.Definition.Title))
			{
				return nodeView.Definition.Title;
			}

			if (nodeView.JsonDefinition != null &&
				!string.IsNullOrWhiteSpace(nodeView.JsonDefinition.title))
			{
				return nodeView.JsonDefinition.title;
			}

			return string.IsNullOrWhiteSpace(definitionId)
				? "Unknown"
				: definitionId;
		}

		private static string GetInitialCode(
			NodeView nodeView,
			string definitionId,
			string title)
		{
			if (nodeView != null &&
				nodeView.JsonDefinition != null &&
				!string.IsNullOrWhiteSpace(nodeView.JsonDefinition.initialCode))
			{
				return nodeView.JsonDefinition.initialCode;
			}

			ModuleOperatingMode defaultMode = nodeView != null && nodeView.JsonDefinition != null
				? ModuleOperatingModeUtility.ParseOrDefault(nodeView.JsonDefinition.mode)
				: ModuleOperatingMode.Balanced;

			return IdeCodeTemplateProvider.GetDefaultCode(
				definitionId,
				title,
				defaultMode);
		}
	}
}
