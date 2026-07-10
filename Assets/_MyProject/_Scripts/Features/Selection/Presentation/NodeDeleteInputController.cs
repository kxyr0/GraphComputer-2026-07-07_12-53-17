using Game.Runtime.Input;
using Game.Selection.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Selection.Presentation
{
	[DisallowMultipleComponent]
	public sealed class NodeDeleteInputController : MonoBehaviour
	{
		[SerializeField] private NodeDeletionService deletionService;
		[SerializeField] private bool deleteWithDeleteKey = true;
		[SerializeField] private bool deleteWithBackspaceKey;
		[SerializeField] private bool blockWhenGameplayInputBlocked = true;

		private void Awake()
		{
			if (deletionService == null)
			{
				deletionService = FindFirstObjectByType<NodeDeletionService>();
			}
		}

		private void Update()
		{
			if (blockWhenGameplayInputBlocked && GameInputBlocker.IsBlocked)
			{
				return;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard == null)
			{
				return;
			}

			bool deletePressed =
				deleteWithDeleteKey && keyboard.deleteKey.wasPressedThisFrame;

			bool backspacePressed =
				deleteWithBackspaceKey && keyboard.backspaceKey.wasPressedThisFrame;

			if (!deletePressed && !backspacePressed)
			{
				return;
			}

			if (deletionService == null)
			{
				Debug.LogWarning(
					$"[{nameof(NodeDeleteInputController)}] Deletion service is missing.",
					this);

				return;
			}

			deletionService.DeleteSelected();
		}
	}
}
