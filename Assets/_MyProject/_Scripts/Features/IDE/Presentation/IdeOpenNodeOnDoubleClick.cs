using System;
using Game.IDE.Runtime;
using Game.Nodes.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NodeView))]
	public sealed class IdeOpenNodeOnDoubleClick : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField] private NodeView nodeView;
		[SerializeField] private IdeRuntimeController ideRuntime;

		[Header("Double Click")]
		[SerializeField, Min(0.05f)] private float doubleClickMaxDelay = 0.32f;
		[SerializeField] private bool ignoreClicksOnPorts = true;

		private float lastClickTime = -999f;

		[Inject]
		private void Construct(IdeRuntimeController runtimeController)
		{
			if (ideRuntime == null)
			{
				ideRuntime = runtimeController;
			}
		}

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
			if (ideRuntime == null)
			{
				ideRuntime = IdeRuntimeController.Instance;
			}

			if (ideRuntime == null)
			{
				ideRuntime = FindAnyObjectByType<IdeRuntimeController>(FindObjectsInactive.Include);
			}

			if (ideRuntime == null)
			{
				Debug.LogError($"[{nameof(IdeOpenNodeOnDoubleClick)}] IdeRuntimeController was not found in scene.", this);
				return;
			}

			IdeNodeDocument document;

			try
			{
				document = IdeNodeDocumentResolver.Resolve(nodeView);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception, this);
				ideRuntime.OpenIdeOnly();
				return;
			}

			ideRuntime.OpenNodeDocument(
				document.DocumentId,
				document.Title,
				document.Code
			);
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

	}
}
