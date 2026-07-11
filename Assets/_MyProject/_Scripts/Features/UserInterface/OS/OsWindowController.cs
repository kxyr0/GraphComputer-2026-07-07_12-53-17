using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI.OS
{
	[DisallowMultipleComponent]
	public sealed class OsWindowController : MonoBehaviour
	{
		[Header("Window")]
		[SerializeField] private CanvasGroup windowGroup;
		[SerializeField] private RectTransform windowTransform;
		[SerializeField] private bool visibleOnStart;
		[SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
		[SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;

		[Header("Buttons")]
		[SerializeField] private Button closeButton;

		[Header("Tabs")]
		[SerializeField] private OsWindowTabsController tabsController;
		[SerializeField] private bool selectDefaultTabOnOpen = true;

		[Header("Input")]
		[SerializeField] private bool openWithTab = true;
		[SerializeField] private bool closeWithTabWhenVisible = true;
		[SerializeField] private bool ignoreTabWhenTextInputFocused = true;

		[Header("Input Actions")]
		[SerializeField] private InputActionReference toggleInputAction;
		[SerializeField] private bool closeWithToggleActionWhenVisible = true;
		[SerializeField] private bool ignoreToggleActionWhenTextInputFocused;

		[Header("Input Blocking")]
		[SerializeField] private CanvasGroup[] blockInputWhenGroupsVisible;
		[SerializeField, Range(0f, 1f)] private float blockInputAlphaThreshold = 0.05f;

		[Header("Animation")]
		[SerializeField] private bool animateScale = true;
		[SerializeField, Min(0f)] private float visibleScale = 1f;
		[SerializeField, Min(0f)] private float hiddenScale = 0.96f;
		[SerializeField, Min(0.01f)] private float showDuration = 0.16f;
		[SerializeField, Min(0.01f)] private float hideDuration = 0.12f;
		[SerializeField] private Ease showEase = Ease.OutBack;
		[SerializeField] private Ease hideEase = Ease.InCubic;

		[Header("Events")]
		[SerializeField] private UnityEvent opened;
		[SerializeField] private UnityEvent closed;

		private bool isVisible;
		private bool toggleInputActionEnabledByController;
		private int lastToggleInputActionFrame = -1;

		public bool IsVisible => isVisible;

		private void Reset()
		{
			windowGroup = GetComponent<CanvasGroup>();
			windowTransform = transform as RectTransform;
			closeButton = GetComponentInChildren<Button>(true);
			tabsController = GetComponentInChildren<OsWindowTabsController>(true);
		}

		private void Awake()
		{
			if (windowGroup == null)
			{
				windowGroup = GetComponent<CanvasGroup>();
			}

			if (windowTransform == null)
			{
				windowTransform = transform as RectTransform;
			}

			if (tabsController == null)
			{
				tabsController = GetComponentInChildren<OsWindowTabsController>(true);
			}

			ApplyInstant(visibleOnStart);

			if (visibleOnStart && selectDefaultTabOnOpen)
			{
				SelectDefaultTab();
			}
		}

		private void OnEnable()
		{
			if (closeButton != null)
			{
				closeButton.onClick.AddListener(Close);
			}

			RegisterToggleInputAction();
		}

		private void OnDisable()
		{
			if (closeButton != null)
			{
				closeButton.onClick.RemoveListener(Close);
			}

			UnregisterToggleInputAction();
		}

		private void Update()
		{
			if (lastToggleInputActionFrame == Time.frameCount)
			{
				return;
			}

			if (!openWithTab || WasTabPressedThisFrame() == false)
			{
				return;
			}

			if (IsInputBlocked() ||
				ignoreTabWhenTextInputFocused && IsTextInputFocused())
			{
				return;
			}

			if (isVisible && closeWithTabWhenVisible)
			{
				Close();
				return;
			}

			if (!isVisible)
			{
				Open();
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

		private void RegisterToggleInputAction()
		{
			InputAction action = toggleInputAction != null
				? toggleInputAction.action
				: null;

			if (action == null)
			{
				return;
			}

			action.performed += HandleToggleInputAction;

			if (!action.enabled)
			{
				action.Enable();
				toggleInputActionEnabledByController = true;
			}
			else
			{
				toggleInputActionEnabledByController = false;
			}
		}

		private void UnregisterToggleInputAction()
		{
			InputAction action = toggleInputAction != null
				? toggleInputAction.action
				: null;

			if (action == null)
			{
				toggleInputActionEnabledByController = false;
				return;
			}

			action.performed -= HandleToggleInputAction;

			if (toggleInputActionEnabledByController && action.enabled)
			{
				action.Disable();
			}

			toggleInputActionEnabledByController = false;
		}

		private void HandleToggleInputAction(InputAction.CallbackContext context)
		{
			lastToggleInputActionFrame = Time.frameCount;

			if (IsInputBlocked() ||
				ignoreToggleActionWhenTextInputFocused && IsTextInputFocused())
			{
				return;
			}

			if (isVisible && closeWithToggleActionWhenVisible)
			{
				Close();
				return;
			}

			if (!isVisible)
			{
				Open();
			}
		}

		private bool IsInputBlocked()
		{
			if (blockInputWhenGroupsVisible == null)
			{
				return false;
			}

			foreach (CanvasGroup group in blockInputWhenGroupsVisible)
			{
				if (group != null && group.alpha > blockInputAlphaThreshold)
				{
					return true;
				}
			}

			return false;
		}

		private void SetVisible(bool visible)
		{
			if (windowGroup == null)
			{
				Debug.LogWarning($"[{nameof(OsWindowController)}] Window CanvasGroup is missing.", this);
				return;
			}

			if (isVisible == visible)
			{
				return;
			}

			isVisible = visible;

			Tween.StopAll(onTarget: windowGroup);
			StopScaleTween();

			windowGroup.interactable = visible;
			windowGroup.blocksRaycasts = visible;

			if (visible && selectDefaultTabOnOpen)
			{
				SelectDefaultTab();
			}

			if (animateScale && windowTransform != null)
			{
				Vector3 targetScale = Vector3.one * (visible ? visibleScale : hiddenScale);

				if (visible)
				{
					windowTransform.localScale = Vector3.one * hiddenScale;
				}

				if ((windowTransform.localScale - targetScale).sqrMagnitude <= 0.000001f)
				{
					windowTransform.localScale = targetScale;
				}
				else
				{
					Tween.Scale(
						windowTransform,
						endValue: targetScale,
						duration: visible ? showDuration : hideDuration,
						ease: visible ? showEase : hideEase
					);
				}
			}

			float targetAlpha = visible ? visibleAlpha : hiddenAlpha;

			if (Mathf.Approximately(windowGroup.alpha, targetAlpha))
			{
				windowGroup.alpha = targetAlpha;
			}
			else
			{
				Tween.Alpha(
					windowGroup,
					endValue: targetAlpha,
					duration: visible ? showDuration : hideDuration,
					ease: visible ? showEase : hideEase
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

		private void SelectDefaultTab()
		{
			if (tabsController == null)
			{
				tabsController = GetComponentInChildren<OsWindowTabsController>(true);
			}

			tabsController?.SelectDefault();
		}

		private void ApplyInstant(bool visible)
		{
			isVisible = visible;

			if (windowGroup == null)
			{
				return;
			}

			Tween.StopAll(onTarget: windowGroup);
			StopScaleTween();

			windowGroup.alpha = visible ? visibleAlpha : hiddenAlpha;
			windowGroup.interactable = visible;
			windowGroup.blocksRaycasts = visible;

			if (animateScale && windowTransform != null)
			{
				windowTransform.localScale = Vector3.one * (visible ? visibleScale : hiddenScale);
			}
		}

		private void StopScaleTween()
		{
			if (windowTransform != null)
			{
				Tween.StopAll(onTarget: windowTransform);
			}
		}

		private static bool WasTabPressedThisFrame()
		{
			Keyboard keyboard = Keyboard.current;
			return keyboard != null && keyboard.tabKey.wasPressedThisFrame;
		}

		private static bool IsTextInputFocused()
		{
			GameObject selectedObject = EventSystem.current != null
				? EventSystem.current.currentSelectedGameObject
				: null;

			if (selectedObject == null)
			{
				return false;
			}

			return selectedObject.GetComponent<TMP_InputField>() != null ||
				   selectedObject.GetComponent<InputField>() != null;
		}
	}
}
