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

		private bool isVisible;

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

			isVisible = visibleOnStart;
			ApplyInstant(isVisible);
		}

		private void Update()
		{
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
			SetVisible(!isVisible);
		}

		public void Show()
		{
			SetVisible(true);
		}

		public void Hide()
		{
			SetVisible(false);
		}

		private void SetVisible(bool value)
		{
			if (targetGroup == null)
			{
				return;
			}

			isVisible = value;

			Tween.StopAll(onTarget: targetGroup);

			float targetAlpha = value ? visibleAlpha : hiddenAlpha;
			Ease ease = value ? showEase : hideEase;

			if (value)
			{
				targetGroup.interactable = true;
				targetGroup.blocksRaycasts = true;
			}
			else
			{
				targetGroup.interactable = false;
				targetGroup.blocksRaycasts = false;
			}

			Tween.Alpha(
				targetGroup,
				endValue: targetAlpha,
				duration: duration,
				ease: ease
			);
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