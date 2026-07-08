using Game.Nodes.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NodeView))]
	public sealed class IdeOpenNodeOnDoubleClick : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField] private NodeView nodeView;

		[Header("Double Click")]
		[SerializeField, Min(0.05f)] private float doubleClickMaxDelay = 0.32f;
		[SerializeField] private bool ignoreClicksOnPorts = true;

		private float lastClickTime = -999f;

		private void Reset()
		{
			nodeView = GetComponent<NodeView>();
		}

		private void Awake()
		{
			if (nodeView == null)
			{
				nodeView = GetComponent<NodeView>();
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}

			if (ignoreClicksOnPorts && IsClickOnPort(eventData))
			{
				return;
			}

			float now = Time.unscaledTime;

			bool doubleClicked =
				eventData.clickCount >= 2 ||
				now - lastClickTime <= doubleClickMaxDelay;

			lastClickTime = now;

			if (!doubleClicked)
			{
				return;
			}

			lastClickTime = -999f;
			OpenInIde();
		}

		private void OpenInIde()
		{
			IdeRuntimeController ideRuntime = IdeRuntimeController.Instance;

			if (ideRuntime == null)
			{
				ideRuntime = FindFirstObjectByType<IdeRuntimeController>(FindObjectsInactive.Include);
			}

			if (ideRuntime == null)
			{
				Debug.LogError($"[{nameof(IdeOpenNodeOnDoubleClick)}] IdeRuntimeController was not found in scene.", this);
				return;
			}

			string definitionId = GetNodeDefinitionId();
			string documentId = GetDocumentId();
			string title = GetNodeTitle();
			string code = GetDefaultCode(definitionId, title);

			ideRuntime.OpenNodeDocument(documentId, title, code);
		}

		private bool IsClickOnPort(PointerEventData eventData)
		{
			GameObject clickedObject = eventData.pointerPressRaycast.gameObject;

			if (clickedObject == null)
			{
				clickedObject = eventData.pointerCurrentRaycast.gameObject;
			}

			if (clickedObject == null)
			{
				return false;
			}

			return clickedObject.GetComponentInParent<GeneratedNodePortView>() != null;
		}

		private string GetDocumentId()
		{
			if (nodeView == null)
			{
				return "unknown_node";
			}

			if (!string.IsNullOrWhiteSpace(nodeView.InstanceId))
			{
				return nodeView.InstanceId;
			}

			return GetNodeDefinitionId();
		}

		private string GetNodeDefinitionId()
		{
			if (nodeView == null)
			{
				return "unknown";
			}

			if (nodeView.Definition != null)
			{
				return nodeView.Definition.Id;
			}

			if (nodeView.JsonDefinition != null)
			{
				return nodeView.JsonDefinition.id;
			}

			return "unknown";
		}

		private string GetNodeTitle()
		{
			if (nodeView == null)
			{
				return "Unknown";
			}

			if (nodeView.Definition != null && !string.IsNullOrWhiteSpace(nodeView.Definition.Title))
			{
				return nodeView.Definition.Title;
			}

			if (nodeView.JsonDefinition != null && !string.IsNullOrWhiteSpace(nodeView.JsonDefinition.title))
			{
				return nodeView.JsonDefinition.title;
			}

			return GetNodeDefinitionId();
		}

		private static string GetDefaultCode(string definitionId, string title)
		{
			return (definitionId ?? string.Empty).ToLowerInvariant() switch
			{
				"system_core" =>
@"module SystemCore

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
}

tick every 1s {
    systemLoad = kernel.load + data.ioLoad

    if systemLoad > 80
        throttle data.index by 25
        alert ""High Load""
    end
}

export {
    stability = 100 - systemLoad * 0.25
    load = systemLoad
}",

				"kernel" =>
@"module Kernel

require SystemCore as core

scheduler {
    mode = round_robin
    timeSlice = 12ms
}

if processes > 32
    alert ""Process Pressure""
end",

				"data" =>
@"module Data

require SystemCore as core

mount fileSystem
enable index

if storageUsage > 80
    alert ""Storage Pressure""
end",

				"security" =>
@"module Security

require SystemCore as core

load firewall
scan incomingPackets every 5s

if threats > 0
    alert ""Threat Detected""
end",

				"control" =>
@"module Control

require SystemCore as core

bind display
bind devices

if inputDelay > 25
    alert ""Input Delay""
end",

				"firewall" =>
@"module Firewall

require Security as security

rule allow trustedApps
rule block unknownPorts

scan packets every 2s",

				_ => $"module {ToModuleName(title)}\n\n// Write module logic here"
			};
		}

		private static string ToModuleName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "NewModule";
			}

			string[] parts = value
				.Replace("-", " ")
				.Replace("_", " ")
				.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

			string result = string.Empty;

			foreach (string part in parts)
			{
				result += char.ToUpperInvariant(part[0]) + part[1..];
			}

			return string.IsNullOrWhiteSpace(result)
				? "NewModule"
				: result;
		}
	}
}