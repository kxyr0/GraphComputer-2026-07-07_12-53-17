using System;
using System.Collections.Generic;
using System.Text;
using Game.Research;
using Game.Research.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace Game.UI.Research
{
	[DisallowMultipleComponent]
	public sealed class ResearchTreePresenter : MonoBehaviour
	{
		[SerializeField] private ResearchConfig config;
		[SerializeField] private ResearchService researchService;

		[Header("Per Tab HUD Roots")]
		[SerializeField] private ResearchTabHudRoot[] tabHudRoots;

		[Header("Templates")]
		[SerializeField] private ResearchButtonView researchButtonTemplate;
		[SerializeField] private ResearchLinkLineView linkLineTemplate;
		[SerializeField] private bool hideSceneButtonTemplate = true;

		[Header("HUD Tooltip")]
		[SerializeField] private bool enableTooltips = true;
		[SerializeField] private ResearchTooltipView tooltipView;

		[Header("Grid")]
		[SerializeField] private ResearchGridLayout gridLayout = new();
		[SerializeField] private ResearchScrollContentLayout scrollContentLayout = new();
		[SerializeField] private string tabId = "system_core";

		[Header("Lines")]
		[SerializeField] private Color dependencyLineColor = Color.white;
		[SerializeField] private Color mutualExclusionLineColor = Color.red;

		[Header("Behaviour")]
		[SerializeField] private bool rebuildOnEnable = true;

		[Header("Diagnostics")]
		[SerializeField] private bool logDiagnostics = true;

		private readonly Dictionary<string, ResearchButtonView> buttonViews =
			new(StringComparer.OrdinalIgnoreCase);

		private readonly List<GameObject> generatedObjects = new();
		private IResearchService service;
		private ResearchTabHudRoot currentTabRoot;

		[Inject]
		private void Construct(ResearchService sceneResearchService)
		{
			if (researchService == null)
			{
				researchService = sceneResearchService;
			}
		}

		private void Awake()
		{
			ConfigureService();
		}

		private void OnEnable()
		{
			ConfigureService();

			if (service != null)
			{
				service.Changed += HandleResearchChanged;
			}

			if (rebuildOnEnable)
			{
				Rebuild();
			}
			else
			{
				RefreshStates();
			}
		}

		private void OnDisable()
		{
			if (service != null)
			{
				service.Changed -= HandleResearchChanged;
			}
		}

		public void SetTab(string researchTabId)
		{
			tabId = researchTabId ?? string.Empty;
			Rebuild();
		}

		[ContextMenu("Rebuild Research Tree")]
		public void Rebuild()
		{
			ClearGeneratedObjects();
			ConfigureService();
			LogInfo($"Rebuild started for tab '{FormatTabId(tabId)}'.");

			if (config == null)
			{
				LogError("Research config is not assigned.");
				return;
			}

			currentTabRoot = ResolveTabRoot(tabId);

			if (currentTabRoot == null)
			{
				LogError(
					$"No HUD root is configured for tab '{FormatTabId(tabId)}'. " +
					$"Configured root ids: {BuildConfiguredTabRootList()}.");
				return;
			}

			if (currentTabRoot.ButtonsRoot == null)
			{
				LogError($"Buttons Root is not assigned for tab '{currentTabRoot.TabId}'.");
				return;
			}

			ResearchButtonView template = ResolveResearchButtonTemplate();

			if (template == null)
			{
				LogError(
					$"Research Button Template is not assigned for tab '{currentTabRoot.TabId}'. " +
					"Assign an object that already has ResearchButtonView.");
				return;
			}

			PrepareSceneTemplate(template);
			ApplyScrollContentLayout(template);

			int createdButtons = CreateResearchButtons(template, currentTabRoot.ButtonsRoot);

			if (createdButtons == 0)
			{
				LogWarning(
					$"No researches found for tab '{FormatTabId(tabId)}'. " +
					$"ResearchConfig tab ids: {BuildConfigTabList()}.");
				return;
			}

			int createdLines = CreateLinkLines();
			RefreshStates();
			LogInfo(
				$"Rebuild completed for tab '{FormatTabId(tabId)}': " +
				$"{createdButtons} button(s), {createdLines} line(s).");
		}

		public void RefreshStates()
		{
			foreach (KeyValuePair<string, ResearchButtonView> pair in buttonViews)
			{
				if (pair.Value == null)
				{
					continue;
				}

				ResearchState state = service != null
					? service.GetState(pair.Key)
					: ResearchState.CreateInactive(pair.Key, ResearchStatus.Locked);

				pair.Value.ApplyState(state);
			}
		}

		private int CreateResearchButtons(ResearchButtonView template, RectTransform parent)
		{
			int createdCount = 0;

			foreach (ResearchDefinition research in config.Researches)
			{
				if (research == null || !research.IsInTab(tabId))
				{
					continue;
				}

				if (string.IsNullOrWhiteSpace(research.Id))
				{
					LogWarning($"Skipped research with empty id in tab '{FormatTabId(tabId)}'.");
					continue;
				}

				ResearchButtonView view = CreateButtonView(research, template, parent);

				if (view == null)
				{
					continue;
				}

				buttonViews[research.Id] = view;
				createdCount++;
			}

			return createdCount;
		}

		private ResearchButtonView CreateButtonView(
			ResearchDefinition research,
			ResearchButtonView template,
			RectTransform parent)
		{
			ResearchButtonView view = Instantiate(template, parent, false);
			GameObject buttonObject = view.gameObject;

			buttonObject.name = $"Research_{research.Id}";
			buttonObject.SetActive(true);
			generatedObjects.Add(buttonObject);

			RectTransform rectTransform = buttonObject.transform as RectTransform;

			if (rectTransform != null)
			{
				rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
				rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
				rectTransform.pivot = new Vector2(0.5f, 0.5f);
				rectTransform.anchoredPosition = gridLayout.GetAnchoredPosition(research.GridPosition);
				rectTransform.localScale = Vector3.one;
				rectTransform.localRotation = Quaternion.identity;
			}

			view.Initialize(research, HandleResearchClicked);
			InitializeTooltip(view);

			return view;
		}

		private int CreateLinkLines()
		{
			HashSet<string> createdMutualLinks = new(StringComparer.OrdinalIgnoreCase);
			int createdCount = 0;

			foreach (ResearchDefinition research in config.Researches)
			{
				if (research == null || !research.IsInTab(tabId) || !buttonViews.ContainsKey(research.Id))
				{
					continue;
				}

				foreach (string prerequisiteId in research.PrerequisiteIds)
				{
					if (!string.IsNullOrWhiteSpace(prerequisiteId) &&
						buttonViews.ContainsKey(prerequisiteId))
					{
						if (CreateLinkLine(prerequisiteId, research.Id, dependencyLineColor))
						{
							createdCount++;
						}
					}
					else if (!string.IsNullOrWhiteSpace(prerequisiteId))
					{
						LogWarning(
							$"Dependency line skipped: '{research.Id}' requires '{prerequisiteId}', " +
							$"but the prerequisite is not present in tab '{FormatTabId(tabId)}'.");
					}
				}

				foreach (string excludedId in research.MutualExclusionIds)
				{
					if (string.IsNullOrWhiteSpace(excludedId) ||
						!buttonViews.ContainsKey(excludedId))
					{
						if (!string.IsNullOrWhiteSpace(excludedId))
						{
							LogWarning(
								$"Mutual exclusion line skipped: '{research.Id}' excludes '{excludedId}', " +
								$"but the excluded research is not present in tab '{FormatTabId(tabId)}'.");
						}

						continue;
					}

					string linkId = CreateCanonicalLinkId(research.Id, excludedId);

					if (!createdMutualLinks.Add(linkId))
					{
						continue;
					}

					if (CreateLinkLine(research.Id, excludedId, mutualExclusionLineColor))
					{
						createdCount++;
					}
				}
			}

			return createdCount;
		}

		private bool CreateLinkLine(string fromResearchId, string toResearchId, Color color)
		{
			if (!buttonViews.TryGetValue(fromResearchId, out ResearchButtonView fromView) ||
				!buttonViews.TryGetValue(toResearchId, out ResearchButtonView toView) ||
				fromView == null ||
				toView == null)
			{
				return false;
			}

			if (currentTabRoot.LinkLinesRoot == null)
			{
				LogError($"Link Lines Root is not assigned for tab '{currentTabRoot.TabId}'.");
				return false;
			}

			if (linkLineTemplate == null)
			{
				LogError("Link Line Template is not assigned.");
				return false;
			}

			ResearchLinkLineView lineView = Instantiate(linkLineTemplate, currentTabRoot.LinkLinesRoot, false);

			lineView.name = $"ResearchLine_{fromResearchId}_To_{toResearchId}";
			generatedObjects.Add(lineView.gameObject);

			if (lineView.transform is RectTransform rectTransform)
			{
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
				rectTransform.localScale = Vector3.one;
				rectTransform.localRotation = Quaternion.identity;
			}

			lineView.Bind(fromView.RectTransform, toView.RectTransform, color);
			return true;
		}

		private void HandleResearchClicked(string researchId)
		{
			ConfigureService();

			if (service != null && service.TryStartResearch(researchId))
			{
				RefreshStates();
				return;
			}

			LogWarning($"Research '{researchId}' was clicked but could not be started.");
		}

		private void HandleResearchChanged()
		{
			RefreshStates();
		}

		private void ConfigureService()
		{
			service = researchService;

			if (researchService == null)
			{
				LogWarning("Research Service is not assigned. Buttons will render, but clicks cannot start research.");
				return;
			}

			if (researchService.Config == null)
			{
				LogWarning("Research Service Config is not assigned. Buttons will render, but clicks cannot start research.");
			}
			else if (config != null && researchService.Config != config)
			{
				LogWarning("ResearchTreePresenter Config and ResearchService Config are different assets.");
			}
		}

		private void PrepareSceneTemplate(ResearchButtonView template)
		{
			if (!hideSceneButtonTemplate || template == null)
			{
				return;
			}

			Scene templateScene = template.gameObject.scene;

			if (templateScene.IsValid())
			{
				template.gameObject.SetActive(false);
			}
		}

		private void ApplyScrollContentLayout(ResearchButtonView template)
		{
			if (scrollContentLayout == null || currentTabRoot == null)
			{
				return;
			}

			if (currentTabRoot.ScrollRect != null && currentTabRoot.ScrollContentRoot == null)
			{
				LogWarning(
					$"Scroll Rect is assigned for tab '{currentTabRoot.TabId}', " +
					"but Scroll Content Root is not assigned. Buttons Root will be used as content.");
			}

			scrollContentLayout.Apply(
				currentTabRoot.ContentRoot,
				currentTabRoot.ButtonsRoot,
				currentTabRoot.LinkLinesRoot,
				currentTabRoot.ScrollRect,
				config,
				tabId,
				gridLayout,
				GetTemplateSize(template),
				LogWarning
			);
		}

		private void InitializeTooltip(ResearchButtonView view)
		{
			if (!enableTooltips || view == null)
			{
				return;
			}

			if (tooltipView == null)
			{
				LogWarning("Tooltips are enabled, but Tooltip View is not assigned.");
				return;
			}

			view.InitializeTooltip(tooltipView);
		}

		private void ClearGeneratedObjects()
		{
			for (int i = generatedObjects.Count - 1; i >= 0; i--)
			{
				GameObject generatedObject = generatedObjects[i];

				if (generatedObject == null)
				{
					continue;
				}

				if (Application.isPlaying)
				{
					Destroy(generatedObject);
				}
				else
				{
					DestroyImmediate(generatedObject);
				}
			}

			generatedObjects.Clear();
			buttonViews.Clear();
		}

		private static string CreateCanonicalLinkId(string firstResearchId, string secondResearchId)
		{
			return string.Compare(firstResearchId, secondResearchId, StringComparison.OrdinalIgnoreCase) <= 0
				? $"{firstResearchId}|{secondResearchId}"
				: $"{secondResearchId}|{firstResearchId}";
		}

		private static Vector2 GetTemplateSize(ResearchButtonView template)
		{
			if (template != null && template.RectTransform != null)
			{
				Vector2 rectSize = template.RectTransform.rect.size;

				if (rectSize.x > 0f && rectSize.y > 0f)
				{
					return rectSize;
				}

				Vector2 sizeDelta = template.RectTransform.sizeDelta;

				if (sizeDelta.x > 0f && sizeDelta.y > 0f)
				{
					return sizeDelta;
				}
			}

			return new Vector2(220f, 88f);
		}

		private ResearchButtonView ResolveResearchButtonTemplate()
		{
			if (currentTabRoot != null && currentTabRoot.ResearchButtonTemplate != null)
			{
				return currentTabRoot.ResearchButtonTemplate;
			}

			return researchButtonTemplate;
		}

		private ResearchTabHudRoot ResolveTabRoot(string researchTabId)
		{
			if (tabHudRoots == null)
			{
				return null;
			}

			foreach (ResearchTabHudRoot root in tabHudRoots)
			{
				if (root != null && root.Matches(researchTabId))
				{
					return root;
				}
			}

			return null;
		}

		private string BuildConfiguredTabRootList()
		{
			if (tabHudRoots == null || tabHudRoots.Length == 0)
			{
				return "<none>";
			}

			StringBuilder builder = new();

			foreach (ResearchTabHudRoot root in tabHudRoots)
			{
				if (root == null)
				{
					continue;
				}

				AppendCommaSeparated(builder, FormatTabId(root.TabId));
			}

			return builder.Length > 0 ? builder.ToString() : "<none>";
		}

		private string BuildConfigTabList()
		{
			if (config == null || config.Researches.Length == 0)
			{
				return "<none>";
			}

			HashSet<string> tabIds = new(StringComparer.OrdinalIgnoreCase);

			foreach (ResearchDefinition research in config.Researches)
			{
				if (research != null)
				{
					tabIds.Add(FormatTabId(research.TabId));
				}
			}

			StringBuilder builder = new();

			foreach (string configTabId in tabIds)
			{
				AppendCommaSeparated(builder, configTabId);
			}

			return builder.Length > 0 ? builder.ToString() : "<none>";
		}

		private static void AppendCommaSeparated(StringBuilder builder, string value)
		{
			if (builder.Length > 0)
			{
				builder.Append(", ");
			}

			builder.Append(value);
		}

		private static string FormatTabId(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? "<empty>" : value;
		}

		private void LogInfo(string message)
		{
			if (logDiagnostics)
			{
				Debug.Log($"[{nameof(ResearchTreePresenter)}] {message}", this);
			}
		}

		private void LogWarning(string message)
		{
			Debug.LogWarning($"[{nameof(ResearchTreePresenter)}] {message}", this);
		}

		private void LogError(string message)
		{
			Debug.LogError($"[{nameof(ResearchTreePresenter)}] {message}", this);
		}
	}

	[Serializable]
	public sealed class ResearchTabHudRoot
	{
		[SerializeField] private string tabId;
		[SerializeField] private ScrollRect scrollRect;
		[SerializeField] private RectTransform scrollContentRoot;
		[SerializeField] private RectTransform buttonsRoot;
		[SerializeField] private RectTransform linkLinesRoot;
		[SerializeField] private ResearchButtonView researchButtonTemplate;

		public string TabId => tabId ?? string.Empty;
		public ScrollRect ScrollRect => scrollRect;
		public RectTransform ScrollContentRoot => scrollContentRoot;
		public RectTransform ContentRoot => scrollContentRoot != null ? scrollContentRoot : buttonsRoot;
		public RectTransform ButtonsRoot => buttonsRoot;
		public RectTransform LinkLinesRoot => linkLinesRoot;
		public ResearchButtonView ResearchButtonTemplate => researchButtonTemplate;

		public bool Matches(string researchTabId)
		{
			return string.Equals(tabId, researchTabId, StringComparison.OrdinalIgnoreCase);
		}
	}
}
