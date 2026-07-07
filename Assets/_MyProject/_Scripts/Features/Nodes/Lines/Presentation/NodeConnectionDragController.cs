using System.Collections.Generic;
using Game.Nodes.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Nodes.Presentation
{
	public sealed class NodeConnectionDragController : MonoBehaviour
	{
		[SerializeField] private RectTransform connectionsRoot;
		[SerializeField] private NodeConnectionView connectionPrefab;
		[SerializeField] private NodeGraphContextMenuController contextMenuController;
		[SerializeField] private NodeGraphConnectionModel connectionModel;

		[Header("Colors")]
		[SerializeField] private Color previewColor = new(1f, 0.75f, 0.1f, 1f);
		[SerializeField] private Color validColor = new(0.1f, 1f, 0.95f, 1f);

		private readonly List<RuntimeConnection> runtimeConnections = new();

		private GeneratedNodePortView sourcePort;
		private GeneratedNodePortView hoveredPort;
		private NodeConnectionView previewConnection;
		private bool isDragging;

		private void Awake()
		{
			if (contextMenuController == null)
			{
				contextMenuController = FindFirstObjectByType<NodeGraphContextMenuController>();
			}

			if (connectionModel == null)
			{
				connectionModel = FindFirstObjectByType<NodeGraphConnectionModel>();
			}
		}

		public void BeginDrag(GeneratedNodePortView port, PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}

			if (port == null || isDragging)
			{
				return;
			}

			if (!port.IsOutput)
			{
				return;
			}

			if (connectionsRoot == null || connectionPrefab == null)
			{
				Debug.LogError($"[{nameof(NodeConnectionDragController)}] Connections root or prefab is missing.", this);
				return;
			}

			sourcePort = port;
			isDragging = true;

			previewConnection = CreateConnectionView("Connection_Preview");
			previewConnection.BindPreview(sourcePort.ConnectorRect, previewColor);
			previewConnection.SetPreviewEndScreen(eventData.position, eventData.pressEventCamera);
		}

		public void UpdateDrag(PointerEventData eventData)
		{
			if (!isDragging || previewConnection == null)
			{
				return;
			}

			previewConnection.SetPreviewEndScreen(eventData.position, eventData.pressEventCamera);
			previewConnection.color = IsValidTarget(sourcePort, hoveredPort) ? validColor : previewColor;
		}

		public void EndDrag(PointerEventData eventData)
		{
			if (!isDragging)
			{
				return;
			}

			if (IsValidTarget(sourcePort, hoveredPort))
			{
				TryCreateConnection(sourcePort, hoveredPort);
			}
			else
			{
				ShowContextMenuFromLooseLine(eventData);
			}

			DestroyPreviewConnection();

			sourcePort = null;
			previewConnection = null;
			isDragging = false;
		}

		public void SetHoveredPort(GeneratedNodePortView port)
		{
			hoveredPort = port;
		}

		public void ClearHoveredPort(GeneratedNodePortView port)
		{
			if (hoveredPort == port)
			{
				hoveredPort = null;
			}
		}

		public bool TryCreateConnection(GeneratedNodePortView from, GeneratedNodePortView to)
		{
			if (!IsValidTarget(from, to))
			{
				return false;
			}

			return CreateFinalConnection(from, to);
		}

		private void ShowContextMenuFromLooseLine(PointerEventData eventData)
		{
			if (sourcePort == null)
			{
				return;
			}

			if (contextMenuController == null)
			{
				Debug.LogWarning($"[{nameof(NodeConnectionDragController)}] Context menu controller is missing.", this);
				return;
			}

			contextMenuController.ShowForLooseConnection(
				sourcePort,
				eventData.position,
				eventData.pressEventCamera,
				this
			);
		}

		private bool IsValidTarget(GeneratedNodePortView from, GeneratedNodePortView to)
		{
			if (from == null || to == null)
			{
				return false;
			}

			if (from == to)
			{
				return false;
			}

			if (ConnectionExists(from, to))
			{
				return false;
			}

			return NodePortCompatibilityUtility.CanConnect(from, to);
		}

		private bool CreateFinalConnection(GeneratedNodePortView from, GeneratedNodePortView to)
		{
			NodeConnectionData data = CreateConnectionData(from, to);

			if (connectionModel != null && !connectionModel.TryAddConnection(data))
			{
				Debug.LogWarning(
					$"[{nameof(NodeConnectionDragController)}] Connection already exists in model: {data.Id}",
					this);

				return false;
			}

			NodeConnectionView connectionView = CreateConnectionView(
				$"Connection_{data.FromNodeId}_{data.FromPortId}_To_{data.ToNodeId}_{data.ToPortId}"
			);

			connectionView.Bind(data, from.ConnectorRect, to.ConnectorRect, validColor);

			connectionView.Clicked += HandleConnectionClicked;

			runtimeConnections.Add(new RuntimeConnection(data, from, to, connectionView));

			Debug.Log(
				$"[{nameof(NodeConnectionDragController)}] Connected {data.Id}",
				this);

			return true;
		}

		private bool ConnectionExists(GeneratedNodePortView from, GeneratedNodePortView to)
		{
			if (from == null || to == null)
			{
				return false;
			}

			string fromNodeId = GetNodeInstanceId(from.Owner);
			string toNodeId = GetNodeInstanceId(to.Owner);

			if (connectionModel != null &&
				connectionModel.ContainsConnection(fromNodeId, from.PortId, toNodeId, to.PortId))
			{
				return true;
			}

			foreach (RuntimeConnection connection in runtimeConnections)
			{
				if (connection.From == from && connection.To == to)
				{
					return true;
				}
			}

			return false;
		}

		private NodeConnectionData CreateConnectionData(
			GeneratedNodePortView from,
			GeneratedNodePortView to)
		{
			return new NodeConnectionData(
				GetNodeInstanceId(from.Owner),
				GetNodeDefinitionId(from.Owner),
				from.PortId,
				GetNodeInstanceId(to.Owner),
				GetNodeDefinitionId(to.Owner),
				to.PortId
			);
		}

		private NodeConnectionView CreateConnectionView(string objectName)
		{
			NodeConnectionView connectionView = Instantiate(connectionPrefab, connectionsRoot);
			connectionView.name = objectName;

			if (connectionView.transform is RectTransform rectTransform)
			{
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
				rectTransform.localScale = Vector3.one;
				rectTransform.localRotation = Quaternion.identity;
			}

			return connectionView;
		}

		private void DestroyPreviewConnection()
		{
			if (previewConnection == null)
			{
				return;
			}

			Destroy(previewConnection.gameObject);
		}

		private static string GetNodeInstanceId(NodeView nodeView)
		{
			if (nodeView == null)
			{
				return string.Empty;
			}

			if (!string.IsNullOrWhiteSpace(nodeView.InstanceId))
			{
				return nodeView.InstanceId;
			}

			if (nodeView.Definition != null)
			{
				return nodeView.Definition.Id;
			}

			if (nodeView.JsonDefinition != null)
			{
				return nodeView.JsonDefinition.id;
			}

			return nodeView.name;
		}

		private static string GetNodeDefinitionId(NodeView nodeView)
		{
			if (nodeView == null)
			{
				return string.Empty;
			}

			if (nodeView.Definition != null)
			{
				return nodeView.Definition.Id;
			}

			if (nodeView.JsonDefinition != null)
			{
				return nodeView.JsonDefinition.id;
			}

			return string.Empty;
		}

		private readonly struct RuntimeConnection
		{
			public readonly NodeConnectionData Data;
			public readonly GeneratedNodePortView From;
			public readonly GeneratedNodePortView To;
			public readonly NodeConnectionView View;

			public RuntimeConnection(
				NodeConnectionData data,
				GeneratedNodePortView from,
				GeneratedNodePortView to,
				NodeConnectionView view)
			{
				Data = data;
				From = from;
				To = to;
				View = view;
			}
		}
		private void HandleConnectionClicked(NodeConnectionView connectionView, PointerEventData eventData)
		{
			if (connectionView == null || connectionView.Data == null)
			{
				return;
			}

			// Сейчас удаляем только ПКМ, чтобы случайно не стирать линии ЛКМ.
			if (eventData.button != PointerEventData.InputButton.Right)
			{
				return;
			}

			DeleteConnection(connectionView.Data);
		}
		private void DeleteConnection(NodeConnectionData data)
		{
			if (data == null)
			{
				return;
			}

			if (connectionModel != null)
			{
				connectionModel.TryRemoveConnection(
					data.FromNodeId,
					data.FromPortId,
					data.ToNodeId,
					data.ToPortId
				);
			}

			for (int i = runtimeConnections.Count - 1; i >= 0; i--)
			{
				RuntimeConnection connection = runtimeConnections[i];

				if (connection.Data == null || connection.View == null)
				{
					runtimeConnections.RemoveAt(i);
					continue;
				}

				if (connection.Data.Id != data.Id)
				{
					continue;
				}

				connection.View.Clicked -= HandleConnectionClicked;
				Destroy(connection.View.gameObject);
				runtimeConnections.RemoveAt(i);

				Debug.Log($"[{nameof(NodeConnectionDragController)}] Deleted connection: {data.Id}", this);
				return;
			}
		}
	}
}