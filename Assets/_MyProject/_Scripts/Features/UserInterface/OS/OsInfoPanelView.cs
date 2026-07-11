using System.Collections.Generic;
using TMPro;
using Michsky.MUIP;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.OS
{
	[System.Serializable]
	public sealed class OsInfoPanelView
	{
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private RectTransform contentRoot;
		[SerializeField] private GameObject rowTemplateObject;
		[SerializeField] private ButtonManager rowButtonTemplate;
		[SerializeField] private TMP_Text rowTemplate;
		[SerializeField] private string rowFormat = "{0}: {1}";
		[SerializeField] private bool hideTemplate = true;
		[SerializeField] private bool rebuildLayout = true;
		[SerializeField] private Color goodColor = new Color(0.2f, 1f, 0.42f);
		[SerializeField] private Color warningColor = new Color(1f, 0.64f, 0.1f);
		[SerializeField] private Color badColor = new Color(1f, 0.18f, 0.18f);

		private readonly List<GameObject> generatedRows = new();

		public void Render(OsInfoSection section)
		{
			if (section == null)
			{
				Clear();
				return;
			}

			if (titleText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(titleText, section.Title);
			}

			Clear();

			GameObject template = ResolveRowTemplateObject();

			if (template == null)
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

			bool titleUsesTemplate = titleText != null && template == titleText.gameObject;

			if (titleUsesTemplate)
			{
				template.SetActive(true);
			}
			else if (hideTemplate)
			{
				template.SetActive(false);
			}

			foreach (OsInfoEntry entry in section.Entries)
			{
				if (entry == null)
				{
					continue;
				}

				GameObject rowObject = Object.Instantiate(template, targetRoot);
				rowObject.name = $"OSInfo_{SanitizeName(entry.Label)}";
				RenderRow(rowObject, entry);
				rowObject.SetActive(true);

				generatedRows.Add(rowObject);
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
				GameObject row = generatedRows[i];

				if (row == null)
				{
					continue;
				}

				if (Application.isPlaying)
				{
					Object.Destroy(row);
				}
				else
				{
					Object.DestroyImmediate(row);
				}
			}

			generatedRows.Clear();
		}

		private GameObject ResolveRowTemplateObject()
		{
			if (rowTemplateObject != null)
			{
				return rowTemplateObject;
			}

			if (rowButtonTemplate != null)
			{
				return rowButtonTemplate.gameObject;
			}

			return rowTemplate != null ? rowTemplate.gameObject : null;
		}

		private RectTransform ResolveContentRoot()
		{
			if (contentRoot != null)
			{
				return contentRoot;
			}

			GameObject template = ResolveRowTemplateObject();
			return template != null
				? template.transform.parent as RectTransform
				: null;
		}

		private void RenderRow(GameObject rowObject, OsInfoEntry entry)
		{
			OsInfoEntryView entryView = rowObject.GetComponent<OsInfoEntryView>();

			if (entryView != null)
			{
				entryView.Render(
					entry,
					FormatPlainRow(entry),
					GetToneColor(entry.Tone),
					entry.Tone != OsInfoTone.Neutral
				);
				return;
			}

			ButtonManager buttonManager = rowObject.GetComponent<ButtonManager>();

			if (buttonManager != null)
			{
				RenderButtonManager(buttonManager, entry);
				return;
			}

			TMP_Text text = rowObject.GetComponent<TMP_Text>() ??
							rowObject.GetComponentInChildren<TMP_Text>(true);

			if (text == null)
			{
				Debug.LogWarning($"[{nameof(OsInfoPanelView)}] Row template has no supported text view.");
				return;
			}

			text.richText = true;
			Game.UI.RuntimeTextRefresh.SetText(text, FormatRichRow(entry));
		}

		private void RenderButtonManager(ButtonManager buttonManager, OsInfoEntry entry)
		{
			UIManagerButton uiManagerButton = buttonManager.GetComponent<UIManagerButton>();

			if (uiManagerButton != null)
			{
				uiManagerButton.overrideColors = true;
			}

			buttonManager.enableText = true;
			Game.UI.RuntimeTextRefresh.SetButtonText(buttonManager, FormatPlainRow(entry));

			if (entry.Tone == OsInfoTone.Neutral)
			{
				return;
			}

			Color toneColor = GetToneColor(entry.Tone);
			SetTextColor(buttonManager.normalText, toneColor);
			SetTextColor(buttonManager.highlightedText, toneColor);
			SetTextColor(buttonManager.disabledText, toneColor);
		}

		private static void SetTextColor(TMP_Text text, Color color)
		{
			if (text != null)
			{
				Game.UI.RuntimeTextRefresh.SetColor(text, color);
			}
		}

		private string FormatPlainRow(OsInfoEntry entry)
		{
			string format = string.IsNullOrWhiteSpace(rowFormat)
				? "{0}: {1}"
				: rowFormat;

			return string.Format(format, entry.Label, entry.Value);
		}

		private string FormatRichRow(OsInfoEntry entry)
		{
			string text = FormatPlainRow(entry);
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
