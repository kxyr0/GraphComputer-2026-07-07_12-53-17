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

		[Header("Close Cleanup")]
		[SerializeField] private CanvasGroup[] hideOnCloseGroups;
		[SerializeField, Range(0f, 1f)] private float hideOnCloseAlpha = 0f;

		[Header("Open Recovery")]
		[SerializeField] private bool restoreZeroScaleOnOpen = true;
		[SerializeField] private Vector3 restoredOpenScale = Vector3.one;
		[SerializeField] private RectTransform[] restoreScaleOnOpenTargets;
		[SerializeField] private bool refreshChildTabsOnOpen = true;

		[Header("Open Order")]
		[SerializeField] private bool activateGroupsOnOpen = true;
		[SerializeField] private bool bringGroupsToFrontOnOpen = true;
		[SerializeField] private bool useChildCanvasGroupsFallback = true;

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
		[SerializeField] private bool useUnscaledTime = true;

		[Header("Events")]
		[SerializeField] private UnityEvent opened;
		[SerializeField] private UnityEvent closed;

		private readonly List<CanvasGroup> validGroups = new();
		private bool isVisible;

		public bool IsVisible => isVisible;

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

			if (!isVisible)
			{
				HideCloseCleanupGroups();
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
			SetVisible(true, force: true, instant: true);
		}

		public void OpenAnimated()
		{
			SetVisible(true, force: true, instant: false);
		}

		public void Close()
		{
			SetVisible(false, force: true);
		}

		public void Toggle()
		{
			bool visible = !isVisible;
			SetVisible(visible, force: true, instant: visible);
		}

		private void SetVisible(bool visible, bool force)
		{
			SetVisible(visible, force, instant: false);
		}

		private void SetVisible(bool visible, bool force, bool instant)
		{
			RebuildGroups();

			if (!force && isVisible == visible)
			{
				return;
			}

			isVisible = visible;

			if (visible)
			{
				PrepareCanvasForOpen();
				PrepareGroupsForOpen();
				RestoreScalesOnOpen();
			}

			float targetAlpha = visible ? visibleAlpha : hiddenAlpha;
			float duration = visible ? showDuration : hideDuration;
			Ease ease = visible ? showEase : hideEase;
			bool applyInstant = instant || duration <= 0f;

			foreach (CanvasGroup group in validGroups)
			{
				if (group == null)
				{
					continue;
				}

				Tween.StopAll(onTarget: group);

				group.interactable = visible;
				group.blocksRaycasts = visible;

				if (applyInstant || Mathf.Approximately(group.alpha, targetAlpha))
				{
					group.alpha = targetAlpha;
					continue;
				}

				Tween.Alpha(
					group,
					endValue: targetAlpha,
					duration: duration,
					ease: ease,
					useUnscaledTime: useUnscaledTime
				);
			}

			if (visible)
			{
				RefreshChildTabsOnOpen();
				Canvas.ForceUpdateCanvases();
				opened?.Invoke();
			}
			else
			{
				HideCloseCleanupGroups();
				closed?.Invoke();
			}
		}

		private void ApplyInstant(bool visible)
		{
			if (visible)
			{
				PrepareCanvasForOpen();
				PrepareGroupsForOpen();
				RestoreScalesOnOpen();
			}

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

			if (!visible)
			{
				HideCloseCleanupGroups();
			}
		}

		private void HideCloseCleanupGroups()
		{
			if (hideOnCloseGroups == null)
			{
				return;
			}

			foreach (CanvasGroup group in hideOnCloseGroups)
			{
				if (group == null)
				{
					continue;
				}

				Tween.StopAll(onTarget: group);

				group.alpha = hideOnCloseAlpha;
				group.interactable = false;
				group.blocksRaycasts = false;
			}
		}

		private void RestoreScalesOnOpen()
		{
			RestoreScaleChainIfZero(transform);

			foreach (CanvasGroup group in validGroups)
			{
				if (group != null)
				{
					RestoreScaleChainIfZero(group.transform);
				}
			}

			if (!restoreZeroScaleOnOpen)
			{
				return;
			}

			if (restoreScaleOnOpenTargets == null)
			{
				return;
			}

			foreach (RectTransform target in restoreScaleOnOpenTargets)
			{
				RestoreScaleIfZero(target);
			}
		}

		private void PrepareCanvasForOpen()
		{
			ActivateHierarchyForOpen();

			Canvas ownCanvas = GetComponent<Canvas>();

			if (ownCanvas != null)
			{
				ownCanvas.enabled = true;
			}

			GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();

			if (raycaster != null)
			{
				raycaster.enabled = true;
			}
		}

		private void ActivateHierarchyForOpen()
		{
			Transform current = transform;

			while (current != null)
			{
				if (!current.gameObject.activeSelf)
				{
					current.gameObject.SetActive(true);
				}

				current = current.parent;
			}
		}

		private void PrepareGroupsForOpen()
		{
			foreach (CanvasGroup group in validGroups)
			{
				if (group == null)
				{
					continue;
				}

				if (activateGroupsOnOpen)
				{
					group.gameObject.SetActive(true);
				}

				if (bringGroupsToFrontOnOpen)
				{
					group.transform.SetAsLastSibling();
				}
			}
		}

		private void RestoreScaleChainIfZero(Transform target)
		{
			Transform current = target;

			while (current != null)
			{
				RestoreScaleIfZero(current);
				current = current.parent;
			}
		}

		private void RestoreScaleIfZero(Transform target)
		{
			if (target == null)
			{
				return;
			}

			Vector3 scale = target.localScale;

			if (Mathf.Abs(scale.x) > 0.0001f &&
				Mathf.Abs(scale.y) > 0.0001f &&
				Mathf.Abs(scale.z) > 0.0001f)
			{
				return;
			}

			target.localScale = restoredOpenScale;
		}

		private void RefreshChildTabsOnOpen()
		{
			if (!refreshChildTabsOnOpen)
			{
				return;
			}

			IdeBottomTabsController[] tabControllers =
				GetComponentsInChildren<IdeBottomTabsController>(true);

			foreach (IdeBottomTabsController tabController in tabControllers)
			{
				tabController?.RefreshCurrentTab(animated: false);
			}

			IdeCodeTabsController[] codeTabsControllers =
				GetComponentsInChildren<IdeCodeTabsController>(true);

			foreach (IdeCodeTabsController codeTabsController in codeTabsControllers)
			{
				codeTabsController?.RefreshAllTabsVisuals();
			}
		}

		private void RebuildGroups()
		{
			validGroups.Clear();

			if (groups != null)
			{
				foreach (CanvasGroup group in groups)
				{
					TryAddGroup(group);
				}
			}

			if (validGroups.Count > 0 || !useChildCanvasGroupsFallback)
			{
				return;
			}

			CanvasGroup ownGroup = GetComponent<CanvasGroup>();
			TryAddGroup(ownGroup);

			if (validGroups.Count > 0)
			{
				return;
			}

			CanvasGroup[] childGroups = GetComponentsInChildren<CanvasGroup>(true);

			foreach (CanvasGroup group in childGroups)
			{
				TryAddGroup(group);

				if (validGroups.Count > 0)
				{
					return;
				}
			}
		}

		private void TryAddGroup(CanvasGroup group)
		{
			if (group == null || validGroups.Contains(group) || IsExcluded(group))
			{
				return;
			}

			validGroups.Add(group);
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
