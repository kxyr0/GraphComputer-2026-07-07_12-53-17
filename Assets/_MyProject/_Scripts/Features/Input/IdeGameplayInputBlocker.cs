using Game.Runtime.Input;
using UnityEngine;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeGameplayInputBlocker : MonoBehaviour
	{
		[Header("Visibility Root")]
		[SerializeField] private CanvasGroup rootGroup;
		[SerializeField] private bool useFirstGroupAsRoot = true;
		[SerializeField] private bool requireRootVisible = true;

		[Header("Groups")]
		[SerializeField] private CanvasGroup[] ideGroups;

		[SerializeField, Range(0f, 1f)]
		private float visibleAlphaThreshold = 0.05f;

		private bool isBlocking;

		private void Reset()
		{
			ideGroups = GetComponentsInChildren<CanvasGroup>(true);
			rootGroup = ideGroups != null && ideGroups.Length > 0
				? ideGroups[0]
				: null;
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
			CanvasGroup resolvedRoot = ResolveRootGroup();

			if (requireRootVisible && resolvedRoot != null)
			{
				return IsVisible(resolvedRoot);
			}

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

				if (IsVisible(group))
				{
					return true;
				}
			}

			return false;
		}

		private CanvasGroup ResolveRootGroup()
		{
			if (rootGroup != null)
			{
				return rootGroup;
			}

			if (!useFirstGroupAsRoot || ideGroups == null || ideGroups.Length == 0)
			{
				return null;
			}

			rootGroup = ideGroups[0];
			return rootGroup;
		}

		private bool IsVisible(CanvasGroup group)
		{
			return group != null && group.alpha > visibleAlphaThreshold;
		}
	}
}
