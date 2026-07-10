using System;
using System.Collections.Generic;
using Game.Nodes.Composition;
using Game.Nodes.Definitions;
using Game.Selection.Categories.Runtime;
using Game.Selection.Runtime;
using Game.UI.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	[DisallowMultipleComponent]
	public sealed class NodeGraphContextMenuController : MonoBehaviour, IPointerClickHandler
	{
		private const string BackCategoryId = "__back";
		private const string CreateSelectionCategoryId = "__create_selection_category";

		[SerializeField] private NodeCatalog nodeCatalog;
		[SerializeField] private NodeFactory nodeFactory;
		[SerializeField] private NodeSelectionService selectionService;
		[SerializeField] private NodeCategoryFrameService categoryFrameService;

		[Header("Graph")]
		[SerializeField] private RectTransform nodesRoot;

		[Header("Menu")]
		[SerializeField] private RectTransform menuPanel;
		[SerializeField] private RectTransform itemsRoot;
		[SerializeField] private NodeSpawnMenuItemView itemPrefab;
		[SerializeField] private NodeSpawnCategoryItemView categoryPrefab;
		[SerializeField] private bool hideAfterSpawn = true;

		[Header("Categories")]
		[SerializeField] private bool useCategories = true;
		[SerializeField] private bool autoOpenSingleCategory = true;
		[SerializeField] private bool showBackButton = true;
		[SerializeField] private bool showSelectionCategoryAction = true;
		[SerializeField] private bool openSelectionMenuFromAnyRightClick = true;
		[SerializeField, Min(2)] private int minSelectedNodesForCategory = 2;

		[Header("Animation")]
		[SerializeField] private NodePopupScaleAnimator popupAnimator;

		private readonly List<Component> spawnedViews = new List<Component>();

		private Canvas canvas;
		private CanvasGroup menuCanvasGroup;
		private Camera lastEventCamera;
		private Vector2 pendingSpawnPosition;
		private bool isMenuOpen;

		private string activeCategoryId;

		private GeneratedNodePortView pendingConnectionSourcePort;
		private NodeConnectionDragController pendingConnectionController;
		private int lastShowFrame = -1;

		private void Awake()
		{
			canvas = GetComponentInParent<Canvas>();
			ResolveSelectionServices();

			if (menuPanel != null)
			{
				menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();

				if (menuCanvasGroup == null)
				{
					menuCanvasGroup = menuPanel.gameObject.AddComponent<CanvasGroup>();
				}
			}

			if (popupAnimator == null && menuPanel != null)
			{
				popupAnimator = menuPanel.GetComponent<NodePopupScaleAnimator>();
			}

			HideTemplateWithoutDisabling();
			HideImmediate();
		}

		private void Update()
		{
			if (menuPanel == null)
			{
				return;
			}

			if (!isMenuOpen)
			{
				TryOpenSelectionMenuFromRightClick();
				return;
			}

			if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
			{
				Hide();
				return;
			}

			if (Mouse.current == null)
			{
				return;
			}

			bool clicked =
				Mouse.current.leftButton.wasPressedThisFrame ||
				Mouse.current.rightButton.wasPressedThisFrame;

			if (!clicked)
			{
				return;
			}

			Vector2 mousePosition = Mouse.current.position.ReadValue();

			bool clickedInsideMenu = RectTransformUtility.RectangleContainsScreenPoint(
				menuPanel,
				mousePosition,
				lastEventCamera
			);

			if (!clickedInsideMenu)
			{
				Hide();
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (Game.Runtime.Input.GameInputBlocker.IsBlocked)
			{
				return;
			}

			if (eventData.button != PointerEventData.InputButton.Right)
			{
				return;
			}

			if (lastShowFrame == Time.frameCount)
			{
				return;
			}

			ClearPendingConnection();

			Camera eventCamera = GetEventCamera(eventData);
			Show(eventData.position, eventCamera);
		}

		public void ShowForLooseConnection(
			GeneratedNodePortView sourcePort,
			Vector2 screenPosition,
			Camera eventCamera,
			NodeConnectionDragController connectionController)
		{
			if (sourcePort == null || connectionController == null)
			{
				return;
			}

			pendingConnectionSourcePort = sourcePort;
			pendingConnectionController = connectionController;

			Show(screenPosition, eventCamera);
		}

		private void Show(Vector2 screenPosition, Camera eventCamera)
		{
			if (nodeCatalog == null || nodeFactory == null || nodesRoot == null)
			{
				Debug.LogError($"[{nameof(NodeGraphContextMenuController)}] Missing catalog, factory, or nodes root.", this);
				return;
			}

			if (menuPanel == null || itemsRoot == null || itemPrefab == null)
			{
				Debug.LogError($"[{nameof(NodeGraphContextMenuController)}] Missing menu panel, items root, or item prefab.", this);
				return;
			}

			lastEventCamera = eventCamera;
			lastShowFrame = Time.frameCount;
			activeCategoryId = string.Empty;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				nodesRoot,
				screenPosition,
				eventCamera,
				out pendingSpawnPosition
			);

			BuildMenu();

			isMenuOpen = true;

			menuPanel.localScale = Vector3.one;
			menuPanel.SetAsLastSibling();

			MoveMenuToScreenPosition(screenPosition, eventCamera);

			Canvas.ForceUpdateCanvases();
			ClampMenuToParent();

			if (popupAnimator != null)
			{
				popupAnimator.Show();
			}
			else if (menuCanvasGroup != null)
			{
				menuCanvasGroup.alpha = 1f;
				menuCanvasGroup.interactable = true;
				menuCanvasGroup.blocksRaycasts = true;
			}
		}

		private void BuildMenu()
		{
			ClearItems();

			IReadOnlyList<NodeDefinition> definitions = nodeCatalog.Definitions;

			if (definitions == null)
			{
				return;
			}

			List<NodeDefinition> visibleDefinitions = CollectVisibleDefinitions(definitions);

			if (!useCategories || categoryPrefab == null)
			{
				BuildFlatMenu(visibleDefinitions);
				return;
			}

			List<NodeSpawnCategoryGroup> groups = BuildGroups(visibleDefinitions);

			if (groups.Count == 1 && autoOpenSingleCategory)
			{
				activeCategoryId = groups[0].Category.Id;
			}

			if (string.IsNullOrWhiteSpace(activeCategoryId))
			{
				BuildCategoryRoot(groups);
				return;
			}

			BuildCategoryContent(groups, activeCategoryId);
		}

		private List<NodeDefinition> CollectVisibleDefinitions(IReadOnlyList<NodeDefinition> definitions)
		{
			List<NodeDefinition> result = new List<NodeDefinition>();

			foreach (NodeDefinition definition in definitions)
			{
				if (definition == null)
				{
					continue;
				}

				if (!CanShowNodeInCurrentMenu(definition))
				{
					continue;
				}

				result.Add(definition);
			}

			return result;
		}

		private void BuildFlatMenu(List<NodeDefinition> definitions)
		{
			foreach (NodeDefinition definition in definitions)
			{
				CreateNodeItem(definition);
			}
		}

		private List<NodeSpawnCategoryGroup> BuildGroups(List<NodeDefinition> definitions)
		{
			Dictionary<string, NodeSpawnCategoryGroup> groupsById =
				new Dictionary<string, NodeSpawnCategoryGroup>(StringComparer.OrdinalIgnoreCase);

			foreach (NodeDefinition definition in definitions)
			{
				NodeSpawnCategory category = ResolveCategory(definition);

				if (!groupsById.TryGetValue(category.Id, out NodeSpawnCategoryGroup group))
				{
					group = new NodeSpawnCategoryGroup(category);
					groupsById.Add(category.Id, group);
				}

				group.Definitions.Add(definition);
			}

			List<NodeSpawnCategoryGroup> groups = new List<NodeSpawnCategoryGroup>(groupsById.Values);

			groups.Sort((a, b) => a.Category.Order.CompareTo(b.Category.Order));

			foreach (NodeSpawnCategoryGroup group in groups)
			{
				group.Definitions.Sort((a, b) => string.Compare(
					a.Title,
					b.Title,
					StringComparison.OrdinalIgnoreCase));
			}

			return groups;
		}

		private void BuildCategoryRoot(List<NodeSpawnCategoryGroup> groups)
		{
			if (CanShowSelectionCategoryAction())
			{
				CreateCategoryItem(
					CreateSelectionCategoryId,
					"Create Category",
					GetSelectedCount(),
					false,
					HandleCategoryClicked
				);
			}

			foreach (NodeSpawnCategoryGroup group in groups)
			{
				CreateCategoryItem(
					group.Category.Id,
					group.Category.Title,
					group.Definitions.Count,
					false,
					HandleCategoryClicked
				);
			}
		}

		private void BuildCategoryContent(List<NodeSpawnCategoryGroup> groups, string categoryId)
		{
			NodeSpawnCategoryGroup group = FindGroup(groups, categoryId);

			if (group == null)
			{
				activeCategoryId = string.Empty;
				BuildCategoryRoot(groups);
				return;
			}

			if (showBackButton)
			{
				CreateCategoryItem(
					BackCategoryId,
					"Categories",
					-1,
					false,
					HandleCategoryClicked
				);
			}

			foreach (NodeDefinition definition in group.Definitions)
			{
				CreateNodeItem(definition);
			}
		}

		private NodeSpawnCategoryGroup FindGroup(List<NodeSpawnCategoryGroup> groups, string categoryId)
		{
			foreach (NodeSpawnCategoryGroup group in groups)
			{
				if (string.Equals(
						group.Category.Id,
						categoryId,
						StringComparison.OrdinalIgnoreCase))
				{
					return group;
				}
			}

			return null;
		}

		private void CreateCategoryItem(
			string id,
			string title,
			int count,
			bool selected,
			Action<string> clicked)
		{
			NodeSpawnCategoryItemView item = Instantiate(categoryPrefab, itemsRoot);
			item.gameObject.name = $"Category_{id}";
			PrepareGeneratedItem(item.gameObject);

			item.Bind(
				id,
				title,
				count,
				selected,
				clicked
			);

			spawnedViews.Add(item);
		}

		private void CreateNodeItem(NodeDefinition definition)
		{
			NodeSpawnMenuItemView item = Instantiate(itemPrefab, itemsRoot);
			item.gameObject.name = $"SpawnItem_{definition.Id}";

			PrepareGeneratedItem(item.gameObject);
			item.Bind(definition, SpawnNodeFromMenu);

			spawnedViews.Add(item);
		}

		private void HandleCategoryClicked(string categoryId)
		{
			if (string.IsNullOrWhiteSpace(categoryId))
			{
				return;
			}

			if (categoryId == CreateSelectionCategoryId)
			{
				CreateCategoryFromSelection();
				return;
			}

			if (categoryId == BackCategoryId)
			{
				activeCategoryId = string.Empty;
			}
			else
			{
				activeCategoryId = categoryId;
			}

			BuildMenu();

			Canvas.ForceUpdateCanvases();
			ClampMenuToParent();
		}

		private void CreateCategoryFromSelection()
		{
			ResolveSelectionServices();

			if (categoryFrameService == null)
			{
				Debug.LogWarning(
					$"[{nameof(NodeGraphContextMenuController)}] Category frame service is missing.",
					this);

				return;
			}

			if (!categoryFrameService.TryCreateFromCurrentSelection())
			{
				return;
			}

			if (hideAfterSpawn)
			{
				Hide();
			}
		}

		private bool CanShowNodeInCurrentMenu(NodeDefinition definition)
		{
			if (pendingConnectionSourcePort == null)
			{
				return true;
			}

			return NodePortCompatibilityUtility.CanConnectToNode(
				pendingConnectionSourcePort,
				definition
			);
		}

		private bool CanShowSelectionCategoryAction()
		{
			if (!showSelectionCategoryAction || pendingConnectionSourcePort != null)
			{
				return false;
			}

			ResolveSelectionServices();

			if (selectionService == null || selectionService.SelectedCount < minSelectedNodesForCategory)
			{
				return false;
			}

			if (categoryPrefab == null)
			{
				return false;
			}

			return categoryFrameService != null &&
				   categoryFrameService.CanCreateFromCurrentSelection;
		}

		private int GetSelectedCount()
		{
			ResolveSelectionServices();
			return selectionService != null ? selectionService.SelectedCount : 0;
		}

		private void TryOpenSelectionMenuFromRightClick()
		{
			if (!openSelectionMenuFromAnyRightClick ||
				Mouse.current == null ||
				!Mouse.current.rightButton.wasPressedThisFrame)
			{
				return;
			}

			if (Game.Runtime.Input.GameInputBlocker.IsBlocked ||
				UiPointerBlockUtility.IsPointerOverBlockingUi())
			{
				return;
			}

			if (!CanShowSelectionCategoryAction())
			{
				return;
			}

			Vector2 screenPosition = Mouse.current.position.ReadValue();
			Show(screenPosition, GetCanvasEventCamera());
		}

		private void PrepareGeneratedItem(GameObject item)
		{
			if (item == null)
			{
				return;
			}

			item.SetActive(true);

			CanvasGroup group = item.GetComponent<CanvasGroup>();

			if (group == null)
			{
				group = item.AddComponent<CanvasGroup>();
			}

			group.alpha = 1f;
			group.interactable = true;
			group.blocksRaycasts = true;

			LayoutElement layoutElement = item.GetComponent<LayoutElement>();

			if (layoutElement == null)
			{
				layoutElement = item.AddComponent<LayoutElement>();
			}

			layoutElement.ignoreLayout = false;
		}

		private void SpawnNodeFromMenu(NodeDefinition definition)
		{
			if (definition == null)
			{
				return;
			}

			GeneratedNodePortView sourcePort = pendingConnectionSourcePort;
			NodeConnectionDragController connectionController = pendingConnectionController;

			NodeView spawnedNode = nodeFactory.Spawn(definition, pendingSpawnPosition);

			TryAttachPendingConnection(spawnedNode, sourcePort, connectionController);

			ClearPendingConnection();

			if (hideAfterSpawn)
			{
				Hide();
			}
		}

		private void TryAttachPendingConnection(
			NodeView spawnedNode,
			GeneratedNodePortView sourcePort,
			NodeConnectionDragController connectionController)
		{
			if (spawnedNode == null || sourcePort == null || connectionController == null)
			{
				return;
			}

			Canvas.ForceUpdateCanvases();

			if (!spawnedNode.TryGetCompatibleInputPort(sourcePort, out GeneratedNodePortView targetInputPort))
			{
				Debug.LogWarning(
					$"[{nameof(NodeGraphContextMenuController)}] Spawned node '{spawnedNode.name}' has no compatible input port.",
					this
				);

				return;
			}

			if (!connectionController.TryCreateConnection(sourcePort, targetInputPort))
			{
				Debug.LogWarning(
					$"[{nameof(NodeGraphContextMenuController)}] Failed to attach loose connection to '{spawnedNode.name}'.",
					this
				);
			}
		}

		private NodeSpawnCategory ResolveCategory(NodeDefinition definition)
		{
			string id = definition.Id?.ToLowerInvariant() ?? string.Empty;

			return id switch
			{
				"system_core" or "kernel" or "data" or "security" or "control"
					=> new NodeSpawnCategory("core", "Core", 0),

				"memory_manager" or "process_manager" or "scheduler" or "kernel_recovery"
					=> new NodeSpawnCategory("kernel", "Kernel", 10),

				"file_system" or "backup" or "compression" or "recovery"
					=> new NodeSpawnCategory("data", "Data", 20),

				"firewall" or "permissions" or "antivirus" or "encryption"
					=> new NodeSpawnCategory("security", "Security", 30),

				"display_manager" or "sound" or "devices" or "power"
					=> new NodeSpawnCategory("control", "Control", 40),

				"time" or "news" or "era_news" or "docs" or "user_reports"
					=> new NodeSpawnCategory("world", "World", 50),

				_ => new NodeSpawnCategory("other", "Other", 999)
			};
		}

		private void MoveMenuToScreenPosition(Vector2 screenPosition, Camera eventCamera)
		{
			RectTransform parent = menuPanel.parent as RectTransform;

			if (parent == null)
			{
				return;
			}

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				parent,
				screenPosition,
				eventCamera,
				out Vector2 localPosition
			);

			menuPanel.anchoredPosition = localPosition;
		}

		private void ClampMenuToParent()
		{
			RectTransform parent = menuPanel.parent as RectTransform;

			if (parent == null)
			{
				return;
			}

			Rect parentRect = parent.rect;
			Rect menuRect = menuPanel.rect;
			Vector2 position = menuPanel.anchoredPosition;
			Vector2 pivot = menuPanel.pivot;

			float minX = parentRect.xMin + menuRect.width * pivot.x;
			float maxX = parentRect.xMax - menuRect.width * (1f - pivot.x);
			float minY = parentRect.yMin + menuRect.height * pivot.y;
			float maxY = parentRect.yMax - menuRect.height * (1f - pivot.y);

			if (minX <= maxX)
			{
				position.x = Mathf.Clamp(position.x, minX, maxX);
			}

			if (minY <= maxY)
			{
				position.y = Mathf.Clamp(position.y, minY, maxY);
			}

			menuPanel.anchoredPosition = position;
		}

		private void Hide()
		{
			if (!isMenuOpen)
			{
				return;
			}

			isMenuOpen = false;
			activeCategoryId = string.Empty;
			ClearPendingConnection();

			if (popupAnimator != null)
			{
				popupAnimator.Hide(ClearItems);
				return;
			}

			if (menuCanvasGroup != null)
			{
				menuCanvasGroup.alpha = 0f;
				menuCanvasGroup.interactable = false;
				menuCanvasGroup.blocksRaycasts = false;
			}

			ClearItems();
		}

		private void HideImmediate()
		{
			isMenuOpen = false;
			activeCategoryId = string.Empty;
			ClearPendingConnection();

			if (popupAnimator != null)
			{
				popupAnimator.HideInstant();
			}
			else if (menuCanvasGroup != null)
			{
				menuCanvasGroup.alpha = 0f;
				menuCanvasGroup.interactable = false;
				menuCanvasGroup.blocksRaycasts = false;
			}

			ClearItems();
		}

		private void ClearItems()
		{
			for (int i = spawnedViews.Count - 1; i >= 0; i--)
			{
				Component view = spawnedViews[i];

				if (view == null)
				{
					continue;
				}

				Destroy(view.gameObject);
			}

			spawnedViews.Clear();
		}

		private void ClearPendingConnection()
		{
			pendingConnectionSourcePort = null;
			pendingConnectionController = null;
		}

		private void HideTemplateWithoutDisabling()
		{
			HideTemplate(itemPrefab != null ? itemPrefab.gameObject : null);
			HideTemplate(categoryPrefab != null ? categoryPrefab.gameObject : null);
		}

		private void HideTemplate(GameObject template)
		{
			if (template == null)
			{
				return;
			}

			template.SetActive(true);

			CanvasGroup group = template.GetComponent<CanvasGroup>();

			if (group == null)
			{
				group = template.AddComponent<CanvasGroup>();
			}

			group.alpha = 0f;
			group.interactable = false;
			group.blocksRaycasts = false;

			LayoutElement layoutElement = template.GetComponent<LayoutElement>();

			if (layoutElement == null)
			{
				layoutElement = template.AddComponent<LayoutElement>();
			}

			layoutElement.ignoreLayout = true;
		}

		private Camera GetEventCamera(PointerEventData eventData)
		{
			if (eventData.pressEventCamera != null)
			{
				return eventData.pressEventCamera;
			}

			if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
			{
				return canvas.worldCamera;
			}

			return null;
		}

		private Camera GetCanvasEventCamera()
		{
			if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
			{
				return canvas.worldCamera;
			}

			return null;
		}

		private void ResolveSelectionServices()
		{
			if (selectionService == null)
			{
				selectionService = FindFirstObjectByType<NodeSelectionService>();
			}

			if (categoryFrameService == null)
			{
				categoryFrameService = FindFirstObjectByType<NodeCategoryFrameService>();
			}
		}

		private readonly struct NodeSpawnCategory
		{
			public readonly string Id;
			public readonly string Title;
			public readonly int Order;

			public NodeSpawnCategory(string id, string title, int order)
			{
				Id = id;
				Title = title;
				Order = order;
			}
		}

		private sealed class NodeSpawnCategoryGroup
		{
			public readonly NodeSpawnCategory Category;
			public readonly List<NodeDefinition> Definitions = new List<NodeDefinition>();

			public NodeSpawnCategoryGroup(NodeSpawnCategory category)
			{
				Category = category;
			}
		}
	}
}
