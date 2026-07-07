namespace Game.Grid.Domain
{
	public readonly struct GridCell
	{
		public GridPosition Position { get; }
		public bool IsWalkable { get; }

		public GridCell(GridPosition position, bool isWalkable = true)
		{
			Position = position;
			IsWalkable = isWalkable;
		}

		public GridCell WithWalkable(bool isWalkable)
		{
			return new GridCell(Position, isWalkable);
		}
	}
}