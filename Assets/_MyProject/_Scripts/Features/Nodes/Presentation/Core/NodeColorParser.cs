using UnityEngine;

namespace Game.Nodes.Presentation
{
	internal static class NodeColorParser
	{
		public static Color ParseColor(string value, Color fallback)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return fallback;
			}

			return ColorUtility.TryParseHtmlString(value, out Color color)
				? color
				: fallback;
		}
	}
}
