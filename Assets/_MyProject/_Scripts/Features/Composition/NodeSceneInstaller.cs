using Game.Nodes.Composition;
using Game.Nodes.Presentation;
using Game.Nodes.Runtime;
using Game.Selection.Categories.Runtime;
using Game.Selection.Runtime;
using VContainer;
using VContainer.Unity;

namespace Game.Composition
{
	public static class NodeSceneInstaller
	{
		public static void Register(IContainerBuilder builder)
		{
			builder.RegisterComponentInHierarchy<NodeFactory>();
			builder.RegisterComponentInHierarchy<NodeGraphConnectionModel>();
			builder.RegisterComponentInHierarchy<NodeConnectionDragController>();
			builder.RegisterComponentInHierarchy<NodeGraphContextMenuController>();
			builder.RegisterComponentInHierarchy<NodeSelectionService>();
			builder.RegisterComponentInHierarchy<NodeDeletionService>();
			builder.RegisterComponentInHierarchy<NodeCategoryFrameService>();
		}
	}
}
