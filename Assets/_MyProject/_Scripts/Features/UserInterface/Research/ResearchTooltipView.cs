using System.Text;
using Game.Research;
using Game.UI.Notifications;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Game.UI.Research
{
	[DisallowMultipleComponent]
	public sealed class ResearchTooltipView : MonoBehaviour
	{
		[Header("Root")]
		[SerializeField] private CanvasGroup tooltipGroup;
		[SerializeField] private RectTransform contentRoot;

		[Header("Content")]
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text bodyText;
		[SerializeField] private TMP_Text researchTimeText;

		[Header("Placement")]
		[SerializeField] private bool followCursor = true;
		[SerializeField, FormerlySerializedAs("targetOffset")] private Vector2 cursorOffset = new(24f, -8f);
		[SerializeField] private RectTransform clampRoot;
		[SerializeField] private bool keepInsideClampRoot = true;

		[Header("Animation")]
		[SerializeField] private NotificationAnimator animator = new();

		[Header("Events")]
		[SerializeField] private UnityEvent shown;
		[SerializeField] private UnityEvent hidden;

		private readonly Vector3[] contentCorners = new Vector3[4];
		private readonly Vector3[] clampCorners = new Vector3[4];
		private Vector2 currentCursorScreenPosition;
		private Camera currentEventCamera;
		private bool hasCursorPosition;

		public bool IsVisible => animator != null && animator.IsVisible;

		private void Awake()
		{
			ValidateReferences();
			animator ??= new NotificationAnimator();
			animator.UseFallbackTargets(tooltipGroup, contentRoot);
			HideInstant();
		}

		private void LateUpdate()
		{
			if (!IsVisible || !followCursor || !hasCursorPosition)
			{
				return;
			}

			PlaceAtCursor(currentCursorScreenPosition, currentEventCamera);
		}

		public void Show(
			ResearchDefinition definition,
			ResearchState state,
			Vector2 cursorScreenPosition,
			Camera eventCamera)
		{
			if (tooltipGroup == null || contentRoot == null)
			{
				Debug.LogError(
					$"[{nameof(ResearchTooltipView)}] Tooltip Group and Content Root must be assigned.",
					this);
				return;
			}

			if (definition == null)
			{
				Hide();
				return;
			}

			hasCursorPosition = true;
			currentCursorScreenPosition = cursorScreenPosition;
			currentEventCamera = eventCamera;
			Render(definition);
			PlaceAtCursor(cursorScreenPosition, eventCamera);

			animator.Show();
			DisableTooltipRaycasts();
			shown?.Invoke();
		}

		public void MoveToCursor(Vector2 cursorScreenPosition, Camera eventCamera)
		{
			if (contentRoot == null)
			{
				return;
			}

			hasCursorPosition = true;
			currentCursorScreenPosition = cursorScreenPosition;
			currentEventCamera = eventCamera;

			if (IsVisible)
			{
				PlaceAtCursor(cursorScreenPosition, eventCamera);
			}
		}

		public void Hide()
		{
			hasCursorPosition = false;
			animator.Hide(OnHidden);
		}

		public void HideInstant()
		{
			hasCursorPosition = false;
			animator.HideInstant();
		}

		private void Render(ResearchDefinition definition)
		{
			SetText(titleText, definition.Title);
			SetText(bodyText, BuildBody(definition));
			SetText(researchTimeText, FormatResearchTime(definition.CostDays));
		}

		private void PlaceAtCursor(Vector2 cursorScreenPosition, Camera eventCamera)
		{
			if (!followCursor || contentRoot == null)
			{
				return;
			}

			Vector2 screenPosition = cursorScreenPosition + cursorOffset;
			RectTransform positioningRoot = contentRoot.parent as RectTransform;

			if (positioningRoot != null &&
				RectTransformUtility.ScreenPointToWorldPointInRectangle(
					positioningRoot,
					screenPosition,
					eventCamera,
					out Vector3 worldPosition))
			{
				contentRoot.position = worldPosition;
			}
			else
			{
				contentRoot.position = screenPosition;
			}

			ClampInsideRoot();
		}

		private static string BuildBody(ResearchDefinition definition)
		{
			StringBuilder builder = new();

			if (!string.IsNullOrWhiteSpace(definition.Description))
			{
				builder.Append(definition.Description);
			}

			AppendList(builder, "Requires", definition.PrerequisiteIds);
			AppendList(builder, "Choice lock", definition.MutualExclusionIds);

			return builder.ToString();
		}

		private static string FormatResearchTime(int costDays)
		{
			return $"Research Time: {costDays} {(costDays == 1 ? "day" : "days")}";
		}

		private static void AppendList(StringBuilder builder, string label, string[] values)
		{
			if (values == null || values.Length == 0)
			{
				return;
			}

			bool hasAnyValue = false;

			foreach (string value in values)
			{
				if (!string.IsNullOrWhiteSpace(value))
				{
					hasAnyValue = true;
					break;
				}
			}

			if (!hasAnyValue)
			{
				return;
			}

			if (builder.Length > 0)
			{
				builder.AppendLine();
				builder.AppendLine();
			}

			builder.Append(label);
			builder.Append(": ");

			bool needsSeparator = false;

			foreach (string value in values)
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					continue;
				}

				if (needsSeparator)
				{
					builder.Append(", ");
				}

				builder.Append(value);
				needsSeparator = true;
			}
		}

		private void OnHidden()
		{
			hidden?.Invoke();
		}

		private void DisableTooltipRaycasts()
		{
			if (tooltipGroup == null)
			{
				return;
			}

			tooltipGroup.interactable = false;
			tooltipGroup.blocksRaycasts = false;
		}

		private void ValidateReferences()
		{
			if (tooltipGroup == null)
			{
				Debug.LogError($"[{nameof(ResearchTooltipView)}] Tooltip Group is not assigned.", this);
			}

			if (contentRoot == null)
			{
				Debug.LogError($"[{nameof(ResearchTooltipView)}] Content Root is not assigned.", this);
			}

			if (titleText == null)
			{
				Debug.LogWarning($"[{nameof(ResearchTooltipView)}] Title Text is not assigned.", this);
			}

			if (bodyText == null)
			{
				Debug.LogWarning($"[{nameof(ResearchTooltipView)}] Body Text is not assigned.", this);
			}

			if (researchTimeText == null)
			{
				Debug.LogWarning($"[{nameof(ResearchTooltipView)}] Research Time Text is not assigned.", this);
			}
		}

		private void ClampInsideRoot()
		{
			if (!keepInsideClampRoot || clampRoot == null || contentRoot == null)
			{
				return;
			}

			contentRoot.GetWorldCorners(contentCorners);
			clampRoot.GetWorldCorners(clampCorners);

			Vector3 adjustment = Vector3.zero;

			if (contentCorners[0].x < clampCorners[0].x)
			{
				adjustment.x = clampCorners[0].x - contentCorners[0].x;
			}
			else if (contentCorners[2].x > clampCorners[2].x)
			{
				adjustment.x = clampCorners[2].x - contentCorners[2].x;
			}

			if (contentCorners[0].y < clampCorners[0].y)
			{
				adjustment.y = clampCorners[0].y - contentCorners[0].y;
			}
			else if (contentCorners[2].y > clampCorners[2].y)
			{
				adjustment.y = clampCorners[2].y - contentCorners[2].y;
			}

			contentRoot.position += adjustment;
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
