using Game.Runtime.Input;
using UnityEngine;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeGameplayInputBlocker : MonoBehaviour
	{
		[SerializeField] private CanvasGroup[] ideGroups;

		[SerializeField, Range(0f, 1f)]
		private float visibleAlphaThreshold = 0.05f;

		private bool isBlocking;

		private void Reset()
		{
			ideGroups = GetComponentsInChildren<CanvasGroup>(true);
		}

		private void LateUpdate()
		{
			bool shouldBlock = IsIdeVisible();

			if (shouldBlock == isBlocking)
			{
				return;
			}

			isBlocking = shouldBlock;

			if (isBlocking)
			{
				GameInputBlocker.Block(this);
			}
			else
			{
				GameInputBlocker.Unblock(this);
			}
		}

		private void OnDisable()
		{
			if (isBlocking)
			{
				GameInputBlocker.Unblock(this);
				isBlocking = false;
			}
		}

		private void OnDestroy()
		{
			if (isBlocking)
			{
				GameInputBlocker.Unblock(this);
				isBlocking = false;
			}
		}

		private bool IsIdeVisible()
		{
			if (ideGroups == null)
			{
				return false;
			}

			foreach (CanvasGroup group in ideGroups)
			{
				if (group == null)
				{
					continue;
				}

				if (group.alpha > visibleAlphaThreshold)
				{
					return true;
				}
			}

			return false;
		}
	}
}