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
		[Header("Root")]
		[SerializeField] private RectTransform root;
		[SerializeField] private bool collectCanvasGroupsFromChildren = true;
		[SerializeField] private CanvasGroup[] manualGroups;

		[Header("Buttons")]
		[SerializeField] private Button closeButton;
		[SerializeField] private Button openButton;

		[Header("Input")]
		[SerializeField] private bool closeWithEscape = true;

		[Header("State")]
		[SerializeField] private bool visibleOnStart = true;

		[Header("Animation")]
		[SerializeField, Min(0.01f)] private float showDuration = 0.16f;
		[SerializeField, Min(0.01f)] private float hideDuration = 0.12f;
		[SerializeField] private Ease showEase = Ease.OutCubic;
		[SerializeField] private Ease hideEase = Ease.InCubic;

		[Header("Events")]
		[SerializeField] private UnityEvent opened;
		[SerializeField] private UnityEvent closed;

		private readonly List<CanvasGroup> groups = new();
		private readonly Dictionary<CanvasGroup, CanvasGroupState> lastVisibleStates = new();

		private bool isVisible;
		private int tweenVersion;

		private void Reset()
		{
			root = transform as RectTransform;
		}

		private void Awake()
		{
			if (root == null)
			{
				root = transform as RectTransform;
			}

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

			if (isVisible)
			{
				CaptureCurrentStates();
				ApplyInstantVisible();
			}
			else
			{
				CaptureCurrentStates();
				ApplyInstantHidden();
			}
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
			SetVisible(true);
		}

		public void Close()
		{
			SetVisible(false);
		}

		public void Toggle()
		{
			SetVisible(!isVisible);
		}

		private void SetVisible(bool value)
		{
			if (isVisible == value)
			{
				return;
			}

			isVisible = value;
			tweenVersion++;

			RebuildGroups();

			if (value)
			{
				AnimateOpen(tweenVersion);
			}
			else
			{
				CaptureCurrentStates();
				AnimateClose(tweenVersion);
			}
		}

		private void AnimateOpen(int version)
		{
			if (groups.Count == 0)
			{
				opened?.Invoke();
				return;
			}

			int pending = 0;

			foreach (CanvasGroup group in groups)
			{
				if (group == null)
				{
					continue;
				}

				pending++;

				Tween.StopAll(onTarget: group);

				CanvasGroupState state = lastVisibleStates.TryGetValue(group, out CanvasGroupState savedState)
					? savedState
					: CanvasGroupState.Visible;

				group.interactable = state.Interactable;
				group.blocksRaycasts = state.BlocksRaycasts;

				Tween.Alpha(
						group,
						endValue: state.Alpha,
						duration: showDuration,
						ease: showEase
					)
					.OnComplete(() =>
					{
						pending--;

						if (pending <= 0 && version == tweenVersion)
						{
							opened?.Invoke();
						}
					});
			}

			if (pending == 0)
			{
				opened?.Invoke();
			}
		}

		private void AnimateClose(int version)
		{
			if (groups.Count == 0)
			{
				closed?.Invoke();
				return;
			}

			int pending = 0;

			foreach (CanvasGroup group in groups)
			{
				if (group == null)
				{
					continue;
				}

				pending++;

				Tween.StopAll(onTarget: group);

				group.interactable = false;
				group.blocksRaycasts = false;

				Tween.Alpha(
						group,
						endValue: 0f,
						duration: hideDuration,
						ease: hideEase
					)
					.OnComplete(() =>
					{
						pending--;

						if (pending <= 0 && version == tweenVersion)
						{
							closed?.Invoke();
						}
					});
			}

			if (pending == 0)
			{
				closed?.Invoke();
			}
		}

		private void ApplyInstantVisible()
		{
			foreach (CanvasGroup group in groups)
			{
				if (group == null)
				{
					continue;
				}

				CanvasGroupState state = lastVisibleStates.TryGetValue(group, out CanvasGroupState savedState)
					? savedState
					: CanvasGroupState.Visible;

				Tween.StopAll(onTarget: group);

				group.alpha = state.Alpha;
				group.interactable = state.Interactable;
				group.blocksRaycasts = state.BlocksRaycasts;
			}
		}

		private void ApplyInstantHidden()
		{
			foreach (CanvasGroup group in groups)
			{
				if (group == null)
				{
					continue;
				}

				Tween.StopAll(onTarget: group);

				group.alpha = 0f;
				group.interactable = false;
				group.blocksRaycasts = false;
			}
		}

		private void CaptureCurrentStates()
		{
			lastVisibleStates.Clear();

			foreach (CanvasGroup group in groups)
			{
				if (group == null)
				{
					continue;
				}

				lastVisibleStates[group] = new CanvasGroupState(
					group.alpha,
					group.interactable,
					group.blocksRaycasts
				);
			}
		}

		private void RebuildGroups()
		{
			groups.Clear();

			if (collectCanvasGroupsFromChildren && root != null)
			{
				root.GetComponentsInChildren(true, groups);
			}

			if (manualGroups == null)
			{
				return;
			}

			foreach (CanvasGroup group in manualGroups)
			{
				if (group == null || groups.Contains(group))
				{
					continue;
				}

				groups.Add(group);
			}
		}

		private readonly struct CanvasGroupState
		{
			public static readonly CanvasGroupState Visible = new(1f, true, true);

			public readonly float Alpha;
			public readonly bool Interactable;
			public readonly bool BlocksRaycasts;

			public CanvasGroupState(float alpha, bool interactable, bool blocksRaycasts)
			{
				Alpha = alpha;
				Interactable = interactable;
				BlocksRaycasts = blocksRaycasts;
			}
		}
	}
}