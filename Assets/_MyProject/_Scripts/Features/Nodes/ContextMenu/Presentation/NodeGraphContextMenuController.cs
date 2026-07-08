using System.Collections.Generic;
using Game.Nodes.Composition;
using Game.Nodes.Definitions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	[DisallowMultipleComponent]
	public sealed class NodeGraphContextMenuController : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField] private NodeCatalog nodeCatalog;
		[SerializeField] private NodeFactory nodeFactory;

		[Header("Graph")]
		[SerializeField] private RectTransform nodesRoot;

		[Header("Menu")]
		[SerializeField] private RectTransform menuPanel;
		[SerializeField] private RectTransform itemsRoot;
		[SerializeField] private NodeSpawnMenuItemView itemPrefab;
		[SerializeField] private bool hideAfterSpawn = true;

		[Header("Animation")]
		[SerializeField] private NodePopupScaleAnimator popupAnimator;

		private readonly List<NodeSpawnMenuItemView> spawnedItems = new();

		private Canvas canvas;
		private CanvasGroup menuCanvasGroup;
		private Camera lastEventCamera;
		private Vector2 pendingSpawnPosition;
		private bool isMenuOpen;

		private GeneratedNodePortView pendingConnectionSourcePort;
		private NodeConnectionDragController pendingConnectionController;

		private void Awake()
		{
			canvas = GetComponentInParent<Canvas>();

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
			if (menuPanel == null || !isMenuOpen)
			{
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

				NodeSpawnMenuItemView item = Instantiate(itemPrefab, itemsRoot);
				item.gameObject.name = $"SpawnItem_{definition.Id}";

				PrepareGeneratedButton(item);
				item.Bind(definition, SpawnNodeFromMenu);

				spawnedItems.Add(item);
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

		private void PrepareGeneratedButton(NodeSpawnMenuItemView item)
		{
			if (item == null)
			{
				return;
			}

			item.gameObject.SetActive(true);

			CanvasGroup group = item.GetComponent<CanvasGroup>();

			if (group == null)
			{
				group = item.gameObject.AddComponent<CanvasGroup>();
			}

			group.alpha = 1f;
			group.interactable = true;
			group.blocksRaycasts = true;

			LayoutElement layoutElement = item.GetComponent<LayoutElement>();

			if (layoutElement == null)
			{
				layoutElement = item.gameObject.AddComponent<LayoutElement>();
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
			for (int i = spawnedItems.Count - 1; i >= 0; i--)
			{
				NodeSpawnMenuItemView item = spawnedItems[i];

				if (item == null)
				{
					continue;
				}

				Destroy(item.gameObject);
			}

			spawnedItems.Clear();
		}

		private void ClearPendingConnection()
		{
			pendingConnectionSourcePort = null;
			pendingConnectionController = null;
		}

		private void HideTemplateWithoutDisabling()
		{
			if (itemPrefab == null)
			{
				return;
			}

			itemPrefab.gameObject.SetActive(true);

			CanvasGroup templateGroup = itemPrefab.GetComponent<CanvasGroup>();

			if (templateGroup == null)
			{
				templateGroup = itemPrefab.gameObject.AddComponent<CanvasGroup>();
			}

			templateGroup.alpha = 0f;
			templateGroup.interactable = false;
			templateGroup.blocksRaycasts = false;

			LayoutElement layoutElement = itemPrefab.GetComponent<LayoutElement>();

			if (layoutElement == null)
			{
				layoutElement = itemPrefab.gameObject.AddComponent<LayoutElement>();
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
	}
}