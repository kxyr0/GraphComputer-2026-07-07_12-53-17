using PrimeTween;
using UnityEngine;

namespace Game.UI.OS
{
	[DisallowMultipleComponent]
	public sealed class OsWindowTabsController : MonoBehaviour
	{
		[SerializeField] private int defaultTabIndex;

		[Header("Animation")]
		[SerializeField, Min(0.01f)] private float fadeDuration = 0.12f;
		[SerializeField] private Ease fadeEase = Ease.OutCubic;

		[Header("Visual")]
		[SerializeField] private Color selectedButtonColor = new(0.12f, 0.14f, 0.20f, 1f);
		[SerializeField] private Color normalButtonColor = new(0.03f, 0.03f, 0.05f, 1f);
		[SerializeField] private Color selectedLabelColor = Color.white;
		[SerializeField] private Color normalLabelColor = new(0.65f, 0.65f, 0.65f, 1f);

		[Header("Tabs")]
		[SerializeField] private OsWindowTabItem[] tabs;

		private int currentIndex = -1;

		public string CurrentTabId =>
			tabs != null && currentIndex >= 0 && currentIndex < tabs.Length
				? tabs[currentIndex].Id
				: string.Empty;

		private void Awake()
		{
			RegisterButtons();
			SelectDefaultInstant();
		}

		private void OnDestroy()
		{
			if (tabs == null)
			{
				return;
			}

			foreach (OsWindowTabItem tab in tabs)
			{
				tab?.RemoveListener();
			}
		}

		public void SelectDefault()
		{
			if (tabs == null || tabs.Length == 0)
			{
				return;
			}

			SelectTab(Mathf.Clamp(defaultTabIndex, 0, tabs.Length - 1));
		}

		public void SelectTabById(string tabId)
		{
			if (tabs == null)
			{
				return;
			}

			for (int i = 0; i < tabs.Length; i++)
			{
				if (tabs[i] != null && tabs[i].Matches(tabId))
				{
					SelectTab(i);
					return;
				}
			}
		}

		public void SelectTab(int index)
		{
			if (!CanSelect(index) || currentIndex == index)
			{
				return;
			}

			currentIndex = index;

			for (int i = 0; i < tabs.Length; i++)
			{
				ApplyTabState(tabs[i], i == index, animated: true);
			}
		}

		private void SelectDefaultInstant()
		{
			if (tabs == null || tabs.Length == 0)
			{
				return;
			}

			int safeIndex = Mathf.Clamp(defaultTabIndex, 0, tabs.Length - 1);
			currentIndex = safeIndex;

			for (int i = 0; i < tabs.Length; i++)
			{
				ApplyTabState(tabs[i], i == safeIndex, animated: false);
			}
		}

		private bool CanSelect(int index)
		{
			return tabs != null &&
				   tabs.Length > 0 &&
				   index >= 0 &&
				   index < tabs.Length &&
				   tabs[index] != null;
		}

		private void RegisterButtons()
		{
			if (tabs == null)
			{
				return;
			}

			for (int i = 0; i < tabs.Length; i++)
			{
				int capturedIndex = i;
				tabs[i]?.Initialize(() => SelectTab(capturedIndex));
			}
		}

		private void ApplyTabState(OsWindowTabItem tab, bool selected, bool animated)
		{
			if (tab == null)
			{
				return;
			}

			CanvasGroup pageGroup = tab.GetOrCreatePageGroup();

			if (pageGroup != null)
			{
				Tween.StopAll(onTarget: pageGroup);

				pageGroup.interactable = selected;
				pageGroup.blocksRaycasts = selected;

				if (animated)
				{
					float targetAlpha = selected ? 1f : 0f;

					if (Mathf.Approximately(pageGroup.alpha, targetAlpha))
					{
						pageGroup.alpha = targetAlpha;
					}
					else
					{
						Tween.Alpha(
							pageGroup,
							endValue: targetAlpha,
							duration: fadeDuration,
							ease: fadeEase
						);
					}
				}
				else
				{
					pageGroup.alpha = selected ? 1f : 0f;
				}
			}

			if (selected && tab.PageRoot != null)
			{
				tab.PageRoot.SetAsLastSibling();
			}

			tab.ApplyButtonVisual(
				selected,
				selectedButtonColor,
				normalButtonColor,
				selectedLabelColor,
				normalLabelColor
			);
		}
	}
}
