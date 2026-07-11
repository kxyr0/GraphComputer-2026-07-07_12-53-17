using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Notifications
{
	[DisallowMultipleComponent]
	public sealed class NotificationView : MonoBehaviour
	{
		[Header("Root")]
		[SerializeField] private CanvasGroup notificationGroup;
		[SerializeField] private RectTransform contentRoot;

		[Header("Content")]
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text bodyText;
		[SerializeField] private Image statusCircle;

		[Header("Buttons")]
		[SerializeField] private ButtonManager primaryButton;
		[SerializeField] private ButtonManager secondaryButton;
		[SerializeField] private bool tintButtonTextByStatus;

		[Header("Status")]
		[SerializeField] private NotificationStatusColorPalette statusColors = new();

		[Header("Animation")]
		[SerializeField] private NotificationAnimator animator = new();

		[Header("Startup")]
		[SerializeField] private bool showInitialOnStart;
		[SerializeField] private NotificationEvent initialNotification;

		[Header("Events")]
		[SerializeField] private UnityEvent shown;
		[SerializeField] private UnityEvent hidden;

		private NotificationButtonBinder primaryBinder;
		private NotificationButtonBinder secondaryBinder;
		private NotificationEvent currentNotification;

		public bool IsVisible => animator != null && animator.IsVisible;

		private void Reset()
		{
			notificationGroup = GetComponent<CanvasGroup>();
			contentRoot = transform as RectTransform;
			titleText = transform.Find("Title")?.GetComponent<TMP_Text>();
			bodyText = transform.Find("BodyText")?.GetComponent<TMP_Text>();
			statusCircle = GetComponentInChildren<Image>(true);
		}

		private void Awake()
		{
			if (notificationGroup == null)
			{
				notificationGroup = GetComponent<CanvasGroup>();
			}

			if (contentRoot == null)
			{
				contentRoot = transform as RectTransform;
			}

			animator ??= new NotificationAnimator();
			animator.UseFallbackTargets(notificationGroup, contentRoot);

			primaryBinder = new NotificationButtonBinder(primaryButton, OnPrimaryClicked);
			secondaryBinder = new NotificationButtonBinder(secondaryButton, OnSecondaryClicked);

			if (!showInitialOnStart)
			{
				HideInstant();
			}
		}

		private void Start()
		{
			if (showInitialOnStart && initialNotification != null)
			{
				Show(initialNotification);
			}
		}

		private void OnDisable()
		{
			primaryBinder?.Unbind();
			secondaryBinder?.Unbind();
		}

		public void Show(NotificationEvent notificationEvent)
		{
			if (notificationEvent == null)
			{
				Hide();
				return;
			}

			currentNotification = notificationEvent;
			Render(notificationEvent);
			animator.Show();
			shown?.Invoke();
		}

		public void Hide()
		{
			animator.Hide(OnHidden);
		}

		public void HideInstant()
		{
			primaryBinder?.Unbind();
			secondaryBinder?.Unbind();
			animator.HideInstant();
		}

		public void Clear()
		{
			currentNotification = null;
			SetText(titleText, string.Empty);
			SetText(bodyText, string.Empty);
			SetStatusColor(NotificationStatus.Info);
			primaryBinder?.Bind(null);
			secondaryBinder?.Bind(null);
		}

		private void Render(NotificationEvent notificationEvent)
		{
			if (notificationEvent == null)
			{
				Clear();
				return;
			}

			SetText(titleText, notificationEvent.Title);
			SetText(bodyText, notificationEvent.Body);
			SetStatusColor(notificationEvent.Status);

			primaryBinder.Bind(notificationEvent.PrimaryAction);
			secondaryBinder.Bind(notificationEvent.SecondaryAction);

			if (tintButtonTextByStatus)
			{
				Color statusColor = statusColors.Resolve(notificationEvent.Status);
				NotificationButtonBinder.SetTextColor(primaryButton, statusColor);
				NotificationButtonBinder.SetTextColor(secondaryButton, statusColor);
			}
		}

		private void SetStatusColor(NotificationStatus status)
		{
			if (statusCircle != null)
			{
				statusCircle.color = statusColors != null
					? statusColors.Resolve(status)
					: Color.white;
			}
		}

		private void OnPrimaryClicked()
		{
			HandleAction(currentNotification?.PrimaryAction);
		}

		private void OnSecondaryClicked()
		{
			HandleAction(currentNotification?.SecondaryAction);
		}

		private void HandleAction(NotificationAction action)
		{
			if (action == null || !action.IsSpecified)
			{
				return;
			}

			action.Invoke();

			if (action.CloseAfterClick)
			{
				Hide();
			}
		}

		private void OnHidden()
		{
			primaryBinder?.Unbind();
			secondaryBinder?.Unbind();
			hidden?.Invoke();
		}

		private static void SetText(TMP_Text text, string value)
		{
			if (text != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(text, value);
			}
		}
	}
}
