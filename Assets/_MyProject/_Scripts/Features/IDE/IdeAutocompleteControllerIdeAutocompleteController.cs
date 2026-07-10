using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeAutocompleteController : MonoBehaviour
	{
		[SerializeField] private TMP_InputField inputField;

		[Header("Dropdown")]
		[SerializeField] private RectTransform dropdownPanel;
		[SerializeField] private RectTransform itemsRoot;
		[SerializeField] private IdeAutocompleteSuggestionView itemPrefab;
		[SerializeField] private CanvasGroup dropdownCanvasGroup;

		[Header("Behaviour")]
		[SerializeField, Min(1)] private int minPrefixLength = 1;
		[SerializeField, Min(1)] private int maxSuggestions = 8;
		[SerializeField] private bool caseInsensitive = true;
		[SerializeField] private Vector2 dropdownOffset = new(0f, -8f);

		[Header("Dictionary")]
		[SerializeField] private IdeAutocompleteEntry[] entries;
		[Header("Safe Area")]
		[SerializeField] private RectTransform safeAreaRoot;
		[SerializeField] private Vector2 safePadding = new Vector2(8f, 8f);
		[SerializeField] private bool flipAboveWhenNoSpace = true;
		private readonly List<IdeAutocompleteSuggestionView> spawnedItems = new();
		private readonly List<IdeAutocompleteEntry> visibleEntries = new();

		private Canvas canvas;
		private int selectedIndex;
		private int tokenStart;
		private int tokenEnd;
		private bool isOpen;

		private void Reset()
		{
			inputField = GetComponentInChildren<TMP_InputField>(true);
			FillDefaultEntries();
		}

		private void Awake()
		{
			canvas = GetComponentInParent<Canvas>();

			if (dropdownCanvasGroup == null && dropdownPanel != null)
			{
				dropdownCanvasGroup = dropdownPanel.GetComponent<CanvasGroup>();

				if (dropdownCanvasGroup == null)
				{
					dropdownCanvasGroup = dropdownPanel.gameObject.AddComponent<CanvasGroup>();
				}
			}

			EnsureDefaultEntries();

			HideInstant();
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
			if (!isOpen || inputField == null || !inputField.isFocused)
			{
				return;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard == null)
			{
				return;
			}

			if (keyboard.escapeKey.wasPressedThisFrame)
			{
				Hide();
				return;
			}

			if (keyboard.downArrowKey.wasPressedThisFrame)
			{
				MoveSelection(1);
				return;
			}

			if (keyboard.upArrowKey.wasPressedThisFrame)
			{
				MoveSelection(-1);
				return;
			}

			if (keyboard.tabKey.wasPressedThisFrame)
			{
				AcceptSelected();
				return;
			}
		}

		private void HandleInputChanged(string _)
		{
			RefreshSuggestions();
		}

		private void RefreshSuggestions()
		{
			if (inputField == null || !inputField.isFocused)
			{
				Hide();
				return;
			}

			string text = inputField.text ?? string.Empty;
			int caret = Mathf.Clamp(inputField.caretPosition, 0, text.Length);

			string prefix = GetCurrentToken(text, caret, out tokenStart, out tokenEnd);

			if (prefix.Length < minPrefixLength)
			{
				Hide();
				return;
			}

			visibleEntries.Clear();

			foreach (IdeAutocompleteEntry entry in entries)
			{
				if (entry == null || string.IsNullOrWhiteSpace(entry.InsertText))
				{
					continue;
				}

				if (!MatchesPrefix(entry, prefix))
				{
					continue;
				}

				visibleEntries.Add(entry);

				if (visibleEntries.Count >= maxSuggestions)
				{
					break;
				}
			}

			if (visibleEntries.Count == 0)
			{
				Hide();
				return;
			}

			selectedIndex = Mathf.Clamp(selectedIndex, 0, visibleEntries.Count - 1);

			BuildDropdown();
			PositionDropdownNearCaret();
			Show();
		}

		private bool MatchesPrefix(IdeAutocompleteEntry entry, string prefix)
		{
			StringComparison comparison = caseInsensitive
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;

			return entry.InsertText.StartsWith(prefix, comparison) ||
				   entry.DisplayText.StartsWith(prefix, comparison);
		}

		private void BuildDropdown()
		{
			ClearItems();

			for (int i = 0; i < visibleEntries.Count; i++)
			{
				IdeAutocompleteSuggestionView item = Instantiate(itemPrefab, itemsRoot);
				item.name = $"Autocomplete_{visibleEntries[i].InsertText}";
				item.Bind(visibleEntries[i], i == selectedIndex, AcceptEntry);

				spawnedItems.Add(item);
			}

			LayoutRebuilder.ForceRebuildLayoutImmediate(itemsRoot);
			LayoutRebuilder.ForceRebuildLayoutImmediate(dropdownPanel);
		}

		private void MoveSelection(int direction)
		{
			if (visibleEntries.Count == 0)
			{
				return;
			}

			selectedIndex += direction;

			if (selectedIndex < 0)
			{
				selectedIndex = visibleEntries.Count - 1;
			}

			if (selectedIndex >= visibleEntries.Count)
			{
				selectedIndex = 0;
			}

			BuildDropdown();
		}

		private void AcceptSelected()
		{
			if (visibleEntries.Count == 0)
			{
				return;
			}

			selectedIndex = Mathf.Clamp(selectedIndex, 0, visibleEntries.Count - 1);
			AcceptEntry(visibleEntries[selectedIndex]);
		}

		private void AcceptEntry(IdeAutocompleteEntry entry)
		{
			if (entry == null || inputField == null)
			{
				return;
			}

			string text = inputField.text ?? string.Empty;

			tokenStart = Mathf.Clamp(tokenStart, 0, text.Length);
			tokenEnd = Mathf.Clamp(tokenEnd, tokenStart, text.Length);

			string insertText = entry.InsertText ?? string.Empty;

			string newText =
				text.Substring(0, tokenStart) +
				insertText +
				text.Substring(tokenEnd);

			int newCaret = tokenStart + insertText.Length;

			inputField.SetTextWithoutNotify(newText);
			inputField.caretPosition = newCaret;
			inputField.selectionAnchorPosition = newCaret;
			inputField.selectionFocusPosition = newCaret;
			inputField.ForceLabelUpdate();
			inputField.ActivateInputField();

			Hide();

			// Чтобы внешний IdeInputCodeEditor обновил подсветку.
			inputField.onValueChanged.Invoke(newText);
		}

		private void PositionDropdownNearCaret()
		{
			if (dropdownPanel == null || inputField == null || inputField.textComponent == null)
			{
				return;
			}

			RectTransform dropdownParent = dropdownPanel.parent as RectTransform;

			if (dropdownParent == null)
			{
				return;
			}

			Canvas.ForceUpdateCanvases();

			if (itemsRoot != null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(itemsRoot);
			}

			LayoutRebuilder.ForceRebuildLayoutImmediate(dropdownPanel);

			Vector3 caretWorldPosition = GetCaretWorldPosition();

			Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
				GetEventCamera(),
				caretWorldPosition
			);

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				dropdownParent,
				screenPosition,
				GetEventCamera(),
				out Vector2 caretLocalPosition
			);

			Rect safeRect = GetSafeRectInParent(dropdownParent);
			Vector2 panelSize = dropdownPanel.rect.size;

			Vector2 belowPosition = caretLocalPosition + dropdownOffset;
			Vector2 targetPosition = belowPosition;

			if (flipAboveWhenNoSpace)
			{
				float panelBottomIfBelow = GetPanelBottom(targetPosition, panelSize, dropdownPanel.pivot);

				if (panelBottomIfBelow < safeRect.yMin + safePadding.y)
				{
					targetPosition = caretLocalPosition + new Vector2(
						dropdownOffset.x,
						Mathf.Abs(dropdownOffset.y) + panelSize.y
					);
				}
			}

			targetPosition = ClampPanelPositionToSafeRect(
				targetPosition,
				panelSize,
				dropdownPanel.pivot,
				safeRect
			);

			dropdownPanel.anchoredPosition = targetPosition;
			dropdownPanel.SetAsLastSibling();
		}
		private Rect GetSafeRectInParent(RectTransform dropdownParent)
		{
			RectTransform root = safeAreaRoot != null
				? safeAreaRoot
				: dropdownParent;

			Vector3[] worldCorners = new Vector3[4];
			root.GetWorldCorners(worldCorners);

			Vector2 min = Vector2.positiveInfinity;
			Vector2 max = Vector2.negativeInfinity;

			for (int i = 0; i < worldCorners.Length; i++)
			{
				Vector2 localPoint = dropdownParent.InverseTransformPoint(worldCorners[i]);

				min = Vector2.Min(min, localPoint);
				max = Vector2.Max(max, localPoint);
			}

			return Rect.MinMaxRect(
				min.x + safePadding.x,
				min.y + safePadding.y,
				max.x - safePadding.x,
				max.y - safePadding.y
			);
		}

		private static Vector2 ClampPanelPositionToSafeRect(
			Vector2 position,
			Vector2 size,
			Vector2 pivot,
			Rect safeRect)
		{
			float minX = safeRect.xMin + size.x * pivot.x;
			float maxX = safeRect.xMax - size.x * (1f - pivot.x);

			float minY = safeRect.yMin + size.y * pivot.y;
			float maxY = safeRect.yMax - size.y * (1f - pivot.y);

			if (minX <= maxX)
			{
				position.x = Mathf.Clamp(position.x, minX, maxX);
			}
			else
			{
				position.x = safeRect.center.x;
			}

			if (minY <= maxY)
			{
				position.y = Mathf.Clamp(position.y, minY, maxY);
			}
			else
			{
				position.y = safeRect.center.y;
			}

			return position;
		}

		private static float GetPanelBottom(Vector2 position, Vector2 size, Vector2 pivot)
		{
			return position.y - size.y * pivot.y;
		}
		private Vector3 GetCaretWorldPosition()
		{
			TMP_Text textComponent = inputField.textComponent;
			RectTransform textRect = textComponent.rectTransform;

			textComponent.ForceMeshUpdate();

			string text = inputField.text ?? string.Empty;
			int caret = Mathf.Clamp(inputField.caretPosition, 0, text.Length);

			if (caret > 0 && textComponent.textInfo.characterCount > 0)
			{
				int charIndex = Mathf.Clamp(caret - 1, 0, textComponent.textInfo.characterCount - 1);
				TMP_CharacterInfo characterInfo = textComponent.textInfo.characterInfo[charIndex];

				Vector3 localPosition = characterInfo.bottomRight;
				return textRect.TransformPoint(localPosition);
			}

			Rect rect = textRect.rect;
			Vector3 fallbackLocalPosition = new(rect.xMin, rect.yMax, 0f);
			return textRect.TransformPoint(fallbackLocalPosition);
		}

		private string GetCurrentToken(
			string text,
			int caret,
			out int start,
			out int end)
		{
			start = caret;
			end = caret;

			while (start > 0 && IsTokenChar(text[start - 1]))
			{
				start--;
			}

			while (end < text.Length && IsTokenChar(text[end]))
			{
				end++;
			}

			if (end <= start)
			{
				return string.Empty;
			}

			return text.Substring(start, caret - start);
		}

		private static bool IsTokenChar(char value)
		{
			return char.IsLetterOrDigit(value) || value == '_';
		}

		private void Show()
		{
			isOpen = true;

			if (dropdownCanvasGroup != null)
			{
				dropdownCanvasGroup.alpha = 1f;
				dropdownCanvasGroup.interactable = true;
				dropdownCanvasGroup.blocksRaycasts = true;
			}
		}

		private void Hide()
		{
			isOpen = false;

			if (dropdownCanvasGroup != null)
			{
				dropdownCanvasGroup.alpha = 0f;
				dropdownCanvasGroup.interactable = false;
				dropdownCanvasGroup.blocksRaycasts = false;
			}

			ClearItems();
		}

		private void HideInstant()
		{
			Hide();
		}

		private void ClearItems()
		{
			for (int i = spawnedItems.Count - 1; i >= 0; i--)
			{
				if (spawnedItems[i] != null)
				{
					Destroy(spawnedItems[i].gameObject);
				}
			}

			spawnedItems.Clear();
		}

		private Camera GetEventCamera()
		{
			if (canvas == null)
			{
				return null;
			}

			if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
			{
				return null;
			}

			return canvas.worldCamera != null
				? canvas.worldCamera
				: Camera.main;
		}

		[ContextMenu("Fill Default Entries")]
		private void FillDefaultEntries()
		{
			entries = BuildDefaultEntries();
		}

		private void EnsureDefaultEntries()
		{
			IdeAutocompleteEntry[] defaultEntries = BuildDefaultEntries();

			if (entries == null || entries.Length == 0)
			{
				entries = defaultEntries;
				return;
			}

			List<IdeAutocompleteEntry> mergedEntries = new List<IdeAutocompleteEntry>(entries.Length + defaultEntries.Length);
			HashSet<string> knownInsertTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (IdeAutocompleteEntry entry in entries)
			{
				if (entry == null || string.IsNullOrWhiteSpace(entry.InsertText))
				{
					continue;
				}

				if (knownInsertTexts.Add(entry.InsertText))
				{
					mergedEntries.Add(entry);
				}
			}

			foreach (IdeAutocompleteEntry defaultEntry in defaultEntries)
			{
				if (defaultEntry == null || string.IsNullOrWhiteSpace(defaultEntry.InsertText))
				{
					continue;
				}

				if (knownInsertTexts.Add(defaultEntry.InsertText))
				{
					mergedEntries.Add(defaultEntry);
				}
			}

			entries = mergedEntries.ToArray();
		}

		private static IdeAutocompleteEntry[] BuildDefaultEntries()
		{
			return new[]
			{
				new IdeAutocompleteEntry("module", "module", "keyword"),
				new IdeAutocompleteEntry("require", "require", "keyword"),
				new IdeAutocompleteEntry("optional", "optional", "keyword"),
				new IdeAutocompleteEntry("if", "if", "keyword"),
				new IdeAutocompleteEntry("else", "else", "keyword"),
				new IdeAutocompleteEntry("end", "end", "keyword"),
				new IdeAutocompleteEntry("budget", "budget", "block"),
				new IdeAutocompleteEntry("export", "export", "block"),
				new IdeAutocompleteEntry("boot", "boot", "block"),
				new IdeAutocompleteEntry("scheduler", "scheduler", "block"),
				new IdeAutocompleteEntry("tick", "tick", "block"),
				new IdeAutocompleteEntry("mode", "mode", "setting"),
				new IdeAutocompleteEntry("platform", "platform", "setting"),
				new IdeAutocompleteEntry("PC", "PC", "platform"),
				new IdeAutocompleteEntry("strict", "strict", "mode"),
				new IdeAutocompleteEntry("balanced", "balanced", "mode"),
				new IdeAutocompleteEntry("permissive", "permissive", "mode"),
				new IdeAutocompleteEntry("aggressive", "aggressive", "mode"),
				new IdeAutocompleteEntry("safe", "safe", "mode"),
				new IdeAutocompleteEntry("performance", "performance", "mode"),
				new IdeAutocompleteEntry("scanTraffic", "scanTraffic", "setting"),
				new IdeAutocompleteEntry("allowUnknownPorts", "allowUnknownPorts", "setting"),
				new IdeAutocompleteEntry("true", "true", "literal"),
				new IdeAutocompleteEntry("false", "false", "literal"),
				new IdeAutocompleteEntry("import", "import", "keyword"),
				new IdeAutocompleteEntry("link", "link", "keyword"),

				new IdeAutocompleteEntry("alert", "alert", "function"),
				new IdeAutocompleteEntry("warning", "warning", "function"),
				new IdeAutocompleteEntry("start", "start", "function"),
				new IdeAutocompleteEntry("mount", "mount", "function"),
				new IdeAutocompleteEntry("load", "load", "function"),
				new IdeAutocompleteEntry("scan", "scan", "function"),
				new IdeAutocompleteEntry("rule", "rule", "function"),
				new IdeAutocompleteEntry("allow", "allow", "function"),
				new IdeAutocompleteEntry("block", "block", "function"),
				new IdeAutocompleteEntry("protect", "protect", "function"),
				new IdeAutocompleteEntry("encrypt", "encrypt", "function"),
				new IdeAutocompleteEntry("backup", "backup", "function"),
				new IdeAutocompleteEntry("cache", "cache", "function"),
				new IdeAutocompleteEntry("index", "index", "function"),
				new IdeAutocompleteEntry("watch", "watch", "function"),
				new IdeAutocompleteEntry("monitor", "monitor", "function"),
				new IdeAutocompleteEntry("route", "route", "function"),
				new IdeAutocompleteEntry("recover", "recover", "function"),
				new IdeAutocompleteEntry("schedule", "schedule", "function"),
				new IdeAutocompleteEntry("bind", "bind", "function"),
				new IdeAutocompleteEntry("throttle", "throttle", "function"),
				new IdeAutocompleteEntry("flush", "flush", "function"),
				new IdeAutocompleteEntry("reduce", "reduce", "function"),
				new IdeAutocompleteEntry("enable", "enable", "function"),
				new IdeAutocompleteEntry("speed", "speed", "function"),
				new IdeAutocompleteEntry("gameSpeed", "gameSpeed", "function"),
				new IdeAutocompleteEntry("timeSpeed", "timeSpeed", "function"),
				new IdeAutocompleteEntry("log", "log", "function"),
				new IdeAutocompleteEntry("disable", "disable", "function"),
				new IdeAutocompleteEntry("connect", "connect", "function"),

				new IdeAutocompleteEntry("SystemCore", "SystemCore", "module"),
				new IdeAutocompleteEntry("Kernel", "Kernel", "module"),
				new IdeAutocompleteEntry("Data", "Data", "module"),
				new IdeAutocompleteEntry("Security", "Security", "module"),
				new IdeAutocompleteEntry("Control", "Control", "module"),
				new IdeAutocompleteEntry("Time", "Time", "module"),
				new IdeAutocompleteEntry("Firewall", "Firewall", "module"),
				new IdeAutocompleteEntry("FileSystem", "FileSystem", "module"),
				new IdeAutocompleteEntry("MemoryManager", "MemoryManager", "module"),
				new IdeAutocompleteEntry("DisplayManager", "DisplayManager", "module"),

				new IdeAutocompleteEntry("stability", "stability", "metric"),
				new IdeAutocompleteEntry("network", "network", "metric"),
				new IdeAutocompleteEntry("security", "security", "metric"),
				new IdeAutocompleteEntry("load", "load", "metric"),
				new IdeAutocompleteEntry("memory", "memory", "metric"),
				new IdeAutocompleteEntry("storage", "storage", "metric"),
				new IdeAutocompleteEntry("falseBlocks", "falseBlocks", "metric"),
				new IdeAutocompleteEntry("fragmentation", "fragmentation", "metric"),
				new IdeAutocompleteEntry("leakRisk", "leakRisk", "metric"),
				new IdeAutocompleteEntry("gpuUsage", "gpuUsage", "metric"),
				new IdeAutocompleteEntry("userMood", "userMood", "metric"),

				new IdeAutocompleteEntry("data", "data", "node"),
				new IdeAutocompleteEntry("firewall", "firewall", "node"),
				new IdeAutocompleteEntry("file_system", "file_system", "node"),
				new IdeAutocompleteEntry("memory_manager", "memory_manager", "node"),
				new IdeAutocompleteEntry("display_manager", "display_manager", "node"),
				new IdeAutocompleteEntry("kernel", "kernel", "node"),
				new IdeAutocompleteEntry("core", "core", "node"),
				new IdeAutocompleteEntry("time", "time", "node"),
				new IdeAutocompleteEntry("output", "output", "port"),
				new IdeAutocompleteEntry("level", "level", "property")
			};
		}
	}

	[Serializable]
	public sealed class IdeAutocompleteEntry
	{
		[SerializeField] private string displayText;
		[SerializeField] private string insertText;
		[SerializeField] private string kind;

		public string DisplayText => string.IsNullOrWhiteSpace(displayText)
			? insertText
			: displayText;

		public string InsertText => string.IsNullOrWhiteSpace(insertText)
			? displayText
			: insertText;

		public string Kind => kind;

		public IdeAutocompleteEntry(string displayText, string insertText, string kind)
		{
			this.displayText = displayText;
			this.insertText = insertText;
			this.kind = kind;
		}
	}
}
