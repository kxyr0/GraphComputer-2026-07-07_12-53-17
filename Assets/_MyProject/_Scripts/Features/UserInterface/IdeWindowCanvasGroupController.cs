using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeWindowCanvasGroupController : MonoBehaviour
	{
		[Header("Groups To Control")]
		[SerializeField] private CanvasGroup[] groups;
		[SerializeField] private CanvasGroup[] excludedGroups;

		[Header("Buttons")]
		[SerializeField] private Button closeButton;
		[SerializeField] private Button openButton;

		[Header("Input")]
		[SerializeField] private bool closeWithEscape = true;

		[Header("State")]
		[SerializeField] private bool visibleOnStart;
		[SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
		[SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;

		[Header("Animation")]
		[SerializeField, Min(0.01f)] private float showDuration = 0.16f;
		[SerializeField, Min(0.01f)] private float hideDuration = 0.12f;
		[SerializeField] private Ease showEase = Ease.OutCubic;
		[SerializeField] private Ease hideEase = Ease.InCubic;

		[Header("Events")]
		[SerializeField] private UnityEvent opened;
		[SerializeField] private UnityEvent closed;

		private readonly List<CanvasGroup> validGroups = new();
		private bool isVisible;

		private void Awake()
		{
			RebuildGroups();

			if (closeButton != null)
			{
				closeButton.onClick.AddListener(Close);
			}

			if (openButton != null)
			{
				openButton.onClick.AddListener(Open);
			}

			isVisible = visibleOnStart;
			ApplyInstant(isVisible);
		}

		private void OnDestroy()
		{
			if (closeButton != null)
			{
				closeButton.onClick.RemoveListener(Close);
			}

			if (openButton != null)
			{
				openButton.onClick.RemoveListener(Open);
			}
		}

		private void Update()
		{
			if (!closeWithEscape || !isVisible)
			{
				return;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
			{
				Close();
			}
		}

		public void Open()
		{
			SetVisible(true, force: true);
		}

		public void Close()
		{
			SetVisible(false, force: true);
		}

		public void Toggle()
		{
			SetVisible(!isVisible, force: true);
		}

		private void SetVisible(bool visible, bool force)
		{
			RebuildGroups();

			if (!force && isVisible == visible)
			{
				return;
			}

			isVisible = visible;

			float targetAlpha = visible ? visibleAlpha : hiddenAlpha;
			float duration = visible ? showDuration : hideDuration;
			Ease ease = visible ? showEase : hideEase;

			foreach (CanvasGroup group in validGroups)
			{
				if (group == null)
				{
					continue;
				}

				Tween.StopAll(onTarget: group);

				group.interactable = visible;
				group.blocksRaycasts = visible;

				if (Mathf.Approximately(group.alpha, targetAlpha))
				{
					group.alpha = targetAlpha;
					continue;
				}

				Tween.Alpha(
					group,
					endValue: targetAlpha,
					duration: duration,
					ease: ease
				);
			}

			if (visible)
			{
				opened?.Invoke();
			}
			else
			{
				closed?.Invoke();
			}
		}

		private void ApplyInstant(bool visible)
		{
			float alpha = visible ? visibleAlpha : hiddenAlpha;

			foreach (CanvasGroup group in validGroups)
			{
				if (group == null)
				{
					continue;
				}

				Tween.StopAll(onTarget: group);

				group.alpha = alpha;
				group.interactable = visible;
				group.blocksRaycasts = visible;
			}
		}

		private void RebuildGroups()
		{
			validGroups.Clear();

			if (groups == null)
			{
				return;
			}

			foreach (CanvasGroup group in groups)
			{
				if (group == null || validGroups.Contains(group) || IsExcluded(group))
				{
					continue;
				}

				validGroups.Add(group);
			}
		}

		private bool IsExcluded(CanvasGroup candidate)
		{
			if (excludedGroups == null)
			{
				return false;
			}

			foreach (CanvasGroup excludedGroup in excludedGroups)
			{
				if (excludedGroup == candidate)
				{
					return true;
				}
			}

			return false;
		}
	}
}
