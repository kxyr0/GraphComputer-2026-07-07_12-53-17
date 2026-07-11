using System;
using PrimeTween;
using UnityEngine;

namespace Game.UI.Notifications
{
	[Serializable]
	public sealed class NotificationAnimator
	{
		[SerializeField] private CanvasGroup notificationGroup;
		[SerializeField] private RectTransform contentRoot;
		[SerializeField] private bool deactivateWhenHidden = true;

		[Header("Fade")]
		[SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;
		[SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;

		[Header("Scale")]
		[SerializeField, Min(0f)] private float hiddenScale = 0.86f;
		[SerializeField, Min(0f)] private float visibleScale = 1f;

		[Header("Fade Timing")]
		[SerializeField, Min(0.01f)] private float fadeShowDuration = 0.16f;
		[SerializeField, Min(0.01f)] private float fadeHideDuration = 0.11f;
		[SerializeField] private Ease fadeShowEase = Ease.OutCubic;
		[SerializeField] private Ease fadeHideEase = Ease.InCubic;

		[Header("Scale Timing")]
		[SerializeField, Min(0.01f)] private float scaleShowDuration = 0.16f;
		[SerializeField, Min(0.01f)] private float scaleHideDuration = 0.11f;
		[SerializeField] private Ease scaleShowEase = Ease.OutBack;
		[SerializeField] private Ease scaleHideEase = Ease.InCubic;

		private Action hideCompleted;
		private Tween hideCompletionTween;

		public bool IsVisible { get; private set; }

		public void UseFallbackTargets(CanvasGroup group, RectTransform content)
		{
			if (notificationGroup == null)
			{
				notificationGroup = group;
			}

			if (contentRoot == null)
			{
				contentRoot = content;
			}
		}

		public void Show()
		{
			if (notificationGroup == null)
			{
				return;
			}

			hideCompleted = null;
			IsVisible = true;

			StopTweens();
			notificationGroup.gameObject.SetActive(true);
			notificationGroup.alpha = hiddenAlpha;
			notificationGroup.interactable = true;
			notificationGroup.blocksRaycasts = true;

			if (contentRoot != null)
			{
				contentRoot.localScale = Vector3.one * hiddenScale;
				Vector3 targetScale = Vector3.one * visibleScale;

				if ((contentRoot.localScale - targetScale).sqrMagnitude <= 0.000001f)
				{
					contentRoot.localScale = targetScale;
				}
				else
				{
					Tween.Scale(
						contentRoot,
						endValue: targetScale,
						duration: scaleShowDuration,
						ease: scaleShowEase
					);
				}
			}

			if (Mathf.Approximately(notificationGroup.alpha, visibleAlpha))
			{
				notificationGroup.alpha = visibleAlpha;
			}
			else
			{
				Tween.Alpha(
					notificationGroup,
					endValue: visibleAlpha,
					duration: fadeShowDuration,
					ease: fadeShowEase
				);
			}
		}

		public void Hide(Action onCompleted = null)
		{
			if (notificationGroup == null)
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

			StopTweens();
			notificationGroup.interactable = false;
			notificationGroup.blocksRaycasts = false;

			if (contentRoot != null)
			{
				Vector3 targetScale = Vector3.one * hiddenScale;

				if ((contentRoot.localScale - targetScale).sqrMagnitude <= 0.000001f)
				{
					contentRoot.localScale = targetScale;
				}
				else
				{
					Tween.Scale(
						contentRoot,
						endValue: targetScale,
						duration: scaleHideDuration,
						ease: scaleHideEase
					);
				}
			}

			if (Mathf.Approximately(notificationGroup.alpha, hiddenAlpha))
			{
				notificationGroup.alpha = hiddenAlpha;
			}
			else
			{
				Tween.Alpha(
					notificationGroup,
					endValue: hiddenAlpha,
					duration: fadeHideDuration,
					ease: fadeHideEase
				);
			}

			float hideCompletionDelay = Mathf.Max(fadeHideDuration, contentRoot != null ? scaleHideDuration : 0f);
			hideCompletionTween = Tween.Delay(hideCompletionDelay).OnComplete(CompleteHide);
		}

		public void HideInstant()
		{
			hideCompleted = null;
			IsVisible = false;
			StopTweens();

			if (contentRoot != null)
			{
				contentRoot.localScale = Vector3.one * hiddenScale;
			}

			if (notificationGroup == null)
			{
				return;
			}

			notificationGroup.alpha = hiddenAlpha;
			notificationGroup.interactable = false;
			notificationGroup.blocksRaycasts = false;

			if (deactivateWhenHidden)
			{
				notificationGroup.gameObject.SetActive(false);
			}
		}

		private void CompleteHide()
		{
			if (contentRoot != null)
			{
				contentRoot.localScale = Vector3.one * hiddenScale;
			}

			if (notificationGroup != null)
			{
				notificationGroup.alpha = hiddenAlpha;
				notificationGroup.interactable = false;
				notificationGroup.blocksRaycasts = false;

				if (deactivateWhenHidden)
				{
					notificationGroup.gameObject.SetActive(false);
				}
			}

			Action completed = hideCompleted;
			hideCompleted = null;
			completed?.Invoke();
		}

		private void StopTweens()
		{
			if (notificationGroup != null)
			{
				Tween.StopAll(onTarget: notificationGroup);
			}

			if (contentRoot != null)
			{
				Tween.StopAll(onTarget: contentRoot);
			}

			if (hideCompletionTween.isAlive)
			{
				hideCompletionTween.Stop();
			}
		}

	}
}
