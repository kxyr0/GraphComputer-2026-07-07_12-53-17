using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeInputCodeEditor : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private TMP_Text tabTitleText;
		[SerializeField] private TMP_InputField inputField;
		[SerializeField] private TMP_Text highlightText;
		[SerializeField] private TMP_Text lineNumbersText;
		[SerializeField] private TMP_Text statusText;

		[Header("Content")]
		[SerializeField] private string tabTitle = "System Core";

		[SerializeField, TextArea(10, 40)]
		private string initialCode =
@"module SystemCore

stability += 12
network += data.output
security += firewall.level

if load > 80
    alert ""High Load""
end";

		[Header("Editing")]
		[SerializeField] private bool insertSpacesInsteadOfTabs = true;
		[SerializeField, Range(1, 8)] private int spacesPerTab = 4;
		[SerializeField] private bool enableTabIndent = true;

		[Header("View")]
		[SerializeField] private bool useSyntaxHighlight = true;
		[SerializeField, Range(0f, 1f)] private float rawInputTextAlpha = 0.02f;
		[SerializeField] private bool syncFontSettings = true;

		private RectTransform inputTextRect;
		private RectTransform highlightRect;
		private RectTransform lineNumbersRect;

		private static readonly HashSet<string> Keywords =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"module",
				"if",
				"else",
				"end",
				"while",
				"for",
				"return",
				"import",
				"link",
				"from",
				"to"
			};

		private static readonly HashSet<string> Builtins =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"alert",
				"log",
				"enable",
				"disable",
				"emit",
				"listen",
				"connect"
			};

		private static readonly HashSet<string> KnownSymbols =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"stability",
				"network",
				"security",
				"load",
				"data",
				"firewall",
				"kernel",
				"memory",
				"storage",
				"control",
				"system",
				"output",
				"input",
				"level",
				"core"
			};

		private void Awake()
		{
			ResolveReferences();
			ConfigureInputField();

			SetTitle(tabTitle);
			SetCode(initialCode);
		}

		private void OnEnable()
		{
			if (inputField != null)
			{
				inputField.onValueChanged.AddListener(HandleInputChanged);
			}
		}

		private void OnDisable()
		{
			if (inputField != null)
			{
				inputField.onValueChanged.RemoveListener(HandleInputChanged);
			}
		}

		private void Update()
		{
			HandleTabIndent();
			UpdateStatus();
		}

		private void LateUpdate()
		{
			SyncScrollPosition();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (!Application.isPlaying)
			{
				ResolveReferences();
				RefreshView();
			}
		}
