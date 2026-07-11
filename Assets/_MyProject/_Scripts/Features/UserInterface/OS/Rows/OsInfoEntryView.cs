using TMPro;
using Michsky.MUIP;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.OS
{
	[DisallowMultipleComponent]
	public sealed class OsInfoEntryView : MonoBehaviour
	{
		[Header("Text")]
		[SerializeField] private TMP_Text labelText;
		[SerializeField] private TMP_Text valueText;
		[SerializeField] private TMP_Text combinedText;
		[SerializeField] private Text legacyLabelText;
		[SerializeField] private Text legacyValueText;
		[SerializeField] private Text legacyCombinedText;

		[Header("Format")]
		[SerializeField] private string combinedFormat = "{0}: {1}";

		[Header("Tone")]
		[SerializeField] private bool tintLabel = true;
		[SerializeField] private bool tintValue = true;
		[SerializeField] private bool tintCombined = true;
		[SerializeField] private Graphic[] toneGraphics;

		[Header("Button Manager")]
		[SerializeField] private ButtonManager buttonManager;
		[SerializeField] private UIManagerButton uiManagerButton;
		[Tooltip("Enabled automatically when this row template has a Modern UI Pack ButtonManager.")]
		[SerializeField] private bool useButtonManagerText;
		[SerializeField] private bool forceButtonColorOverride = true;

		private void Reset()
		{
			TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

			if (texts.Length == 1)
			{
				combinedText = texts[0];
			}
			else if (texts.Length >= 2)
			{
				labelText = texts[0];
				valueText = texts[1];
			}

			buttonManager = GetComponent<ButtonManager>();
			uiManagerButton = GetComponent<UIManagerButton>();
			useButtonManagerText = buttonManager != null;
		}

		public void Render(
			OsInfoEntry entry,
			string fallbackText,
			Color toneColor,
			bool useToneColor)
		{
			if (entry == null)
			{
				return;
			}

			SetText(labelText, legacyLabelText, entry.Label);
			SetText(valueText, legacyValueText, entry.Value);

			string combined = FormatCombined(entry, fallbackText);
			RenderButtonManager(combined, toneColor, useToneColor);
			SetText(combinedText, legacyCombinedText, combined);

			if (useToneColor)
			{
				ApplyTone(toneColor);
			}
		}

		private string FormatCombined(OsInfoEntry entry, string fallbackText)
		{
			if (!string.IsNullOrWhiteSpace(fallbackText))
			{
				return fallbackText;
			}

			string format = string.IsNullOrWhiteSpace(combinedFormat)
				? "{0}: {1}"
				: combinedFormat;

			return string.Format(format, entry.Label, entry.Value);
		}

		private void ApplyTone(Color toneColor)
		{
			if (tintLabel)
			{
				SetColor(labelText, legacyLabelText, toneColor);
			}

			if (tintValue)
			{
				SetColor(valueText, legacyValueText, toneColor);
			}

			if (tintCombined)
			{
				SetColor(combinedText, legacyCombinedText, toneColor);
			}

			if (toneGraphics == null)
			{
				return;
			}

			foreach (Graphic graphic in toneGraphics)
			{
				if (graphic != null)
				{
					graphic.color = toneColor;
				}
			}
		}

		private void RenderButtonManager(string text, Color toneColor, bool useToneColor)
		{
			if (buttonManager == null)
			{
				buttonManager = GetComponent<ButtonManager>();
			}

			if (buttonManager == null)
			{
				return;
			}

			useButtonManagerText = true;

			if (uiManagerButton == null)
			{
				uiManagerButton = GetComponent<UIManagerButton>();
			}

			if (forceButtonColorOverride && uiManagerButton != null)
			{
				uiManagerButton.overrideColors = true;
			}

			buttonManager.enableText = true;
			Game.UI.RuntimeTextRefresh.SetButtonText(buttonManager, text);

			if (useToneColor)
			{
				SetButtonTextColor(buttonManager, toneColor);
			}
		}

		private static void SetButtonTextColor(ButtonManager manager, Color color)
		{
			SetTextColor(manager.normalText, color);
			SetTextColor(manager.highlightedText, color);
			SetTextColor(manager.disabledText, color);
		}

		private static void SetTextColor(TMP_Text text, Color color)
		{
			if (text != null)
			{
				Game.UI.RuntimeTextRefresh.SetColor(text, color);
			}
		}

		private static void SetText(TMP_Text tmpText, Text legacyText, string value)
		{
			if (tmpText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(tmpText, value);
			}

			if (legacyText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(legacyText, value);
			}
		}

		private static void SetColor(TMP_Text tmpText, Text legacyText, Color color)
		{
			if (tmpText != null)
			{
				Game.UI.RuntimeTextRefresh.SetColor(tmpText, color);
			}

			if (legacyText != null)
			{
				Game.UI.RuntimeTextRefresh.SetColor(legacyText, color);
			}
		}
	}
}
