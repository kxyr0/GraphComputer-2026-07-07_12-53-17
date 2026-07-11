using UnityEngine;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeRuntimeController : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private IdeWindowCanvasGroupController ideWindow;
		[SerializeField] private IdeCodeTabsController codeTabsController;

		public static IdeRuntimeController Instance { get; private set; }

		private void Awake()
		{
			Instance = this;
			ResolveReferences();
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
			ResolveReferences();

			if (ideWindow != null)
			{
				ideWindow.Open();
			}
			else
			{
				Debug.LogError($"[{nameof(IdeRuntimeController)}] IDE window controller is missing.", this);
			}

			if (codeTabsController == null)
			{
				Debug.LogError($"[{nameof(IdeRuntimeController)}] Code tabs controller is missing.", this);
				return;
			}

			codeTabsController.OpenDocument(documentId, title, defaultCode);
		}

		[ContextMenu("Open IDE")]
		public void OpenIdeOnly()
		{
			ResolveReferences();

			if (ideWindow == null)
			{
				Debug.LogError($"[{nameof(IdeRuntimeController)}] IDE window controller is missing.", this);
				return;
			}

			ideWindow.Open();
		}

		private void ResolveReferences()
		{
			if (ideWindow == null)
			{
				ideWindow = FindAnyObjectByType<IdeWindowCanvasGroupController>(
					FindObjectsInactive.Include);
			}

			if (codeTabsController == null)
			{
				codeTabsController = FindAnyObjectByType<IdeCodeTabsController>(
					FindObjectsInactive.Include);
			}
		}
	}
}
