using System;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Research
{
	[DisallowMultipleComponent]
	public sealed class ResearchButtonView : MonoBehaviour
	{
		[SerializeField] private Button button;
		[SerializeField] private ButtonManager buttonManager;
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text costText;
		[SerializeField] private TMP_Text statusText;
		[SerializeField] private ResearchTooltipBinder tooltipBinder;
		[SerializeField] private Graphic backgroundGraphic;
		[SerializeField] private Graphic frameGraphic;
		[SerializeField] private Graphic iconGraphic;

		[Header("Colors")]
		[SerializeField] private Color availableColor = new(0.16f, 0.18f, 0.22f, 1f);
		[SerializeField] private Color lockedColor = new(0.08f, 0.08f, 0.09f, 0.72f);
		[SerializeField] private Color inProgressColor = new(0f, 0.55f, 1f, 1f);
		[SerializeField] private Color completedColor = new(0.08f, 0.24f, 0.14f, 1f);
		[SerializeField] private Color excludedColor = new(0.28f, 0.06f, 0.06f, 1f);
		[SerializeField] private Color normalTextColor = Color.white;
		[SerializeField] private Color disabledTextColor = new(0.55f, 0.55f, 0.55f, 1f);

		[Header("Optional Materials")]
		[SerializeField] private Material inProgressBackgroundMaterial;
		[SerializeField] private bool restoreInitialBackgroundMaterial = true;

		private Game.Research.ResearchDefinition definition;
		private Action<string> clicked;
		private UnityAction cachedClickAction;
		private Material initialBackgroundMaterial;
		private bool hasInitialBackgroundMaterial;

		public string ResearchId => definition != null ? definition.Id : string.Empty;
		public RectTransform RectTransform => transform as RectTransform;

		private void Awake()
		{
			CacheInitialMaterials();
		}

		private void OnDestroy()
		{
			RemoveListener();
		}

		public void Initialize(Game.Research.ResearchDefinition researchDefinition, Action<string> onClicked)
		{
			definition = researchDefinition;
			clicked = onClicked;

			RemoveListener();

			cachedClickAction = HandleClicked;

			if (buttonManager != null)
			{
				buttonManager.onClick.AddListener(cachedClickAction);
			}
			else if (button != null)
			{
				button.onClick.AddListener(cachedClickAction);
			}
			else
			{
				Debug.LogWarning(
					$"[{nameof(ResearchButtonView)}] '{name}' has no Button or ButtonManager assigned.",
					this);
			}

			if (titleText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(titleText, definition.Title);
			}

			if (buttonManager != null)
			{
				buttonManager.enableText = true;
				Game.UI.RuntimeTextRefresh.SetButtonText(buttonManager, definition.Title);
			}

			if (costText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(costText, $"{definition.CostDays} day(s)");
			}

			ValidateViewReferences();
		}

		public void InitializeTooltip(ResearchTooltipView tooltipView)
		{
			if (definition == null)
			{
				return;
			}

			if (tooltipBinder == null)
			{
				Debug.LogWarning(
					$"[{nameof(ResearchButtonView)}] '{name}' has no ResearchTooltipBinder assigned. Tooltip is skipped.",
					this);
				return;
			}

			tooltipBinder.Initialize(definition, tooltipView);
		}

		public void ApplyState(Game.Research.ResearchState state)
		{
			Game.Research.ResearchStatus status = state.Status;
			bool canStart = status == Game.Research.ResearchStatus.Available;

			if (button != null)
			{
				button.interactable = canStart;
			}

			if (buttonManager != null)
			{
				buttonManager.Interactable(canStart);
			}

			Color backgroundColor = GetBackgroundColor(status);
			Color textColor = canStart || status == Game.Research.ResearchStatus.InProgress ||
							  status == Game.Research.ResearchStatus.Completed
				? normalTextColor
				: disabledTextColor;
			Color accentColor = GetAccentColor(status, textColor);

			SetGraphicColor(backgroundGraphic, backgroundColor);
			SetGraphicColor(frameGraphic, accentColor);
			SetGraphicColor(iconGraphic, accentColor);
			ApplyOptionalMaterials(status);
			SetTextColor(titleText, textColor);
			SetTextColor(costText, textColor);
			SetTextColor(statusText, textColor);

			if (statusText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(statusText, GetStatusText(state));
			}

			if (tooltipBinder != null)
			{
				tooltipBinder.ApplyState(state);
			}
		}

		private void CacheInitialMaterials()
		{
			if (backgroundGraphic == null || hasInitialBackgroundMaterial)
			{
				return;
			}

			initialBackgroundMaterial = backgroundGraphic.material;
			hasInitialBackgroundMaterial = true;
		}

		private void ApplyOptionalMaterials(Game.Research.ResearchStatus status)
		{
			if (backgroundGraphic == null)
			{
				return;
			}

			CacheInitialMaterials();

			if (status == Game.Research.ResearchStatus.InProgress &&
				inProgressBackgroundMaterial != null)
			{
				backgroundGraphic.material = inProgressBackgroundMaterial;
				return;
			}

			if (restoreInitialBackgroundMaterial && hasInitialBackgroundMaterial)
			{
				backgroundGraphic.material = initialBackgroundMaterial;
			}
		}

		private void ValidateViewReferences()
		{
			if (definition == null)
			{
				return;
			}

			if (titleText == null && buttonManager == null)
			{
				Debug.LogWarning(
					$"[{nameof(ResearchButtonView)}] '{name}' has no Title Text assigned for research '{definition.Id}'.",
					this);
			}

			if (backgroundGraphic == null)
			{
				Debug.LogWarning(
					$"[{nameof(ResearchButtonView)}] '{name}' has no Background Graphic assigned for research '{definition.Id}'.",
					this);
			}
		}

		private void RemoveListener()
		{
			if (cachedClickAction == null)
			{
				return;
			}

			if (buttonManager != null)
			{
				buttonManager.onClick.RemoveListener(cachedClickAction);
			}
			else if (button != null)
			{
				button.onClick.RemoveListener(cachedClickAction);
			}

			cachedClickAction = null;
		}

		private void HandleClicked()
		{
			if (definition != null)
			{
				clicked?.Invoke(definition.Id);
			}
		}

		private Color GetBackgroundColor(Game.Research.ResearchStatus status)
		{
			return status switch
			{
				Game.Research.ResearchStatus.Available => availableColor,
				Game.Research.ResearchStatus.InProgress => inProgressColor,
				Game.Research.ResearchStatus.Completed => completedColor,
				Game.Research.ResearchStatus.Excluded => excludedColor,
				_ => lockedColor
			};
		}

		private Color GetAccentColor(Game.Research.ResearchStatus status, Color fallbackColor)
		{
			return status == Game.Research.ResearchStatus.InProgress
				? inProgressColor
				: fallbackColor;
		}

		private static string GetStatusText(Game.Research.ResearchState state)
		{
			return state.Status switch
			{
				Game.Research.ResearchStatus.Available => "Available",
				Game.Research.ResearchStatus.InProgress => $"{state.RemainingDays} day(s) left",
				Game.Research.ResearchStatus.Completed => "Completed",
				Game.Research.ResearchStatus.Excluded => "Choice locked",
				_ => "Locked"
			};
		}

		private static void SetGraphicColor(Graphic graphic, Color color)
		{
			if (graphic != null)
			{
				graphic.color = color;
			}
		}

		private static void SetTextColor(TMP_Text text, Color color)
		{
			if (text != null)
			{
				Game.UI.RuntimeTextRefresh.SetColor(text, color);
			}
		}
	}
}
