using Michsky.MUIP;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI.Research
{
	[DisallowMultipleComponent]
	public sealed class ResearchWindowController : MonoBehaviour
	{
		[Header("Window")]
		[SerializeField] private CanvasGroup windowGroup;
		[SerializeField] private RectTransform windowTransform;
		[SerializeField] private bool visibleOnStart;
		[SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
		[SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;

		[Header("Buttons")]
		[SerializeField] private Button closeButton;
		[SerializeField] private ButtonManager closeButtonManager;
		[SerializeField] private Button openButton;
		[SerializeField] private ButtonManager openButtonManager;

		[Header("Tabs")]
		[SerializeField] private ResearchTabsController tabsController;
		[SerializeField] private bool selectDefaultTabOnOpen = true;

		[Header("Input Actions")]
		[SerializeField] private InputActionReference toggleInputAction;
		[SerializeField] private bool closeWithToggleActionWhenVisible = true;
		[SerializeField] private bool ignoreToggleActionWhenTextInputFocused;

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

		public bool IsVisible => isVisible;

		private void Reset()
		{
			windowGroup = GetComponent<CanvasGroup>();
			windowTransform = transform as RectTransform;
			tabsController = GetComponentInChildren<ResearchTabsController>(true);
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
				tabsController = GetComponentInChildren<ResearchTabsController>(true);
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

			if (closeButtonManager != null)
			{
				closeButtonManager.onClick.AddListener(Close);
			}

			if (openButton != null)
			{
				openButton.onClick.AddListener(Open);
			}

			if (openButtonManager != null)
			{
				openButtonManager.onClick.AddListener(Open);
			}

			RegisterToggleInputAction();
		}

		private void OnDisable()
		{
			if (closeButton != null)
			{
				closeButton.onClick.RemoveListener(Close);
			}

			if (closeButtonManager != null)
			{
				closeButtonManager.onClick.RemoveListener(Close);
			}

			if (openButton != null)
			{
				openButton.onClick.RemoveListener(Open);
			}

			if (openButtonManager != null)
			{
				openButtonManager.onClick.RemoveListener(Open);
			}

			UnregisterToggleInputAction();
		}

		private void OnDestroy()
		{
			if (windowGroup != null)
			{
				Tween.StopAll(onTarget: windowGroup);
			}

			StopScaleTween();
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
			if (ignoreToggleActionWhenTextInputFocused && IsTextInputFocused())
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

		private void SetVisible(bool visible)
		{
			if (windowGroup == null)
			{
				Debug.LogWarning($"[{nameof(ResearchWindowController)}] Window CanvasGroup is missing.", this);
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
				tabsController = GetComponentInChildren<ResearchTabsController>(true);
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
