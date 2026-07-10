using Game.Nodes.Runtime;

namespace Game.Nodes.Presentation
{
	internal sealed class NodeRuntimeBehaviourInitializer
	{
		private readonly NodeView nodeView;

		public NodeRuntimeBehaviourInitializer(NodeView nodeView)
		{
			this.nodeView = nodeView;
		}

		public void Initialize()
		{
			if (nodeView == null)
			{
				return;
			}

			NodeRuntimeBehaviour[] behaviours = nodeView.GetComponents<NodeRuntimeBehaviour>();

			foreach (NodeRuntimeBehaviour behaviour in behaviours)
			{
				if (behaviour != null)
				{
					behaviour.Initialize(nodeView);
				}
			}
		}
	}
}
