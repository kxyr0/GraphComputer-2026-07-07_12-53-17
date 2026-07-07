using System;

namespace Game.Grid.Domain
{
	public sealed class GridData
	{
		private readonly GridCell[] cells;

		public int Width { get; }
		public int Height { get; }

		public event Action<GridPosition, GridCell> CellChanged;

		public GridData(int width, int height)
		{
			ValidateSize(width, height);

			Width = width;
			Height = height;

			cells = new GridCell[Width * Height];

			FillCells();
		}

		public bool Contains(GridPosition position)
		{
			return position.X >= 0 &&
				   position.Y >= 0 &&
				   position.X < Width &&
				   position.Y < Height;
		}

		public GridCell GetCell(GridPosition position)
		{
			int index = GetIndexChecked(position);
			return cells[index];
		}

		public void SetWalkable(GridPosition position, bool isWalkable)
		{
			int index = GetIndexChecked(position);

			GridCell currentCell = cells[index];

			if (currentCell.IsWalkable == isWalkable)
			{
				return;
			}

			GridCell updatedCell = currentCell.WithWalkable(isWalkable);

			cells[index] = updatedCell;
			CellChanged?.Invoke(position, updatedCell);
		}

		private void FillCells()
		{
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					GridPosition position = new GridPosition(x, y);
					cells[GetIndex(position)] = new GridCell(position);
				}
			}
		}

		private int GetIndexChecked(GridPosition position)
		{
			if (!Contains(position))
			{
				throw new ArgumentOutOfRangeException(
					nameof(position),
					$"Grid position {position} is outside grid bounds {Width}x{Height}."
				);
			}

			return GetIndex(position);
		}

		private int GetIndex(GridPosition position)
		{
			return position.Y * Width + position.X;
		}

		private static void ValidateSize(int width, int height)
		{
			if (width <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(width), "Grid width must be greater than zero.");
			}

			if (height <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(height), "Grid height must be greater than zero.");
			}
		}
	}
}