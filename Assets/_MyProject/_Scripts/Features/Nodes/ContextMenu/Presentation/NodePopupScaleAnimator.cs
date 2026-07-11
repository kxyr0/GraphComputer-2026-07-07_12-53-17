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

			TweenScaleIfNeeded(
				Vector3.one * visibleScale,
				showDuration,
				showEase);

			TweenAlphaIfNeeded(1f, showDuration, Ease.OutCubic);
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

			Tween scaleTween = TweenScaleIfNeeded(
				Vector3.one * hiddenScale,
				hideDuration,
				hideEase);

			if (scaleTween.isAlive)
			{
				scaleTween.OnComplete(CompleteHide);
			}
			else
			{
				CompleteHide();
			}

			TweenAlphaIfNeeded(0f, hideDuration, Ease.InCubic);
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

		private Tween TweenScaleIfNeeded(Vector3 targetScale, float duration, Ease ease)
		{
			if ((target.localScale - targetScale).sqrMagnitude <= 0.000001f)
			{
				target.localScale = targetScale;
				return default;
			}

			return Tween.Scale(
				target,
				endValue: targetScale,
				duration: duration,
				ease: ease
			);
		}

		private void TweenAlphaIfNeeded(float targetAlpha, float duration, Ease ease)
		{
			if (Mathf.Approximately(canvasGroup.alpha, targetAlpha))
			{
				canvasGroup.alpha = targetAlpha;
				return;
			}

			Tween.Alpha(
				canvasGroup,
				endValue: targetAlpha,
				duration: duration,
				ease: ease
			);
		}
	}
}
