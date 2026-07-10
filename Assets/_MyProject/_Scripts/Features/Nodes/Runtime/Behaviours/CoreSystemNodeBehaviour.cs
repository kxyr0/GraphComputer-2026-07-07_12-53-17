using UnityEngine;

namespace Game.Nodes.Runtime
{
	public sealed class CoreSystemNodeBehaviour : NodeRuntimeBehaviour
	{
		[SerializeField, Min(0)] private int maxPower = 100;
		[SerializeField, Min(0)] private int maxCompute = 64;
		[SerializeField, Min(0)] private int maxConnections = 8;

		public int MaxPower => maxPower;
		public int MaxCompute => maxCompute;
		public int MaxConnections => maxConnections;

		protected override void OnInitialized()
		{
			Debug.Log(
				$"System Core initialized. Power: {maxPower}, Compute: {maxCompute}, Connections: {maxConnections}"
			);
		}
	}
}