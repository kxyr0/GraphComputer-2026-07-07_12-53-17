using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.World.Time;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeConsoleController : MonoBehaviour
	{
		[Header("UI")]
		[SerializeField] private TMP_InputField consoleInput;
		[SerializeField] private ScrollRect scrollRect;
		[SerializeField] private LayoutElement inputLayoutElement;

		[Header("Services")]
		[SerializeField] private GameTimeService timeService;

		[Header("Console")]
		[SerializeField] private string userName = "user";
		[SerializeField] private string systemName = "GraphOS";
		[SerializeField] private bool focusInputOnStart = true;
		[SerializeField] private bool printBootMessage = true;

		[Header("Layout")]
		[SerializeField, Min(16f)] private float minInputHeight = 160f;
		[SerializeField, Min(0f)] private float inputHeightPadding = 32f;

		private readonly List<string> commandHistory = new();

		private ConsoleDirectory rootDirectory;
		private ConsoleDirectory currentDirectory;

		private string currentPath = "/";
		private string committedText = string.Empty;
		private string currentCommand = string.Empty;

		private int promptStartIndex;
		private int historyIndex;

		private bool isChangingTextInternally;
		private Coroutine scrollCoroutine;

		private void Awake()
		{
			ResolveReferences();
			ConfigureInput();
			BuildVirtualFileSystem();

			if (timeService == null)
			{
				timeService = FindFirstObjectByType<GameTimeService>();
			}

			ResetConsole();
		}

		private void Start()
		{
			if (focusInputOnStart)
			{
				FocusInput();
			}
		}

		private void OnEnable()
		{
			if (consoleInput != null)
			{
				consoleInput.onValueChanged.AddListener(HandleValueChanged);
				consoleInput.onSubmit.AddListener(HandleSubmit);
			}
		}

		private void OnDisable()
		{
			if (consoleInput != null)
			{
				consoleInput.onValueChanged.RemoveListener(HandleValueChanged);
				consoleInput.onSubmit.RemoveListener(HandleSubmit);
			}
		}

		private void Update()
		{
			KeepCaretInEditableArea();
			HandleHistoryInput();
		}

		public void ResetConsole()
		{
			committedText = string.Empty;
			currentCommand = string.Empty;
			promptStartIndex = 0;

			SetTextInternal(string.Empty, 0);

			if (printBootMessage)
			{
				WriteLine("[System Core] Boot sequence completed.");
				WriteLine("GraphOS Console initialized.");
				WriteLine("Type 'help' to list available commands.");
				WriteLine(string.Empty);
			}

			AppendPrompt();
		}

		public void WriteLine(string message)
		{
			committedText += (message ?? string.Empty) + "\n";
			SetTextInternal(committedText + currentCommand, committedText.Length + currentCommand.Length);
		}

		private void ResolveReferences()
		{
			if (consoleInput == null)
			{
				consoleInput = GetComponentInChildren<TMP_InputField>(true);
			}

			if (scrollRect == null)
			{
				scrollRect = GetComponentInParent<ScrollRect>();
			}

			if (inputLayoutElement == null && consoleInput != null)
			{
				inputLayoutElement = consoleInput.GetComponent<LayoutElement>();
			}
		}

		private void ConfigureInput()
		{
			if (consoleInput == null)
			{
				return;
			}

			consoleInput.lineType = TMP_InputField.LineType.MultiLineSubmit;
			consoleInput.contentType = TMP_InputField.ContentType.Standard;
			consoleInput.richText = false;
			consoleInput.resetOnDeActivation = false;
			consoleInput.restoreOriginalTextOnEscape = false;

			if (consoleInput.textComponent != null)
			{
				consoleInput.textComponent.richText = false;
				consoleInput.textComponent.enableWordWrapping = true;
				consoleInput.textComponent.overflowMode = TextOverflowModes.Overflow;
			}

			if (inputLayoutElement == null)
			{
				inputLayoutElement = consoleInput.gameObject.AddComponent<LayoutElement>();
			}
		}

		private void HandleValueChanged(string value)
		{
			if (isChangingTextInternally)
			{
				return;
			}

			value ??= string.Empty;

			if (!value.StartsWith(committedText, StringComparison.Ordinal))
			{
				RestoreEditableText();
				return;
			}

			currentCommand = value.Substring(promptStartIndex);
			KeepCaretInEditableArea();
			RefreshInputHeight();
		}

		private void HandleSubmit(string _)
		{
			SubmitCurrentCommand();
		}

		private void SubmitCurrentCommand()
		{
			string command = currentCommand.Trim();

			committedText += currentCommand + "\n";

			if (!string.IsNullOrWhiteSpace(command))
			{
				commandHistory.Add(command);
				historyIndex = commandHistory.Count;

				ExecuteCommand(command);
			}

			currentCommand = string.Empty;
			AppendPrompt();
		}

		private void ExecuteCommand(string commandLine)
		{
			List<string> args = SplitArguments(commandLine);

			if (args.Count == 0)
			{
				return;
			}

			string command = args[0].ToLowerInvariant();

			switch (command)
			{
				case "help":
					CommandHelp();
					break;

				case "clear":
				case "cls":
					CommandClear();
					break;

				case "pwd":
					WriteLine(currentPath);
					break;

				case "ls":
				case "list":
				case "dir":
					CommandList();
					break;

				case "cd":
					CommandChangeDirectory(args);
					break;

				case "cat":
				case "type":
					CommandCat(args);
					break;

				case "echo":
					CommandEcho(args);
					break;

				case "time":
					CommandTime();
					break;

				case "whoami":
					WriteLine(userName);
					break;

				case "system":
				case "neofetch":
					CommandSystem();
					break;

				case "history":
					CommandHistory();
					break;

				default:
					WriteLine($"Command not found: {command}");
					break;
			}
		}

		private void CommandHelp()
		{
			WriteLine("Available commands:");
			WriteLine("  help              Show command list");
			WriteLine("  clear / cls       Clear console output");
			WriteLine("  pwd               Print current path");
			WriteLine("  list / ls / dir   List directory content");
			WriteLine("  cd <path>         Change directory");
			WriteLine("  cat <file>        Print file content");
			WriteLine("  echo <text>       Print text");
			WriteLine("  time              Print current game time");
			WriteLine("  whoami            Print current user");
			WriteLine("  system            Print system info");
			WriteLine("  history           Print command history");
		}

		private void CommandClear()
		{
			committedText = string.Empty;
			currentCommand = string.Empty;
			promptStartIndex = 0;
		}

		private void CommandList()
		{
			foreach (string directoryName in currentDirectory.Directories.Keys)
			{
				WriteLine($"{directoryName}/");
			}

			foreach (string fileName in currentDirectory.Files.Keys)
			{
				WriteLine(fileName);
			}
		}

		private void CommandChangeDirectory(List<string> args)
		{
			if (args.Count < 2)
			{
				currentDirectory = rootDirectory;
				currentPath = "/";
				return;
			}

			string targetPath = args[1];

			if (!TryResolveDirectory(targetPath, out ConsoleDirectory directory, out string resolvedPath))
			{
				WriteLine($"Directory not found: {targetPath}");
				return;
			}

			currentDirectory = directory;
			currentPath = resolvedPath;
		}

		private void CommandCat(List<string> args)
		{
			if (args.Count < 2)
			{
				WriteLine("Usage: cat <file>");
				return;
			}

			string fileName = args[1];

			if (!currentDirectory.Files.TryGetValue(fileName, out string content))
			{
				WriteLine($"File not found: {fileName}");
				return;
			}

			WriteLine(content);
		}

		private void CommandEcho(List<string> args)
		{
			if (args.Count <= 1)
			{
				WriteLine(string.Empty);
				return;
			}

			WriteLine(string.Join(" ", args.GetRange(1, args.Count - 1)));
		}

		private void CommandTime()
		{
			if (timeService == null)
			{
				WriteLine("Time service is not available.");
				return;
			}

			WriteLine($"{timeService.GetDateText()} {timeService.GetClockText()}");
		}

		private void CommandSystem()
		{
			WriteLine("GraphOS Developer Console");
			WriteLine($"User: {userName}");
			WriteLine($"Host: {systemName}");
			WriteLine($"Path: {currentPath}");
			WriteLine("Kernel: Prototype");
			WriteLine("Shell: node-console");
		}

		private void CommandHistory()
		{
			if (commandHistory.Count == 0)
			{
				WriteLine("No commands in history.");
				return;
			}

			for (int i = 0; i < commandHistory.Count; i++)
			{
				WriteLine($"{i + 1}: {commandHistory[i]}");
			}
		}

		private void AppendPrompt()
		{
			committedText += BuildPrompt();
			promptStartIndex = committedText.Length;

			SetTextInternal(committedText, promptStartIndex);
			FocusInput();
		}

		private string BuildPrompt()
		{
			return $"{userName}@{systemName}:{currentPath}$ ";
		}

		private void HandleHistoryInput()
		{
			if (consoleInput == null || !consoleInput.isFocused || commandHistory.Count == 0)
			{
				return;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard == null)
			{
				return;
			}

			if (keyboard.upArrowKey.wasPressedThisFrame)
			{
				historyIndex = Mathf.Clamp(historyIndex - 1, 0, commandHistory.Count - 1);
				SetCurrentCommand(commandHistory[historyIndex]);
			}

			if (keyboard.downArrowKey.wasPressedThisFrame)
			{
				historyIndex++;

				if (historyIndex >= commandHistory.Count)
				{
					historyIndex = commandHistory.Count;
					SetCurrentCommand(string.Empty);
					return;
				}

				SetCurrentCommand(commandHistory[historyIndex]);
			}
		}

		private void SetCurrentCommand(string command)
		{
			currentCommand = command ?? string.Empty;
			SetTextInternal(committedText + currentCommand, committedText.Length + currentCommand.Length);
			FocusInput();
		}

		private void RestoreEditableText()
		{
			SetTextInternal(committedText + currentCommand, committedText.Length + currentCommand.Length);
		}

		private void KeepCaretInEditableArea()
		{
			if (consoleInput == null || isChangingTextInternally)
			{
				return;
			}

			if (consoleInput.caretPosition < promptStartIndex ||
				consoleInput.selectionAnchorPosition < promptStartIndex ||
				consoleInput.selectionFocusPosition < promptStartIndex)
			{
				int end = consoleInput.text.Length;

				consoleInput.caretPosition = end;
				consoleInput.selectionAnchorPosition = end;
				consoleInput.selectionFocusPosition = end;
			}
		}

		private void SetTextInternal(string value, int caretPosition)
		{
			if (consoleInput == null)
			{
				return;
			}

			isChangingTextInternally = true;

			Game.UI.RuntimeTextRefresh.SetTextWithoutNotify(consoleInput, value);

			caretPosition = Mathf.Clamp(caretPosition, 0, consoleInput.text.Length);

			consoleInput.caretPosition = caretPosition;
			consoleInput.selectionAnchorPosition = caretPosition;
			consoleInput.selectionFocusPosition = caretPosition;

			consoleInput.ForceLabelUpdate();

			isChangingTextInternally = false;

			RefreshInputHeight();
			ScrollToBottom();
		}

		private void RefreshInputHeight()
		{
			if (consoleInput == null || consoleInput.textComponent == null || inputLayoutElement == null)
			{
				return;
			}

			consoleInput.textComponent.ForceMeshUpdate();

			float preferredHeight = consoleInput.textComponent.preferredHeight + inputHeightPadding;
			inputLayoutElement.preferredHeight = Mathf.Max(minInputHeight, preferredHeight);

			LayoutRebuilder.ForceRebuildLayoutImmediate(consoleInput.transform as RectTransform);
		}

		private void ScrollToBottom()
		{
			if (scrollRect == null)
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

			Canvas.ForceUpdateCanvases();

			if (scrollRect != null)
			{
				scrollRect.verticalNormalizedPosition = 0f;
			}
		}

		private void FocusInput()
		{
			if (consoleInput == null)
			{
				return;
			}

			consoleInput.ActivateInputField();
		}

		private bool TryResolveDirectory(
			string path,
			out ConsoleDirectory directory,
			out string resolvedPath)
		{
			directory = path.StartsWith("/", StringComparison.Ordinal)
				? rootDirectory
				: currentDirectory;

			List<string> parts = new();

			if (!path.StartsWith("/", StringComparison.Ordinal))
			{
				parts.AddRange(GetCurrentPathParts());
			}

			string[] rawParts = path.Split(
				'/',
				StringSplitOptions.RemoveEmptyEntries
			);

			foreach (string rawPart in rawParts)
			{
				string part = rawPart.Trim();

				if (part == ".")
				{
					continue;
				}

				if (part == "..")
				{
					if (parts.Count > 0)
					{
						parts.RemoveAt(parts.Count - 1);
					}

					continue;
				}

				parts.Add(part);
			}

			directory = rootDirectory;

			foreach (string part in parts)
			{
				if (!directory.Directories.TryGetValue(part, out ConsoleDirectory next))
				{
					directory = null;
					resolvedPath = currentPath;
					return false;
				}

				directory = next;
			}

			resolvedPath = parts.Count == 0
				? "/"
				: "/" + string.Join("/", parts);

			return true;
		}

		private List<string> GetCurrentPathParts()
		{
			List<string> parts = new();

			if (string.IsNullOrWhiteSpace(currentPath) || currentPath == "/")
			{
				return parts;
			}

			parts.AddRange(
				currentPath.Split(
					'/',
					StringSplitOptions.RemoveEmptyEntries
				)
			);

			return parts;
		}

		private void BuildVirtualFileSystem()
		{
			rootDirectory = new ConsoleDirectory();

			ConsoleDirectory sys = rootDirectory.GetOrCreateDirectory("sys");
			ConsoleDirectory core = sys.GetOrCreateDirectory("core");
			ConsoleDirectory kernel = sys.GetOrCreateDirectory("kernel");

			ConsoleDirectory dev = rootDirectory.GetOrCreateDirectory("dev");
			ConsoleDirectory log = rootDirectory.GetOrCreateDirectory("log");
			ConsoleDirectory home = rootDirectory.GetOrCreateDirectory("home");
			ConsoleDirectory user = home.GetOrCreateDirectory("user");

			core.Files["readme.txt"] =
				"System Core controls OS-wide metrics, boot policy and module routing.";

			kernel.Files["scheduler.os"] =
				"module Scheduler\n\nqueue processes\nrun next process every tick";

			dev.Files["sound0"] =
				"Sound device placeholder. Driver status: missing.";

			dev.Files["net0"] =
				"Network adapter placeholder. Link speed depends on hardware era.";

			log.Files["boot.log"] =
				"[OK] System Core initialized.\n[WARN] Sound driver missing.\n[OK] Console online.";

			user.Files["notes.txt"] =
				"TODO:\n- write better scheduler\n- add sound driver\n- reduce boot time";

			currentDirectory = rootDirectory;
			currentPath = "/";
		}

		private static List<string> SplitArguments(string commandLine)
		{
			List<string> args = new();
			StringBuilder current = new();

			bool insideQuotes = false;

			for (int i = 0; i < commandLine.Length; i++)
			{
				char c = commandLine[i];

				if (c == '"')
				{
					insideQuotes = !insideQuotes;
					continue;
				}

				if (char.IsWhiteSpace(c) && !insideQuotes)
				{
					if (current.Length > 0)
					{
						args.Add(current.ToString());
						current.Clear();
					}

					continue;
				}

				current.Append(c);
			}

			if (current.Length > 0)
			{
				args.Add(current.ToString());
			}

			return args;
		}

		private sealed class ConsoleDirectory
		{
			public readonly Dictionary<string, ConsoleDirectory> Directories =
				new(StringComparer.OrdinalIgnoreCase);

			public readonly Dictionary<string, string> Files =
				new(StringComparer.OrdinalIgnoreCase);

			public ConsoleDirectory GetOrCreateDirectory(string name)
			{
				if (!Directories.TryGetValue(name, out ConsoleDirectory directory))
				{
					directory = new ConsoleDirectory();
					Directories.Add(name, directory);
				}

				return directory;
			}
		}
	}
}
