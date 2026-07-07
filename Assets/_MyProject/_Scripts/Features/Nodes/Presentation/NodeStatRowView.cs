using TMPro;
using UnityEngine;

namespace Game.Nodes.Presentation
{
	public sealed class NodeStatRowView : MonoBehaviour
	{
		[Header("Single Text Mode")]
		[SerializeField] private TMP_Text rowText;

		[SerializeField, Min(1)]
		private int labelColumnWidth = 18;

		[SerializeField]
		private string separator = " ";

		[Header("Two Text Mode")]
		[SerializeField] private TMP_Text labelText;
		[SerializeField] private TMP_Text valueText;

		private string currentLabel;
		private string currentValue;

		public string Label => currentLabel;
		public string Value => currentValue;

		public void Bind(string label, string value)
		{
			currentLabel = label ?? string.Empty;
			currentValue = value ?? string.Empty;

			name = $"Stat_{currentLabel}";

			Apply();
		}

		public void SetValue(string value)
		{
			currentValue = value ?? string.Empty;
			Apply();
		}

		public void SetLabelAndValue(string label, string value)
		{
			currentLabel = label ?? string.Empty;
			currentValue = value ?? string.Empty;
			Apply();
		}

		private void Apply()
		{
			if (rowText != null)
			{
				rowText.text = BuildSingleLine(currentLabel, currentValue);
				return;
			}

			if (labelText != null && labelText == valueText)
			{
				labelText.text = BuildSingleLine(currentLabel, currentValue);
				return;
			}

			if (labelText != null)
			{
				labelText.text = currentLabel;
			}

			if (valueText != null)
			{
				valueText.text = currentValue;
			}
		}

		private string BuildSingleLine(string label, string value)
		{
			return label.PadRight(labelColumnWidth) + separator + value;
		}
	}
}