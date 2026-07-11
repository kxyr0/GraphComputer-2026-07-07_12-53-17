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

platform = PC

require Kernel as kernel
require Data as data
optional Security as security
optional Control as control

budget {
    cpu <= 72
    memory <= 68
    storage <= 80
}

boot {
    start kernel.scheduler
    mount data.fileSystem
    scan security.firewall
}

tick every 1s {
    monitor kernel.load
    cache data.index

    if kernel.load > 80
        throttle data.index by 25
        alert ""High Load""
    end
}

export {
    stability = 100 - kernel.load * 0.25
    load = kernel.load
}";

		[Header("Editing")]
		[SerializeField] private bool insertSpacesInsteadOfTabs = true;
		[SerializeField, Range(1, 8)] private int spacesPerTab = 4;
		[SerializeField] private bool enableTabIndent = true;

		[Header("Undo / Redo")]
		[SerializeField] private bool enableUndoRedo = true;
		[SerializeField, Min(1)] private int maxUndoSteps = 128;

		[Header("View")]
		[SerializeField] private bool useSyntaxHighlight = true;
		[SerializeField, Range(0f, 1f)] private float rawInputTextAlpha = 0.02f;
		[SerializeField] private bool syncFontSettings = true;

		private RectTransform inputTextRect;
		private RectTransform highlightRect;
		private RectTransform lineNumbersRect;
		private readonly List<TextEditSnapshot> undoHistory = new();
		private readonly List<TextEditSnapshot> redoHistory = new();
		private TextEditSnapshot lastTrackedSnapshot;
		private bool hasTrackedSnapshot;
		private bool isApplyingHistorySnapshot;

		private static readonly HashSet<string> Keywords =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"module",
				"require",
				"optional",
				"mode",
				"platform",
				"if",
				"else",
				"end",
				"budget",
				"export",
				"boot",
				"scheduler",
				"tick",
				"while",
				"for",
				"return",
				"import",
				"link",
				"as",
				"every",
				"by",
				"from",
				"to"
			};

		private static readonly HashSet<string> Builtins =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"alert",
				"warning",
				"start",
				"mount",
				"load",
				"scan",
				"rule",
				"allow",
				"block",
				"protect",
				"encrypt",
				"backup",
				"cache",
				"index",
				"watch",
				"monitor",
				"route",
				"recover",
				"schedule",
				"bind",
				"throttle",
				"flush",
				"reduce",
				"speed",
				"gameSpeed",
				"timeSpeed",
				"set",
				"run",
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
				"SystemCore",
				"Kernel",
				"Data",
				"Security",
				"Control",
				"Time",
				"Firewall",
				"PC",
				"cpu",
				"fileSystem",
				"scheduler",
				"processes",
				"processQueue",
				"incomingPackets",
				"packets",
				"trustedApps",
				"unknownPorts",
				"threats",
				"inputDelay",
				"storageUsage",
				"systemLoad",
				"ioLoad",
				"system",
				"output",
				"input",
				"level",
				"core",
				"strict",
				"balanced",
				"permissive",
				"aggressive",
				"safe",
				"performance"
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
			if (HandleUndoRedo())
			{
				return;
			}

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
				Game.UI.RuntimeTextRefresh.SetText(tabTitleText, tabTitle);
			}
		}

		public void SetCode(string code)
		{
			if (inputField == null)
			{
				initialCode = code ?? string.Empty;
				return;
			}

			Game.UI.RuntimeTextRefresh.SetTextWithoutNotify(inputField, code);
			ResetUndoRedoHistory();
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
			if (!isApplyingHistorySnapshot)
			{
				TrackInputChange(value);
			}

			RefreshView();
		}

		private bool HandleUndoRedo()
		{
			if (!enableUndoRedo || inputField == null || !inputField.isFocused)
			{
				return false;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard == null || !IsControlPressed(keyboard))
			{
				return false;
			}

			bool shiftPressed = keyboard.shiftKey.isPressed;
			bool redoPressed = keyboard.yKey.wasPressedThisFrame ||
							   (shiftPressed && keyboard.zKey.wasPressedThisFrame);

			if (redoPressed)
			{
				return Redo();
			}

			if (keyboard.zKey.wasPressedThisFrame)
			{
				return Undo();
			}

			return false;
		}

		private bool Undo()
		{
			if (undoHistory.Count == 0 || inputField == null)
			{
				return false;
			}

			TextEditSnapshot current = CaptureCurrentSnapshot();
			TextEditSnapshot previous = PopSnapshot(undoHistory);

			PushSnapshot(redoHistory, current);
			ApplySnapshot(previous);
			return true;
		}

		private bool Redo()
		{
			if (redoHistory.Count == 0 || inputField == null)
			{
				return false;
			}

			TextEditSnapshot current = CaptureCurrentSnapshot();
			TextEditSnapshot next = PopSnapshot(redoHistory);

			PushSnapshot(undoHistory, current);
			ApplySnapshot(next);
			return true;
		}

		private void TrackInputChange(string value)
		{
			if (inputField == null)
			{
				return;
			}

			TextEditSnapshot nextSnapshot = CaptureSnapshot(value);

			if (!hasTrackedSnapshot)
			{
				lastTrackedSnapshot = nextSnapshot;
				hasTrackedSnapshot = true;
				return;
			}

			if (lastTrackedSnapshot.Text != nextSnapshot.Text)
			{
				PushSnapshot(undoHistory, lastTrackedSnapshot);
				redoHistory.Clear();
			}

			lastTrackedSnapshot = nextSnapshot;
		}

		private void RecordManualTextChange()
		{
			if (!enableUndoRedo || inputField == null)
			{
				return;
			}

			EnsureTrackedSnapshot();
			PushSnapshot(undoHistory, lastTrackedSnapshot);
			redoHistory.Clear();
		}

		private void ResetUndoRedoHistory()
		{
			undoHistory.Clear();
			redoHistory.Clear();

			if (inputField == null)
			{
				hasTrackedSnapshot = false;
				return;
			}

			lastTrackedSnapshot = CaptureCurrentSnapshot();
			hasTrackedSnapshot = true;
		}

		private void ApplySnapshot(TextEditSnapshot snapshot)
		{
			if (inputField == null)
			{
				return;
			}

			isApplyingHistorySnapshot = true;

			Game.UI.RuntimeTextRefresh.SetTextWithoutNotify(inputField, snapshot.Text);
			RestoreSelection(snapshot);
			inputField.ForceLabelUpdate();
			inputField.ActivateInputField();

			lastTrackedSnapshot = CaptureCurrentSnapshot();
			hasTrackedSnapshot = true;
			isApplyingHistorySnapshot = false;

			RefreshView();
		}

		private void EnsureTrackedSnapshot()
		{
			if (hasTrackedSnapshot || inputField == null)
			{
				return;
			}

			lastTrackedSnapshot = CaptureCurrentSnapshot();
			hasTrackedSnapshot = true;
		}

		private TextEditSnapshot CaptureCurrentSnapshot()
		{
			return CaptureSnapshot(inputField != null ? inputField.text : string.Empty);
		}

		private TextEditSnapshot CaptureSnapshot(string text)
		{
			if (inputField == null)
			{
				return new TextEditSnapshot(text, 0, 0, 0);
			}

			return new TextEditSnapshot(
				text,
				inputField.caretPosition,
				inputField.selectionAnchorPosition,
				inputField.selectionFocusPosition);
		}

		private void RestoreSelection(TextEditSnapshot snapshot)
		{
			int length = inputField.text != null ? inputField.text.Length : 0;
			int caret = Mathf.Clamp(snapshot.CaretPosition, 0, length);
			int anchor = Mathf.Clamp(snapshot.SelectionAnchorPosition, 0, length);
			int focus = Mathf.Clamp(snapshot.SelectionFocusPosition, 0, length);

			inputField.caretPosition = caret;
			inputField.selectionAnchorPosition = anchor;
			inputField.selectionFocusPosition = focus;
		}

		private void PushSnapshot(List<TextEditSnapshot> history, TextEditSnapshot snapshot)
		{
			if (history == null)
			{
				return;
			}

			if (history.Count > 0 && history[history.Count - 1].Equals(snapshot))
			{
				return;
			}

			history.Add(snapshot);

			if (history.Count > Mathf.Max(1, maxUndoSteps))
			{
				history.RemoveAt(0);
			}
		}

		private static TextEditSnapshot PopSnapshot(List<TextEditSnapshot> history)
		{
			int index = history.Count - 1;
			TextEditSnapshot snapshot = history[index];
			history.RemoveAt(index);
			return snapshot;
		}

		private static bool IsControlPressed(Keyboard keyboard)
		{
			return keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
		}

		private void RefreshView()
		{
			string code = inputField != null
				? inputField.text
				: initialCode;

			if (tabTitleText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(tabTitleText, tabTitle);
			}

			if (lineNumbersText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(lineNumbersText, BuildLineNumbers(code));
			}

			if (highlightText != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(
					highlightText,
					useSyntaxHighlight
						? HighlightCode(code)
						: EscapeRichText(code));
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

			RecordManualTextChange();
			Game.UI.RuntimeTextRefresh.SetTextWithoutNotify(inputField, newText);
			inputField.caretPosition = newCaretPosition;
			inputField.selectionAnchorPosition = newCaretPosition;
			inputField.selectionFocusPosition = newCaretPosition;

			inputField.ForceLabelUpdate();

			lastTrackedSnapshot = CaptureCurrentSnapshot();
			hasTrackedSnapshot = true;
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

			Game.UI.RuntimeTextRefresh.SetText(
				statusText,
				$"Ln {line}, Col {column}    Lines {lineCount}    Chars {text.Length}");
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

			StringBuilder builder = new StringBuilder(value.Length);

			foreach (char character in value)
			{
				switch (character)
				{
					case '<':
						builder.Append("<noparse><</noparse>");
						break;

					case '>':
						builder.Append("<noparse>></noparse>");
						break;

					default:
						builder.Append(character);
						break;
				}
			}

			return builder.ToString();
		}

		private readonly struct TextEditSnapshot : IEquatable<TextEditSnapshot>
		{
			public readonly string Text;
			public readonly int CaretPosition;
			public readonly int SelectionAnchorPosition;
			public readonly int SelectionFocusPosition;

			public TextEditSnapshot(
				string text,
				int caretPosition,
				int selectionAnchorPosition,
				int selectionFocusPosition)
			{
				Text = text ?? string.Empty;
				CaretPosition = caretPosition;
				SelectionAnchorPosition = selectionAnchorPosition;
				SelectionFocusPosition = selectionFocusPosition;
			}

			public bool Equals(TextEditSnapshot other)
			{
				return Text == other.Text &&
					   CaretPosition == other.CaretPosition &&
					   SelectionAnchorPosition == other.SelectionAnchorPosition &&
					   SelectionFocusPosition == other.SelectionFocusPosition;
			}
		}
	}
}
