using System;
using Game.Grid.Domain;

namespace Game.Grid.Presentation
{
	public sealed class GridPresenter : IDisposable
	{
		private readonly GridData gridData;
		private readonly CanvasGridView gridView;

		public GridPresenter(GridData gridData, CanvasGridView gridView)
		{
			this.gridData = gridData ?? throw new ArgumentNullException(nameof(gridData));
			this.gridView = gridView ?? throw new ArgumentNullException(nameof(gridView));
		}

		public void Initialize()
		{
			gridView.SetGridSize(gridData.Width, gridData.Height);
		}

		public void Dispose()
		{
		}
	}
}