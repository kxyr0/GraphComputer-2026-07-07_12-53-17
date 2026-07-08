using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Input
{
	public static class GameInputBlocker
	{
		private static readonly HashSet<object> blockers = new();

		public static bool IsBlocked => blockers.Count > 0;

		public static void Block(object source)
		{
			if (source == null)
			{
				return;
			}

			blockers.Add(source);
		}

		public static void Unblock(object source)
		{
			if (source == null)
			{
				return;
			}

			blockers.Remove(source);
		}

		public static void Clear()
		{
			blockers.Clear();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			blockers.Clear();
		}
	}
}