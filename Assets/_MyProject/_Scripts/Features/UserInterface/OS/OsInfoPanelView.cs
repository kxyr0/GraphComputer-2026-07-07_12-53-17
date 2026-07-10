using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.OS
{
	[System.Serializable]
	public sealed class OsInfoPanelView
	{
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private RectTransform contentRoot;
		[SerializeField] private TMP_Text rowTemplate;
		[SerializeField] private string rowFormat = "{0}: {1}";
		[SerializeField] private bool hideTemplate = true;
		[SerializeField] private bool rebuildLayout = true;
		[SerializeField] private Color goodColor = new Color(0.2f, 1f, 0.42f);
		[SerializeField] private Color warningColor = new Color(1f, 0.64f, 0.1f);
		[SerializeField] private Color badColor = new Color(1f, 0.18f, 0.18f);

		private readonly List<TMP_Text> generatedRows = new();

		public void Render(OsInfoSection section)
		{
			if (section == null)
			{
				Clear();
				return;
			}

			if (titleText != null)
			{
				titleText.text = section.Title;
			}

			Clear();

			if (rowTemplate == null)
			{
				Debug.LogWarning($"[{nameof(OsInfoPanelView)}] Row template is missing.");
				return;
			}

			RectTransform targetRoot = ResolveContentRoot();

			if (targetRoot == null)
			{
				Debug.LogWarning($"[{nameof(OsInfoPanelView)}] Content root is missing.");
				return;
			}

			bool titleUsesTemplate = titleText != null && rowTemplate == titleText;

			if (titleUsesTemplate)
			{
				rowTemplate.gameObject.SetActive(true);
			}
			else if (hideTemplate)
			{
				rowTemplate.gameObject.SetActive(false);
			}

			foreach (OsInfoEntry entry in section.Entries)
			{
				if (entry == null)
				{
					continue;
				}

				TMP_Text row = Object.Instantiate(rowTemplate, targetRoot);
				row.name = $"OSInfo_{SanitizeName(entry.Label)}";
				row.text = FormatRow(entry);
				row.richText = true;
				row.gameObject.SetActive(true);

				generatedRows.Add(row);
			}

			if (rebuildLayout)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(targetRoot);
			}
		}

		public void Clear()
		{
			for (int i = generatedRows.Count - 1; i >= 0; i--)
			{
				TMP_Text row = generatedRows[i];

				if (row == null)
				{
					continue;
				}

				if (Application.isPlaying)
				{
					Object.Destroy(row.gameObject);
				}
				else
				{
					Object.DestroyImmediate(row.gameObject);
				}
			}

			generatedRows.Clear();
		}

		private RectTransform ResolveContentRoot()
		{
			if (contentRoot != null)
			{
				return contentRoot;
			}

			return rowTemplate != null
				? rowTemplate.transform.parent as RectTransform
				: null;
		}

		private string FormatRow(OsInfoEntry entry)
		{
			string format = string.IsNullOrWhiteSpace(rowFormat)
				? "{0}: {1}"
				: rowFormat;

			string text = string.Format(format, entry.Label, entry.Value);

			return entry.Tone == OsInfoTone.Neutral
				? text
				: $"<color=#{ColorUtility.ToHtmlStringRGB(GetToneColor(entry.Tone))}>{text}</color>";
		}

		private Color GetToneColor(OsInfoTone tone)
		{
			return tone switch
			{
				OsInfoTone.Good => goodColor,
				OsInfoTone.Warning => warningColor,
				OsInfoTone.Bad => badColor,
				_ => Color.white
			};
		}

		private static string SanitizeName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "Row";
			}

			return value.Replace(" ", string.Empty).Replace(":", string.Empty);
		}
	}
}
