using Game.Grid.Domain;
using Game.Grid.Presentation;
using UnityEngine;

namespace Game.Grid.Composition
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(CanvasGridView))]
	public sealed class GridBootstrapper : MonoBehaviour
	{
		[SerializeField] private CanvasGridView gridView;

		[Header("Grid")]
		[SerializeField, Min(1)] private int width = 12;
		[SerializeField, Min(1)] private int height = 8;

		private GridPresenter presenter;

		public GridData GridData { get; private set; }

		private void Awake()
		{
			if (gridView == null)
			{
				gridView = GetComponent<CanvasGridView>();
			}

			GridData = new GridData(width, height);

			presenter = new GridPresenter(GridData, gridView);
			presenter.Initialize();
		}

		private void OnDestroy()
		{
			presenter?.Dispose();
		}

#if UNITY_EDITOR
        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            if (gridView == null)
            {
                gridView = GetComponent<CanvasGridView>();
            }

            if (gridView != null)
            {
                gridView.SetGridSize(width, height);
            }
        }
#endif
	}
}