using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeCodeTabsController : MonoBehaviour
	{
		[Header("Editor")]
		[SerializeField] private IdeInputCodeEditor codeEditor;

		[Header("Tabs")]
		[SerializeField] private RectTransform tabsRoot;
		[SerializeField] private IdeCodeTabButtonView tabPrefab;

		[Header("Visual")]
		[SerializeField] private Color selectedBackground = new(0.12f, 0.14f, 0.20f, 1f);
		[SerializeField] private Color normalBackground = new(0.03f, 0.03f, 0.05f, 1f);
		[SerializeField] private Color selectedText = Color.white;
		[SerializeField] private Color normalText = new(0.65f, 0.65f, 0.65f, 1f);

		[Header("Initial Documents")]
		[SerializeField] private InitialCodeDocument[] initialDocuments;

		private readonly List<CodeDocumentState> documents = new();
		private readonly Dictionary<string, IdeCodeTabButtonView> tabViewsById =
			new(StringComparer.OrdinalIgnoreCase);

		private string activeDocumentId;

		public string ActiveDocumentId => activeDocumentId;

		private void Awake()
		{
			BuildInitialDocuments();
		}

		private void Start()
		{
			if (documents.Count > 0)
			{
				SelectDocument(documents[0].Id);
			}
			else
			{
				ClearEditor();
			}
		}

		public void OpenDocument(string documentId, string title, string code)
		{
			if (string.IsNullOrWhiteSpace(documentId))
			{
				Debug.LogWarning($"[{nameof(IdeCodeTabsController)}] Document id is empty.", this);
				return;
			}

			CodeDocumentState existing = FindDocument(documentId);

			if (existing != null)
			{
				SelectDocument(existing.Id);
				return;
			}

			CodeDocumentState document = new CodeDocumentState(
				documentId,
				string.IsNullOrWhiteSpace(title) ? documentId : title,
				code ?? string.Empty
			);

			documents.Add(document);
			CreateTab(document);
			SelectDocument(document.Id);
		}

		public void OpenDocumentWithDefaultCode(string documentId, string title)
		{
			OpenDocument(documentId, title, GetDefaultCode(documentId));
		}

		public void SelectDocument(string documentId)
		{
			CodeDocumentState document = FindDocument(documentId);

			if (document == null)
			{
				return;
			}

			SaveActiveDocument();

			activeDocumentId = document.Id;

			if (codeEditor != null)
			{
				codeEditor.SetTitle(document.Title);
				codeEditor.SetCode(document.Code);
			}

			RefreshTabVisuals();
		}

		public void CloseDocument(string documentId)
		{
			CodeDocumentState document = FindDocument(documentId);

			if (document == null)
			{
				return;
			}

			bool closingActive = string.Equals(
				activeDocumentId,
				document.Id,
				StringComparison.OrdinalIgnoreCase
			);

			int index = documents.IndexOf(document);

			documents.Remove(document);

			if (tabViewsById.TryGetValue(document.Id, out IdeCodeTabButtonView tabView))
			{
				tabViewsById.Remove(document.Id);

				if (tabView != null)
				{
					Destroy(tabView.gameObject);
				}
			}

			if (!closingActive)
			{
				RefreshTabVisuals();
				return;
			}

			activeDocumentId = string.Empty;

			if (documents.Count == 0)
			{
				ClearEditor();
				return;
			}

			int nextIndex = Mathf.Clamp(index, 0, documents.Count - 1);
			SelectDocument(documents[nextIndex].Id);
		}

		public void SaveActiveDocument()
		{
			if (string.IsNullOrWhiteSpace(activeDocumentId) || codeEditor == null)
			{
				return;
			}

			CodeDocumentState active = FindDocument(activeDocumentId);

			if (active == null)
			{
				return;
			}

			active.Code = codeEditor.GetCode();
		}

		public string GetActiveCode()
		{
			return codeEditor != null
				? codeEditor.GetCode()
				: string.Empty;
		}

		private void BuildInitialDocuments()
		{
			ClearTabs();

			documents.Clear();
			activeDocumentId = string.Empty;

			if (initialDocuments == null || initialDocuments.Length == 0)
			{
				CodeDocumentState systemCore = new(
					"system_core",
					"System Core",
					GetDefaultCode("system_core")
				);

				documents.Add(systemCore);
				CreateTab(systemCore);
				return;
			}

			foreach (InitialCodeDocument initialDocument in initialDocuments)
			{
				if (string.IsNullOrWhiteSpace(initialDocument.Id))
				{
					continue;
				}

				string code = string.IsNullOrWhiteSpace(initialDocument.Code)
					? GetDefaultCode(initialDocument.Id)
					: initialDocument.Code;

				CodeDocumentState document = new(
					initialDocument.Id,
					string.IsNullOrWhiteSpace(initialDocument.Title)
						? initialDocument.Id
						: initialDocument.Title,
					code
				);

				documents.Add(document);
				CreateTab(document);
			}
		}

		private void CreateTab(CodeDocumentState document)
		{
			if (tabsRoot == null || tabPrefab == null)
			{
				return;
			}

			IdeCodeTabButtonView tabView = Instantiate(tabPrefab, tabsRoot);
			tabView.gameObject.SetActive(true);

			tabView.Bind(
				document.Id,
				document.Title,
				() => SelectDocument(document.Id),
				() => CloseDocument(document.Id)
			);

			tabViewsById[document.Id] = tabView;
		}

		private void RefreshTabVisuals()
		{
			foreach (CodeDocumentState document in documents)
			{
				if (!tabViewsById.TryGetValue(document.Id, out IdeCodeTabButtonView tabView))
				{
					continue;
				}

				bool selected = string.Equals(
					document.Id,
					activeDocumentId,
					StringComparison.OrdinalIgnoreCase
				);

				tabView.SetSelected(
					selected,
					selectedBackground,
					normalBackground,
					selectedText,
					normalText
				);
			}
		}

		private CodeDocumentState FindDocument(string documentId)
		{
			string targetKey = NormalizeDocumentId(documentId);

			foreach (CodeDocumentState document in documents)
			{
				if (NormalizeDocumentId(document.Id) == targetKey)
				{
					return document;
				}
			}

			return null;
		}

		private static string NormalizeDocumentId(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);

			foreach (char character in value)
			{
				if (char.IsLetterOrDigit(character))
				{
					builder.Append(char.ToLowerInvariant(character));
				}
			}

			return builder.ToString();
		}

		private void ClearTabs()
		{
			foreach (IdeCodeTabButtonView tabView in tabViewsById.Values)
			{
				if (tabView != null)
				{
					Destroy(tabView.gameObject);
				}
			}

			tabViewsById.Clear();
		}

		private void ClearEditor()
		{
			if (codeEditor == null)
			{
				return;
			}

			codeEditor.SetTitle("No File");
			codeEditor.SetCode(string.Empty);
		}

		private static string GetDefaultCode(string documentId)
		{
			switch ((documentId ?? string.Empty).ToLowerInvariant())
			{
				case "system_core":
					return
@"module SystemCore

stability += 12
network += data.output
security += firewall.level

if load > 80
    alert ""High Load""
end";

				case "kernel":
					return
@"module Kernel

require SystemCore as core

scheduler {
    mode = round_robin
    timeSlice = 12ms
}

if processes > 32
    alert ""Process Pressure""
end";

				case "data":
					return
@"module Data

require SystemCore as core

mount fileSystem
enable index

if storageUsage > 80
    alert ""Storage Pressure""
end";

				case "security":
					return
@"module Security

require SystemCore as core

load firewall
scan incomingPackets every 5s

if threats > 0
    alert ""Threat Detected""
end";

				case "control":
					return
@"module Control

require SystemCore as core

bind display
bind devices

if inputDelay > 25
    alert ""Input Delay""
end";

				default:
					return $"module {ToModuleName(documentId)}\n\n// Write module logic here";
			}
		}

		private static string ToModuleName(string documentId)
		{
			if (string.IsNullOrWhiteSpace(documentId))
			{
				return "NewModule";
			}

			string[] parts = documentId.Split('_');
			string result = string.Empty;

			foreach (string part in parts)
			{
				if (string.IsNullOrWhiteSpace(part))
				{
					continue;
				}

				result += char.ToUpperInvariant(part[0]) + part[1..];
			}

			return string.IsNullOrWhiteSpace(result)
				? "NewModule"
				: result;
		}

		[Serializable]
		private sealed class InitialCodeDocument
		{
			[SerializeField] private string id;
			[SerializeField] private string title;

			[SerializeField, TextArea(8, 32)]
			private string code;

			public string Id => id;
			public string Title => title;
			public string Code => code;
		}

		private sealed class CodeDocumentState
		{
			public string Id { get; }
			public string Title { get; }
			public string Code { get; set; }

			public CodeDocumentState(string id, string title, string code)
			{
				Id = id;
				Title = title;
				Code = code;
			}
		}
	}
}