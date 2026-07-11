using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Game.Nodes.Presentation
{
	public sealed class NodeStatRowView : MonoBehaviour
	{
		private enum StatVisualState
		{
			Neutral,
			Bad,
			Warning,
			Good
		}

		[Header("Single Text Mode")]
		[SerializeField] private TMP_Text rowText;

		[SerializeField, Min(1)]
		private int labelColumnWidth = 18;

		[SerializeField]
		private string separator = " ";

		[Header("Two Text Mode")]
		[SerializeField] private TMP_Text labelText;
		[SerializeField] private TMP_Text valueText;

		[Header("State Colors")]
		[SerializeField] private Color badColor = new Color(1f, 0.25f, 0.25f, 1f);
		[SerializeField] private Color warningColor = new Color(1f, 0.63f, 0.12f, 1f);
		[SerializeField] private Color goodColor = new Color(0.25f, 1f, 0.45f, 1f);

		private string currentLabel;
		private string currentValue;
		private string currentStateHint;
		private bool hasDefaultColors;
		private Color defaultRowTextColor;
		private Color defaultLabelTextColor;
		private Color defaultValueTextColor;

		public string Label => currentLabel;
		public string Value => currentValue;

		private void Awake()
		{
			CaptureDefaultColors();
		}

		public void Bind(string label, string value)
		{
			currentLabel = label ?? string.Empty;
			currentValue = value ?? string.Empty;
			currentStateHint = string.Empty;

			name = $"Stat_{currentLabel}";

			Apply();
		}

		public void SetValue(string value)
		{
			currentValue = value ?? string.Empty;
			currentStateHint = string.Empty;
			Apply();
		}

		public void SetValue(string value, string stateHint)
		{
			currentValue = value ?? string.Empty;
			currentStateHint = stateHint ?? string.Empty;
			Apply();
		}

		public void SetLabelAndValue(string label, string value)
		{
			currentLabel = label ?? string.Empty;
			currentValue = value ?? string.Empty;
			currentStateHint = string.Empty;
			Apply();
		}

		private void Apply()
		{
			CaptureDefaultColors();

			StatVisualState visualState =
				ResolveVisualState(currentLabel, currentValue, currentStateHint);

			if (rowText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(rowText, BuildSingleLine(currentLabel, currentValue));
				Game.UI.RuntimeTextRefresh.SetColor(rowText, GetStateColor(visualState, defaultRowTextColor));
				return;
			}

			if (labelText != null && labelText == valueText)
			{
				Game.UI.RuntimeTextRefresh.SetText(labelText, BuildSingleLine(currentLabel, currentValue));
				Game.UI.RuntimeTextRefresh.SetColor(labelText, GetStateColor(visualState, defaultLabelTextColor));
				return;
			}

			if (labelText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(labelText, currentLabel);
				Game.UI.RuntimeTextRefresh.SetColor(labelText, defaultLabelTextColor);
			}

			if (valueText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(valueText, currentValue);
				Game.UI.RuntimeTextRefresh.SetColor(valueText, GetStateColor(visualState, defaultValueTextColor));
			}
		}

		private string BuildSingleLine(string label, string value)
		{
			return label.PadRight(labelColumnWidth) + separator + value;
		}

		private void CaptureDefaultColors()
		{
			if (hasDefaultColors)
			{
				return;
			}

			defaultRowTextColor = rowText != null ? rowText.color : Color.white;
			defaultLabelTextColor = labelText != null ? labelText.color : Color.white;
			defaultValueTextColor = valueText != null ? valueText.color : defaultLabelTextColor;
			hasDefaultColors = true;
		}

		private Color GetStateColor(StatVisualState state, Color fallback)
		{
			return state switch
			{
				StatVisualState.Bad => badColor,
				StatVisualState.Warning => warningColor,
				StatVisualState.Good => goodColor,
				_ => fallback
			};
		}

		private static StatVisualState ResolveVisualState(
			string label,
			string value,
			string stateHint)
		{
			string normalizedLabel = Normalize(label);
			string normalizedValue = Normalize(value);
			string normalizedStateHint = Normalize(stateHint);

			StatVisualState hintState = ResolveTextState(normalizedLabel, normalizedStateHint);

			if (hintState != StatVisualState.Neutral)
			{
				return hintState;
			}

			StatVisualState textState = ResolveTextState(normalizedLabel, normalizedValue);

			if (textState != StatVisualState.Neutral)
			{
				return textState;
			}

			if (!TryReadFirstNumber(value, out float number))
			{
				return StatVisualState.Neutral;
			}

			if (IsLowerBetterMetric(normalizedLabel, normalizedValue))
			{
				return ResolveLowerBetterState(normalizedLabel, normalizedValue, number);
			}

			if (IsHigherBetterMetric(normalizedLabel, normalizedValue))
			{
				return ResolveHigherBetterState(normalizedLabel, normalizedValue, number);
			}

			return StatVisualState.Neutral;
		}

		private static StatVisualState ResolveTextState(string normalizedLabel, string normalizedValue)
		{
			if (string.IsNullOrWhiteSpace(normalizedValue))
			{
				return StatVisualState.Neutral;
			}

			if (ContainsAny(
					normalizedValue,
					"missing",
					"offline",
					"unknown",
					"none",
					"no service",
					"no signal",
					"failed",
					"failure",
					"error",
					"critical",
					"obsolete",
					"poor",
					"slow",
					"angry"))
			{
				return StatVisualState.Bad;
			}

			if (ContainsAny(
					normalizedValue,
					"mixed",
					"concerned",
					"guarded",
					"elevated",
					"aging",
					"lagging",
					"warning",
					"basic"))
			{
				return StatVisualState.Warning;
			}

			if (ContainsAny(
					normalizedValue,
					"excellent",
					"good",
					"stable",
					"active",
					"enabled",
					"online",
					"current",
					"ok"))
			{
				return StatVisualState.Good;
			}

			if (normalizedValue == "low" && normalizedLabel.Contains("threat"))
			{
				return StatVisualState.Good;
			}

			return StatVisualState.Neutral;
		}

		private static StatVisualState ResolveLowerBetterState(
			string normalizedLabel,
			string normalizedValue,
			float number)
		{
			if (ContainsAny(normalizedLabel, "vulnerabilities", "critical", "reports"))
			{
				if (number <= 0f)
				{
					return StatVisualState.Good;
				}

				return number <= 3f
					? StatVisualState.Warning
					: StatVisualState.Bad;
			}

			if (ContainsAny(normalizedLabel, "delay", "response", "latency")
				|| normalizedValue.Contains("ms"))
			{
				if (number <= 80f)
				{
					return StatVisualState.Good;
				}

				return number <= 180f
					? StatVisualState.Warning
					: StatVisualState.Bad;
			}

			if (ContainsAny(normalizedLabel, "size")
				|| normalizedValue.EndsWith("mb", StringComparison.Ordinal))
			{
				if (number <= 64f)
				{
					return StatVisualState.Good;
				}

				return number <= 256f
					? StatVisualState.Warning
					: StatVisualState.Bad;
			}

			if (number <= 35f)
			{
				return StatVisualState.Good;
			}

			return number <= 70f
				? StatVisualState.Warning
				: StatVisualState.Bad;
		}

		private static StatVisualState ResolveHigherBetterState(
			string normalizedLabel,
			string normalizedValue,
			float number)
		{
			if (ContainsAny(normalizedLabel, "speed")
				|| normalizedValue.Contains("mbps")
				|| normalizedValue.Contains("mb/s"))
			{
				if (number <= 0f)
				{
					return StatVisualState.Bad;
				}

				return number < 1f
					? StatVisualState.Warning
					: StatVisualState.Good;
			}

			if (normalizedValue.Contains("%"))
			{
				if (number >= 70f)
				{
					return StatVisualState.Good;
				}

				return number >= 35f
					? StatVisualState.Warning
					: StatVisualState.Bad;
			}

			if (ContainsAny(normalizedLabel, "users", "limit"))
			{
				if (number <= 0f)
				{
					return StatVisualState.Bad;
				}

				return number < 10f
					? StatVisualState.Warning
					: StatVisualState.Good;
			}

			return StatVisualState.Neutral;
		}

		private static bool IsLowerBetterMetric(string normalizedLabel, string normalizedValue)
		{
			return ContainsAny(
					normalizedLabel,
					"risk",
					"load",
					"usage",
					"used",
					"delay",
					"latency",
					"response",
					"vulnerabilities",
					"critical",
					"threat",
					"size")
				|| normalizedValue.Contains("ms");
		}

		private static bool IsHigherBetterMetric(string normalizedLabel, string normalizedValue)
		{
			return ContainsAny(
					normalizedLabel,
					"stability",
					"safety",
					"support",
					"encryption",
					"protection",
					"power control",
					"speed",
					"users",
					"limit")
				|| normalizedValue.Contains("%")
				|| normalizedValue.Contains("mbps")
				|| normalizedValue.Contains("mb/s");
		}

		private static bool TryReadFirstNumber(string value, out float number)
		{
			number = 0f;

			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			int start = -1;
			int length = 0;

			for (int i = 0; i < value.Length; i++)
			{
				char character = value[i];
				bool isNumberCharacter = char.IsDigit(character)
					|| character == '-'
					|| character == '+'
					|| character == '.'
					|| character == ',';

				if (!isNumberCharacter)
				{
					if (start >= 0)
					{
						break;
					}

					continue;
				}

				if (start < 0)
				{
					start = i;
				}

				length++;
			}

			if (start < 0 || length <= 0)
			{
				return false;
			}

			string numericText = value.Substring(start, length).Replace(',', '.');
			return float.TryParse(
				numericText,
				NumberStyles.Float,
				CultureInfo.InvariantCulture,
				out number);
		}

		private static bool ContainsAny(string source, params string[] values)
		{
			if (string.IsNullOrEmpty(source))
			{
				return false;
			}

			foreach (string value in values)
			{
				if (source.Contains(value))
				{
					return true;
				}
			}

			return false;
		}

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value)
				? string.Empty
				: value.Trim().ToLowerInvariant();
		}
	}
}
