using System;
using PrimeTween;
using UnityEngine;

namespace Game.Nodes.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(CanvasGroup))]
	public sealed class NodePopupScaleAnimator : MonoBehaviour
	{
		[SerializeField] private RectTransform target;
		[SerializeField] private CanvasGroup canvasGroup;

		[Header("Scale")]
		[SerializeField, Min(0f)] private float hiddenScale = 0.82f;
		[SerializeField, Min(0f)] private float visibleScale = 1f;

		[Header("Timing")]
		[SerializeField, Min(0.01f)] private float showDuration = 0.14f;
		[SerializeField, Min(0.01f)] private float hideDuration = 0.08f;

		[Header("Ease")]
		[SerializeField] private Ease showEase = Ease.OutBack;
		[SerializeField] private Ease hideEase = Ease.InCubic;

		private Action hideCompleted;

		public bool IsVisible { get; private set; }

		private void Reset()
		{
			target = transform as RectTransform;
			canvasGroup = GetComponent<CanvasGroup>();
		}

		private void Awake()
		{
			if (target == null)
			{
				target = transform as RectTransform;
			}

			if (canvasGroup == null)
			{
				canvasGroup = GetComponent<CanvasGroup>();
			}
		}

		public void Show()
		{
			if (target == null || canvasGroup == null)
			{
				return;
			}

			hideCompleted = null;
			IsVisible = true;

			Tween.StopAll(onTarget: target);
			Tween.StopAll(onTarget: canvasGroup);

			target.localScale = Vector3.one * hiddenScale;

			canvasGroup.alpha = 0f;
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;

			Tween.Scale(
				target,
				endValue: Vector3.one * visibleScale,
				duration: showDuration,
				ease: showEase
			);

			Tween.Alpha(
				canvasGroup,
				endValue: 1f,
				duration: showDuration,
				ease: Ease.OutCubic
			);
		}

		public void Hide(Action onCompleted = null)
		{
			if (target == null || canvasGroup == null)
			{
				onCompleted?.Invoke();
				return;
			}

			if (!IsVisible)
			{
				onCompleted?.Invoke();
				return;
			}

			hideCompleted = onCompleted;
			IsVisible = false;

			Tween.StopAll(onTarget: target);
			Tween.StopAll(onTarget: canvasGroup);

			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;

			Tween.Scale(
					target,
					endValue: Vector3.one * hiddenScale,
					duration: hideDuration,
					ease: hideEase
				)
				.OnComplete(CompleteHide);

			Tween.Alpha(
				canvasGroup,
				endValue: 0f,
				duration: hideDuration,
				ease: Ease.InCubic
			);
		}

		public void HideInstant()
		{
			if (target == null || canvasGroup == null)
			{
				return;
			}

			hideCompleted = null;
			IsVisible = false;

			Tween.StopAll(onTarget: target);
			Tween.StopAll(onTarget: canvasGroup);

			target.localScale = Vector3.one * hiddenScale;

			canvasGroup.alpha = 0f;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
		}

		private void CompleteHide()
		{
			if (target != null)
			{
				target.localScale = Vector3.one * hiddenScale;
			}

			if (canvasGroup != null)
			{
				canvasGroup.alpha = 0f;
				canvasGroup.interactable = false;
				canvasGroup.blocksRaycasts = false;
			}

			Action completed = hideCompleted;
			hideCompleted = null;
			completed?.Invoke();
		}
	}
}