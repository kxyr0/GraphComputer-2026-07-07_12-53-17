using UnityEngine;

namespace Game.Selection.Presentation
{
	[DisallowMultipleComponent]
	public sealed class SelectBoxView : MonoBehaviour
	{
		[SerializeField] private RectTransform boxRect;
		[SerializeField] private CanvasGroup canvasGroup;
		[SerializeField] private bool hideOnAwake = true;
		[SerializeField] private bool driveAnchorsAndPivot = true;

		public RectTransform BoxRect
		{
			get
			{
				if (boxRect == null)
				{
					boxRect = transform as RectTransform;
				}

				return boxRect;
			}
		}

		private void Reset()
		{
			boxRect = transform as RectTransform;
			canvasGroup = GetComponent<CanvasGroup>();
		}

		private void Awake()
		{
			if (boxRect == null)
			{
				boxRect = transform as RectTransform;
			}

			if (canvasGroup == null)
			{
				canvasGroup = GetComponent<CanvasGroup>();
			}

			if (hideOnAwake)
			{
				Hide();
			}
		}

		public void Show(Rect localRect)
		{
			if (BoxRect == null)
			{
				return;
			}

			if (driveAnchorsAndPivot)
			{
				Vector2 anchor = Vector2.zero;

				if (boxRect.parent is RectTransform parent)
				{
					anchor = parent.pivot;
				}

				boxRect.anchorMin = anchor;
				boxRect.anchorMax = anchor;
				boxRect.pivot = new Vector2(0.5f, 0.5f);
			}

			boxRect.gameObject.SetActive(true);
			boxRect.anchoredPosition = localRect.center;
			boxRect.sizeDelta = localRect.size;

			if (canvasGroup != null)
			{
				canvasGroup.alpha = 1f;
				canvasGroup.interactable = false;
				canvasGroup.blocksRaycasts = false;
			}
		}

		public void Hide()
		{
			if (BoxRect == null)
			{
				return;
			}

			if (canvasGroup != null)
			{
				canvasGroup.alpha = 0f;
				canvasGroup.interactable = false;
				canvasGroup.blocksRaycasts = false;
			}

			boxRect.gameObject.SetActive(false);
		}
	}
}
