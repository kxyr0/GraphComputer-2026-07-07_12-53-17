using Game.Nodes.Presentation;
using UnityEngine;

namespace Game.Nodes.Runtime
{
	public abstract class NodeRuntimeBehaviour : MonoBehaviour
	{
		protected NodeView View { get; private set; }

		public void Initialize(NodeView view)
		{
			View = view;
			OnInitialized();
		}

		protected virtual void OnInitialized()
		{
		}

		public virtual void Tick(float deltaTime)
		{
		}
	}
}