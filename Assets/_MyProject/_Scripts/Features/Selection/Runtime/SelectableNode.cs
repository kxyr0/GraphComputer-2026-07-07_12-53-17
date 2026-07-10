using System;
using Game.Nodes.Presentation;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Selection.Runtime
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NodeView))]
	[RequireComponent(typeof(RectTransform))]
	public sealed class SelectableNode : MonoBehaviour
	{
		[SerializeField] private NodeView nodeView;
		[SerializeField] private RectTransform rectTransform;
		[SerializeField] private bool selectable = true;
		[SerializeField] private bool clearSelectionOnDisable = true;
		[SerializeField] private UnityEvent<bool> selectedChanged = new();

		public event Action<SelectableNode, bool> SelectionChanged;

		public bool IsSelected { get; private set; }

		public bool CanSelect => selectable && isActiveAndEnabled;

		public NodeView NodeView
		{
			get
			{
				if (nodeView == null)
				{
					nodeView = GetComponent<NodeView>();
				}

				return nodeView;
			}
		}

		public RectTransform RectTransform
		{
			get
			{
				if (rectTransform == null)
				{
					rectTransform = transform as RectTransform;
				}

				return rectTransform;
			}
		}

		public UnityEvent<bool> SelectedChanged => selectedChanged;

		private void Reset()
		{
			ResolveReferences();
		}

		private void Awake()
		{
			ResolveReferences();
		}

		private void OnDisable()
		{
			if (clearSelectionOnDisable)
			{
				SetSelected(false);
			}
		}

		public void SetSelectable(bool value)
		{
			selectable = value;

			if (!selectable)
			{
				SetSelected(false);
			}
		}

		public void SetSelected(bool value)
		{
			if (IsSelected == value)
			{
				return;
			}

			IsSelected = value;
			SelectionChanged?.Invoke(this, IsSelected);
			selectedChanged?.Invoke(IsSelected);
		}

		private void ResolveReferences()
		{
			if (nodeView == null)
			{
				nodeView = GetComponent<NodeView>();
			}

			if (rectTransform == null)
			{
				rectTransform = transform as RectTransform;
			}
		}
	}
}