#endif

		public void SetTitle(string title)
		{
			tabTitle = title ?? string.Empty;

			if (tabTitleText != null)
			{
				tabTitleText.text = tabTitle;
			}
		}

		public void SetCode(string code)
		{
			if (inputField == null)
			{
				initialCode = code ?? string.Empty;
				return;
			}

			inputField.SetTextWithoutNotify(code ?? string.Empty);
			RefreshView();
		}

		public string GetCode()
		{
			return inputField != null
				? inputField.text
				: initialCode;
		}

		private void ResolveReferences()
		{
			if (inputField == null)
			{
				inputField = GetComponentInChildren<TMP_InputField>(true);
			}

			if (inputField != null && inputField.textComponent != null)
			{
				inputTextRect = inputField.textComponent.rectTransform;
			}

			if (highlightText != null)
			{
				highlightRect = highlightText.rectTransform;
			}

			if (lineNumbersText != null)
			{
				lineNumbersRect = lineNumbersText.rectTransform;
			}
		}

		private void ConfigureInputField()
		{
			if (inputField == null)
			{
				return;
			}

			inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
			inputField.contentType = TMP_InputField.ContentType.Standard;

			if (inputField.textComponent != null)
			{
				inputField.textComponent.richText = false;
				inputField.textComponent.enableWordWrapping = false;
				inputField.textComponent.overflowMode = TextOverflowModes.Overflow;

				Color rawColor = inputField.textComponent.color;
				rawColor.a = rawInputTextAlpha;
				inputField.textComponent.color = rawColor;
			}

			if (highlightText != null)
			{
				highlightText.richText = true;
				highlightText.raycastTarget = false;
				highlightText.enableWordWrapping = false;
				highlightText.overflowMode = TextOverflowModes.Overflow;
			}

			if (lineNumbersText != null)
			{
				lineNumbersText.raycastTarget = false;
				lineNumbersText.enableWordWrapping = false;
				lineNumbersText.overflowMode = TextOverflowModes.Overflow;
			}

			if (syncFontSettings)
			{
				SyncTextSettings();
			}
		}

		private void SyncTextSettings()
		{
			if (inputField == null || inputField.textComponent == null)
			{
				return;
			}

			TMP_Text source = inputField.textComponent;

			if (highlightText != null)
			{
				highlightText.font = source.font;
				highlightText.fontSize = source.fontSize;
				highlightText.fontStyle = source.fontStyle;
				highlightText.alignment = source.alignment;
				highlightText.margin = source.margin;
			}

			if (lineNumbersText != null)
			{
				lineNumbersText.font = source.font;
				lineNumbersText.fontSize = source.fontSize;
				lineNumbersText.fontStyle = source.fontStyle;
			}
		}

		private void HandleInputChanged(string value)
		{
			RefreshView();
		}

		private void RefreshView()
		{
			string code = inputField != null
				? inputField.text
				: initialCode;

			if (tabTitleText != null)
			{
				tabTitleText.text = tabTitle;
			}

			if (lineNumbersText != null)
			{
				lineNumbersText.text = BuildLineNumbers(code);
			}

			if (highlightText != null)
			{
				highlightText.text = useSyntaxHighlight
					? HighlightCode(code)
					: EscapeRichText(code);
			}

			UpdateStatus();
			SyncScrollPosition();
		}

		private void HandleTabIndent()
		{
			if (!enableTabIndent || inputField == null || !inputField.isFocused)
			{
				return;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame)
			{
				return;
			}

			InsertIndent();
		}

		private void InsertIndent()
		{
			string indent = insertSpacesInsteadOfTabs
				? new string(' ', Mathf.Max(1, spacesPerTab))
				: "\t";

			string text = inputField.text ?? string.Empty;

			int anchor = Mathf.Clamp(inputField.selectionAnchorPosition, 0, text.Length);
			int focus = Mathf.Clamp(inputField.selectionFocusPosition, 0, text.Length);

			int start = Mathf.Min(anchor, focus);
			int end = Mathf.Max(anchor, focus);

			string newText =
				text.Substring(0, start) +
				indent +
				text.Substring(end);

			int newCaretPosition = start + indent.Length;

			inputField.SetTextWithoutNotify(newText);
			inputField.caretPosition = newCaretPosition;
			inputField.selectionAnchorPosition = newCaretPosition;
			inputField.selectionFocusPosition = newCaretPosition;

			inputField.ForceLabelUpdate();

			RefreshView();
		}

		private void SyncScrollPosition()
		{
			if (inputTextRect == null)
			{
				return;
			}

			if (highlightRect != null)
			{
				highlightRect.anchorMin = inputTextRect.anchorMin;
				highlightRect.anchorMax = inputTextRect.anchorMax;
				highlightRect.pivot = inputTextRect.pivot;
				highlightRect.sizeDelta = inputTextRect.sizeDelta;
				highlightRect.anchoredPosition = inputTextRect.anchoredPosition;
			}

			if (lineNumbersRect != null)
			{
				Vector2 position = lineNumbersRect.anchoredPosition;
				position.y = inputTextRect.anchoredPosition.y;
				lineNumbersRect.anchoredPosition = position;
			}
		}

		private void UpdateStatus()
		{
			if (statusText == null || inputField == null)
			{
				return;
			}

			string text = inputField.text ?? string.Empty;
			int caret = Mathf.Clamp(inputField.caretPosition, 0, text.Length);

			GetLineAndColumn(text, caret, out int line, out int column);

			int lineCount = CountLines(text);

			statusText.text =
				$"Ln {line}, Col {column}    Lines {lineCount}    Chars {text.Length}";
		}

		private static string BuildLineNumbers(string source)
		{
			int lineCount = CountLines(source);
			int width = lineCount.ToString().Length;

			StringBuilder builder = new StringBuilder();

			for (int i = 1; i <= lineCount; i++)
			{
				builder.Append(i.ToString().PadLeft(width));

				if (i < lineCount)
				{
					builder.Append('\n');
				}
			}

			return builder.ToString();
		}

		private static int CountLines(string source)
		{
			if (string.IsNullOrEmpty(source))
			{
				return 1;
			}

			int count = 1;

			for (int i = 0; i < source.Length; i++)
			{
				if (source[i] == '\n')
				{
					count++;
				}
			}

			return count;
		}

		private static void GetLineAndColumn(
			string source,
			int index,
			out int line,
			out int column)
		{
			line = 1;
			column = 1;

			if (string.IsNullOrEmpty(source))
			{
				return;
			}

			index = Mathf.Clamp(index, 0, source.Length);

			for (int i = 0; i < index; i++)
			{
				if (source[i] == '\n')
				{
					line++;
					column = 1;
				}
				else
				{
					column++;
				}
			}
		}

		private static string HighlightCode(string source)
		{
			if (string.IsNullOrEmpty(source))
			{
				return string.Empty;
			}

			source = source.Replace("\r\n", "\n").Replace('\r', '\n');

			string[] lines = source.Split('\n');
			StringBuilder builder = new StringBuilder();

			for (int i = 0; i < lines.Length; i++)
			{
				builder.Append(HighlightLine(lines[i]));

				if (i < lines.Length - 1)
				{
					builder.Append('\n');
				}
			}

			return builder.ToString();
		}

		private static string HighlightLine(string line)
		{
			if (string.IsNullOrEmpty(line))
			{
				return string.Empty;
			}

			int commentIndex = FindCommentIndex(line);

			string codePart = commentIndex >= 0
				? line.Substring(0, commentIndex)
				: line;

			string commentPart = commentIndex >= 0
				? line.Substring(commentIndex)
				: string.Empty;

			StringBuilder builder = new StringBuilder();
			AppendHighlightedCodePart(codePart, builder);

			if (!string.IsNullOrEmpty(commentPart))
			{
				builder.Append("<color=#6A9955>");
				builder.Append(EscapeRichText(commentPart));
				builder.Append("</color>");
			}

			return builder.ToString();
		}

		private static int FindCommentIndex(string line)
		{
			bool insideString = false;

			for (int i = 0; i < line.Length - 1; i++)
			{
				char current = line[i];

				if (current == '"')
				{
					insideString = !insideString;
					continue;
				}

				if (!insideString && current == '/' && line[i + 1] == '/')
				{
					return i;
				}
			}

			return -1;
		}

		private static void AppendHighlightedCodePart(string source, StringBuilder builder)
		{
			int index = 0;

			while (index < source.Length)
			{
				char current = source[index];

				if (current == '"')
				{
					int end = FindStringEnd(source, index);

					string stringToken = source.Substring(index, end - index + 1);

					builder.Append("<color=#CE9178>");
					builder.Append(EscapeRichText(stringToken));
					builder.Append("</color>");

					index = end + 1;
					continue;
				}

				if (char.IsDigit(current))
				{
					int end = index;

					while (end < source.Length &&
						   (char.IsDigit(source[end]) || source[end] == '.'))
					{
						end++;
					}

					string numberToken = source.Substring(index, end - index);

					builder.Append("<color=#B5CEA8>");
					builder.Append(EscapeRichText(numberToken));
					builder.Append("</color>");

					index = end;
					continue;
				}

				if (char.IsLetter(current) || current == '_')
				{
					int end = index;

					while (end < source.Length &&
						   (char.IsLetterOrDigit(source[end]) || source[end] == '_'))
					{
						end++;
					}

					string word = source.Substring(index, end - index);
					AppendHighlightedWord(word, builder);

					index = end;
					continue;
				}

				if (IsOperator(current))
				{
					builder.Append("<color=#D4D4D4>");
					builder.Append(EscapeRichText(current.ToString()));
					builder.Append("</color>");
				}
				else
				{
					builder.Append(EscapeRichText(current.ToString()));
				}

				index++;
			}
		}

		private static int FindStringEnd(string source, int startIndex)
		{
			for (int i = startIndex + 1; i < source.Length; i++)
			{
				if (source[i] == '"' && source[i - 1] != '\\')
				{
					return i;
				}
			}

			return source.Length - 1;
		}

		private static void AppendHighlightedWord(string word, StringBuilder builder)
		{
			if (Keywords.Contains(word))
			{
				builder.Append("<color=#C586C0>");
				builder.Append(EscapeRichText(word));
				builder.Append("</color>");
				return;
			}

			if (Builtins.Contains(word))
			{
				builder.Append("<color=#DCDCAA>");
				builder.Append(EscapeRichText(word));
				builder.Append("</color>");
				return;
			}

			if (KnownSymbols.Contains(word))
			{
				builder.Append("<color=#9CDCFE>");
				builder.Append(EscapeRichText(word));
				builder.Append("</color>");
				return;
			}

			builder.Append("<color=#D4D4D4>");
			builder.Append(EscapeRichText(word));
			builder.Append("</color>");
		}

		private static bool IsOperator(char value)
		{
			return value is '+' or '-' or '*' or '/' or '=' or '>' or '<' or '.' or ':' or ';';
		}

		private static string EscapeRichText(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			return value
				.Replace("&", "&amp;")
				.Replace("<", "&lt;")
				.Replace(">", "&gt;");
		}
	}
}