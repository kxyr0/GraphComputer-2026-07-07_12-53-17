using Game.Nodes.Definitions;

namespace Game.Nodes.Presentation
{
	internal sealed class NodeLegacyPortBinder
	{
		private readonly NodePortView[] portViews;

		public NodeLegacyPortBinder(NodePortView[] portViews)
		{
			this.portViews = portViews;
		}

		public void BindLegacyPorts(NodeDefinition definition)
		{
			if (portViews == null || definition == null || definition.Ports == null)
			{
				return;
			}

			for (int i = 0; i < portViews.Length; i++)
			{
				if (portViews[i] == null)
				{
					continue;
				}

				if (i < definition.Ports.Count)
				{
					portViews[i].Bind(definition.Ports[i]);
				}
				else
				{
					portViews[i].Hide();
				}
			}
		}

		public void HideLegacyPorts()
		{
			if (portViews == null)
			{
				return;
			}

			foreach (NodePortView portView in portViews)
			{
				if (portView != null)
				{
					portView.Hide();
				}
			}
		}
	}
}
