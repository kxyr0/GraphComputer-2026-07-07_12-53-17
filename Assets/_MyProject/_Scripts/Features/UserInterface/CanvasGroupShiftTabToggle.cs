using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
	[DisallowMultipleComponent]
	public sealed class CanvasGroupShiftTabToggle : MonoBehaviour
	{
		[Header("Target")]
		[SerializeField] private CanvasGroup targetGroup;

		[Header("State")]
		[SerializeField] private bool visibleOnStart;
		[SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
		[SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;

		[Header("Animation")]
		[SerializeField, Min(0.01f)] private float duration = 0.16f;
		[SerializeField] private Ease showEase = Ease.OutCubic;
		[SerializeField] private Ease hideEase = Ease.InCubic;

		[Header("Input")]
		[SerializeField] private bool useShiftTab = true;

		[Header("Input Guard")]
		[SerializeField] private bool requireVisibleParentGroups = true;
		[SerializeField, Range(0f, 1f)] private float parentVisibleAlphaThreshold = 0.05f;

		private void Reset()
		{
			targetGroup = GetComponent<CanvasGroup>();
		}

		private void Awake()
		{
			if (targetGroup == null)
			{
				targetGroup = GetComponent<CanvasGroup>();
			}

			ApplyInstant(visibleOnStart);
		}

		private void OnDestroy()
		{
			if (targetGroup != null)
			{
				Tween.StopAll(onTarget: targetGroup);
			}
		}

		private void Update()
		{
			if (!useShiftTab)
			{
				return;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard == null)
			{
				return;
			}

			bool shiftPressed =
				keyboard.leftShiftKey.isPressed ||
				keyboard.rightShiftKey.isPressed;

			if (!shiftPressed || !keyboard.tabKey.wasPressedThisFrame)
			{
				return;
			}

			Toggle();
		}

		public void Toggle()
		{
			SetVisible(!IsCurrentlyVisible());
		}

		public void Show()
		{
			SetVisible(true);
		}

		public void Hide()
		{
			SetVisible(false);
		}

		public void SetVisible(bool visible)
		{
			if (targetGroup == null)
			{
				return;
			}

			if (visible && requireVisibleParentGroups && HasHiddenParentCanvasGroup())
			{
				return;
			}

			Tween.StopAll(onTarget: targetGroup);

			float targetAlpha = visible ? visibleAlpha : hiddenAlpha;

			targetGroup.interactable = visible;
			targetGroup.blocksRaycasts = visible;

			if (Mathf.Approximately(targetGroup.alpha, targetAlpha))
			{
				targetGroup.alpha = targetAlpha;
				return;
			}

			Tween.Alpha(
				targetGroup,
				endValue: targetAlpha,
				duration: duration,
				ease: visible ? showEase : hideEase
			);
		}

		private bool IsCurrentlyVisible()
		{
			if (targetGroup == null)
			{
				return false;
			}

			float middle = (visibleAlpha + hiddenAlpha) * 0.5f;
			return targetGroup.alpha > middle;
		}

		private bool HasHiddenParentCanvasGroup()
		{
			Transform current = targetGroup.transform.parent;

			while (current != null)
			{
				CanvasGroup parentGroup = current.GetComponent<CanvasGroup>();

				if (parentGroup != null &&
					parentGroup.alpha <= parentVisibleAlphaThreshold)
				{
					return true;
				}

				current = current.parent;
			}

			return false;
		}

		private void ApplyInstant(bool visible)
		{
			if (targetGroup == null)
			{
				return;
			}

			Tween.StopAll(onTarget: targetGroup);

			targetGroup.alpha = visible ? visibleAlpha : hiddenAlpha;
			targetGroup.interactable = visible;
			targetGroup.blocksRaycasts = visible;
		}
	}
}
