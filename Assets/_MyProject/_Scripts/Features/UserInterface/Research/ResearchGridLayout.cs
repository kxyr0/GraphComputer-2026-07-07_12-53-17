using System;
using UnityEngine;

namespace Game.UI.Research
{
	[Serializable]
	public sealed class ResearchGridLayout
	{
		[SerializeField] private Vector2 cellSize = new(260f, 150f);
		[SerializeField] private Vector2 centerOffset;

		public Vector2 CellSize => cellSize;
		public Vector2 CenterOffset => centerOffset;

		public Vector2 GetAnchoredPosition(Vector2Int gridPosition)
		{
			return centerOffset + new Vector2(
				gridPosition.x * cellSize.x,
				gridPosition.y * cellSize.y
			);
		}
	}
}
