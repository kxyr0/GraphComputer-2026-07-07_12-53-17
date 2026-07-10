using TMPro;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Selection.Categories.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public sealed class NodeCategoryFrameView : MonoBehaviour
	{
		[SerializeField] private RectTransform rectTransform;
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private Graphic raycastGraphic;
		[SerializeField] private CanvasGroup canvasGroup;
		[SerializeField] private bool controlChildRaycastGraphics = true;
		[SerializeField] private UnityEvent<bool> hoverChanged;
		[SerializeField] private UnityEvent<bool> selectedChanged;

		private Graphic[] childRaycastGraphics;
		private bool[] originalChildRaycastTargets;

		public bool IsHovered { get; private set; }
		public bool IsSelected { get; private set; }

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

		private void Reset()
		{
			rectTransform = transform as RectTransform;
			titleText = GetComponentInChildren<TMP_Text>(true);
			raycastGraphic = GetComponent<Graphic>();
			canvasGroup = GetComponent<CanvasGroup>();
		}

		private void Awake()
		{
			if (rectTransform == null)
			{
				rectTransform = transform as RectTransform;
			}

			if (raycastGraphic == null)
			{
				raycastGraphic = GetComponent<Graphic>();
			}

			if (canvasGroup == null)
			{
				canvasGroup = GetComponent<CanvasGroup>();
			}

			CaptureChildRaycastTargets();
		}

		public void SetHovered(bool value)
		{
			if (IsHovered == value)
			{
				return;
			}

			IsHovered = value;
			hoverChanged?.Invoke(value);
		}

		public void SetSelected(bool value)
		{
			if (IsSelected == value)
			{
				return;
			}

			IsSelected = value;
			selectedChanged?.Invoke(value);
		}

		public void SetRaycastBlocking(bool value)
		{
			if (canvasGroup != null)
			{
				canvasGroup.blocksRaycasts = value;
				canvasGroup.interactable = value;
			}

			if (controlChildRaycastGraphics)
			{
				ApplyChildRaycastBlocking(value);
			}
			else if (raycastGraphic != null)
			{
				raycastGraphic.raycastTarget = value;
			}
		}

		public void Bind(string title, Rect localBounds, int nodeCount)
		{
			name = $"NodeCategory_{title}";

			RectTransform target = RectTransform;
			target.anchorMin = new Vector2(0.5f, 0.5f);
			target.anchorMax = new Vector2(0.5f, 0.5f);
			target.pivot = new Vector2(0.5f, 0.5f);
			target.anchoredPosition = localBounds.center;
			target.sizeDelta = localBounds.size;
			target.localScale = Vector3.one;
			target.localRotation = Quaternion.identity;

			if (titleText != null)
			{
				titleText.text = $"{title} ({nodeCount})";
			}

			SetRaycastBlocking(true);
		}

		private void CaptureChildRaycastTargets()
		{
			childRaycastGraphics = GetComponentsInChildren<Graphic>(true);
			originalChildRaycastTargets = new bool[childRaycastGraphics.Length];

			for (int i = 0; i < childRaycastGraphics.Length; i++)
			{
				originalChildRaycastTargets[i] = childRaycastGraphics[i] != null && childRaycastGraphics[i].raycastTarget;
			}
		}

		private void ApplyChildRaycastBlocking(bool value)
		{
			if (childRaycastGraphics == null || originalChildRaycastTargets == null)
			{
				CaptureChildRaycastTargets();
			}

			for (int i = 0; i < childRaycastGraphics.Length; i++)
			{
				Graphic graphic = childRaycastGraphics[i];

				if (graphic == null)
				{
					continue;
				}

				graphic.raycastTarget = value && originalChildRaycastTargets[i];
			}

			if (raycastGraphic != null && !ContainsCapturedGraphic(raycastGraphic))
			{
				raycastGraphic.raycastTarget = value;
			}
		}

		private bool ContainsCapturedGraphic(Graphic graphic)
		{
			if (childRaycastGraphics == null)
			{
				return false;
			}

			for (int i = 0; i < childRaycastGraphics.Length; i++)
			{
				if (childRaycastGraphics[i] == graphic)
				{
					return true;
				}
			}

			return false;
		}
	}
}
