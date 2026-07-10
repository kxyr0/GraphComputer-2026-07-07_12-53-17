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

		[Header("Input")]
		[SerializeField] private bool openWithTab = true;
		[SerializeField] private bool closeWithTabWhenVisible = true;
		[SerializeField] private bool ignoreTabWhenTextInputFocused = true;

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

		public bool IsVisible => isVisible;

		private void Reset()
		{
			windowGroup = GetComponent<CanvasGroup>();
			windowTransform = transform as RectTransform;
			closeButton = GetComponentInChildren<Button>(true);
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

			ApplyInstant(visibleOnStart);
		}

		private void OnEnable()
		{
			if (closeButton != null)
			{
				closeButton.onClick.AddListener(Close);
			}
		}

		private void OnDisable()
		{
			if (closeButton != null)
			{
				closeButton.onClick.RemoveListener(Close);
			}
		}

		private void Update()
		{
			if (!openWithTab || WasTabPressedThisFrame() == false)
			{
				return;
			}

			if (ignoreTabWhenTextInputFocused && IsTextInputFocused())
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

			if (animateScale && windowTransform != null)
			{
				if (visible)
				{
					windowTransform.localScale = Vector3.one * hiddenScale;
				}

				Tween.Scale(
					windowTransform,
					endValue: Vector3.one * (visible ? visibleScale : hiddenScale),
					duration: visible ? showDuration : hideDuration,
					ease: visible ? showEase : hideEase
				);
			}

			Tween.Alpha(
				windowGroup,
				endValue: visible ? visibleAlpha : hiddenAlpha,
				duration: visible ? showDuration : hideDuration,
				ease: visible ? showEase : hideEase
			);

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
