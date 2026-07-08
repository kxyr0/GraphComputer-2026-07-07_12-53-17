using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	public enum IdeOutputMessageType
	{
		Info,
		Success,
		Warning,
		Error,
		System
	}

	[DisallowMultipleComponent]
	public sealed class IdeOutputConsoleView : MonoBehaviour
	{
		[Header("UI")]
		[SerializeField] private TMP_InputField outputField;
		[SerializeField] private Scrollbar verticalScrollbar;

		[Header("Settings")]
		[SerializeField, Min(1)] private int maxLines = 300;
		[SerializeField] private bool autoScroll = true;
		[SerializeField] private bool autoScrollOnlyIfAlreadyAtBottom = true;
		[SerializeField, Range(0f, 0.2f)] private float bottomThreshold = 0.03f;
		[SerializeField] private bool useRichText = true;
		[SerializeField] private bool printBootMessage = true;

		private readonly StringBuilder builder = new StringBuilder(4096);
		private readonly Queue<string> lines = new Queue<string>();

		private bool isDirty;
		private bool forceScrollToBottom;
		private bool wasAtBottomBeforeWrite = true;
		private Coroutine scrollCoroutine;

		private void Awake()
		{
			ResolveReferences();
			ConfigureOutputField();
			ClearImmediate();

			if (printBootMessage)
			{
				WriteSystem("[Output] Build output stream initialized.");
				FlushNow();
			}
		}

		private void LateUpdate()
		{
			if (!isDirty)
			{
				return;
			}

			FlushNow();
		}

		private void ResolveReferences()
		{
			if (outputField == null)
			{
				outputField = GetComponentInChildren<TMP_InputField>(true);
			}

			if (verticalScrollbar == null && outputField != null)
			{
				verticalScrollbar = outputField.verticalScrollbar;
			}
		}

		private void ConfigureOutputField()
		{
			if (outputField == null)
			{
				return;
			}

			outputField.readOnly = true;

			// ВАЖНО:
			// Interactable должен быть true, иначе TMP_InputField
			// может перестать нормально управлять своим внутренним Text Area.
			outputField.interactable = true;

			outputField.lineType = TMP_InputField.LineType.MultiLineNewline;
			outputField.contentType = TMP_InputField.ContentType.Standard;
			outputField.richText = useRichText;
			outputField.resetOnDeActivation = false;
			outputField.restoreOriginalTextOnEscape = false;

			outputField.transition = Selectable.Transition.None;
			outputField.navigation = new Navigation
			{
				mode = Navigation.Mode.None
			};

			// Caret визуально не нужен, но сам InputField остаётся живым.
			outputField.caretWidth = 0;
			outputField.customCaretColor = true;
			outputField.caretColor = new Color(0f, 0f, 0f, 0f);
			outputField.selectionColor = new Color(1f, 1f, 1f, 0.08f);

			if (outputField.textComponent != null)
			{
				outputField.textComponent.richText = useRichText;
				outputField.textComponent.enableWordWrapping = true;
				outputField.textComponent.overflowMode = TextOverflowModes.Overflow;
			}

			if (outputField.placeholder != null)
			{
				outputField.placeholder.gameObject.SetActive(false);
			}

			if (verticalScrollbar == null)
			{
				verticalScrollbar = outputField.verticalScrollbar;
			}
		}

		public void WriteInfo(string message)
		{
			Write(message, IdeOutputMessageType.Info);
		}

		public void WriteSuccess(string message)
		{
			Write(message, IdeOutputMessageType.Success);
		}

		public void WriteWarning(string message)
		{
			Write(message, IdeOutputMessageType.Warning);
		}

		public void WriteError(string message)
		{
			Write(message, IdeOutputMessageType.Error);
		}

		public void WriteSystem(string message)
		{
			Write(message, IdeOutputMessageType.System);
		}

		public void WriteBuildReport(
			string title,
			int warnings,
			int errors,
			string summary)
		{
			BeginWriteBatch();

			WriteSystem($"[Build] {title}");

			if (errors > 0)
			{
				WriteError($"[Build] Failed with {errors} error(s), {warnings} warning(s).");
			}
			else if (warnings > 0)
			{
				WriteWarning($"[Build] Completed with {warnings} warning(s).");
			}
			else
			{
				WriteSuccess("[Build] Completed successfully.");
			}

			if (!string.IsNullOrWhiteSpace(summary))
			{
				WriteInfo(summary);
			}

			EndWriteBatch(scrollToBottom: true);
		}

		public void BeginWriteBatch()
		{
			wasAtBottomBeforeWrite = IsAtBottom();
		}

		public void EndWriteBatch(bool scrollToBottom)
		{
			if (scrollToBottom)
			{
				forceScrollToBottom = true;
			}

			isDirty = true;
		}

		public void Write(string message, IdeOutputMessageType type = IdeOutputMessageType.Info)
		{
			if (!isDirty)
			{
				wasAtBottomBeforeWrite = IsAtBottom();
			}

			lines.Enqueue(BuildLine(message, type));

			while (lines.Count > maxLines)
			{
				lines.Dequeue();
			}

			isDirty = true;
		}

		public void Clear()
		{
			lines.Clear();
			forceScrollToBottom = true;
			isDirty = true;
		}

		public void ClearImmediate()
		{
			lines.Clear();

			if (outputField != null)
			{
				outputField.SetTextWithoutNotify(string.Empty);
				outputField.ForceLabelUpdate();
			}

			isDirty = false;
			forceScrollToBottom = false;
		}

		private string BuildLine(string message, IdeOutputMessageType type)
		{
			string safeMessage = useRichText
				? EscapeRichText(message ?? string.Empty)
				: message ?? string.Empty;

			if (!useRichText)
			{
				return safeMessage;
			}

			return type switch
			{
				IdeOutputMessageType.Success => $"<color=#7CFF7C>{safeMessage}</color>",
				IdeOutputMessageType.Warning => $"<color=#FFD166>{safeMessage}</color>",
				IdeOutputMessageType.Error => $"<color=#FF5C5C>{safeMessage}</color>",
				IdeOutputMessageType.System => $"<color=#9CDCFE>{safeMessage}</color>",
				_ => $"<color=#D4D4D4>{safeMessage}</color>"
			};
		}

		private void FlushNow()
		{
			if (outputField == null)
			{
				isDirty = false;
				return;
			}

			builder.Clear();

			foreach (string line in lines)
			{
				builder.AppendLine(line);
			}

			outputField.SetTextWithoutNotify(builder.ToString());

			// Держим caret в конце, чтобы TMP_InputField сам
			// корректно прокрутил внутренний Text Area вниз.
			int end = outputField.text.Length;
			outputField.caretPosition = end;
			outputField.selectionAnchorPosition = end;
			outputField.selectionFocusPosition = end;

			outputField.ForceLabelUpdate();

			isDirty = false;

			if (ShouldScrollToBottom())
			{
				ScrollToBottom();
			}

			forceScrollToBottom = false;
		}

		private bool ShouldScrollToBottom()
		{
			if (!autoScroll)
			{
				return false;
			}

			if (forceScrollToBottom)
			{
				return true;
			}

			if (!autoScrollOnlyIfAlreadyAtBottom)
			{
				return true;
			}

			return wasAtBottomBeforeWrite;
		}

		private bool IsAtBottom()
		{
			if (verticalScrollbar == null)
			{
				return true;
			}

			return verticalScrollbar.value <= bottomThreshold;
		}

		private void ScrollToBottom()
		{
			if (outputField == null)
			{
				return;
			}

			if (scrollCoroutine != null)
			{
				StopCoroutine(scrollCoroutine);
			}

			scrollCoroutine = StartCoroutine(ScrollToBottomRoutine());
		}

		private IEnumerator ScrollToBottomRoutine()
		{
			yield return null;

			if (outputField == null)
			{
				yield break;
			}

			int end = outputField.text.Length;

			outputField.caretPosition = end;
			outputField.selectionAnchorPosition = end;
			outputField.selectionFocusPosition = end;

			outputField.MoveTextEnd(false);
			outputField.ForceLabelUpdate();

			if (verticalScrollbar != null)
			{
				verticalScrollbar.value = 0f;
			}
		}

		private static string EscapeRichText(string value)
		{
			return (value ?? string.Empty)
				.Replace("&", "&amp;")
				.Replace("<", "&lt;")
				.Replace(">", "&gt;");
		}
	}
}