using UnityEngine;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeRuntimeController : MonoBehaviour
	{
		[SerializeField] private IdeWindowCanvasGroupController ideWindow;
		[SerializeField] private IdeCodeTabsController codeTabsController;

		public static IdeRuntimeController Instance { get; private set; }

		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void OpenNodeDocument(string documentId, string title, string defaultCode)
		{
			if (ideWindow != null)
			{
				ideWindow.Open();
			}

			if (codeTabsController == null)
			{
				Debug.LogError($"[{nameof(IdeRuntimeController)}] Code tabs controller is missing.", this);
				return;
			}

			codeTabsController.OpenDocument(documentId, title, defaultCode);
		}
	}
}