using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class IdeBottomTabsController : MonoBehaviour
	{
		[SerializeField] private int defaultTabIndex = 0;

		[Header("Animation")]
		[SerializeField, Min(0.01f)] private float fadeDuration = 0.12f;
		[SerializeField] private Ease fadeEase = Ease.OutCubic;

		[Header("Visual")]
		[SerializeField] private Color selectedButtonColor = new(0.12f, 0.14f, 0.20f, 1f);
		[SerializeField] private Color normalButtonColor = new(0.03f, 0.03f, 0.05f, 1f);

		[SerializeField] private Color selectedTextColor = Color.white;
		[SerializeField] private Color normalTextColor = new(0.65f, 0.65f, 0.65f, 1f);

		[SerializeField] private IdeTabItem[] tabs;

		private int currentIndex = -1;

		private void Awake()
		{
			RegisterButtons();

			if (tabs == null || tabs.Length == 0)
			{
				return;
			}

			int safeIndex = Mathf.Clamp(defaultTabIndex, 0, tabs.Length - 1);
			SelectTabInstant(safeIndex);
		}

		private void OnDestroy()
		{
			if (tabs == null)
			{
				return;
			}

			foreach (IdeTabItem tab in tabs)
			{
				tab.RemoveListener();
			}
		}

		public void SelectConsole()
		{
			SelectTabById("console");
		}

		public void SelectOutput()
		{
			SelectTabById("output");
		}

		public void SelectTabById(string tabId)
		{
			if (tabs == null)
			{
				return;
			}

			for (int i = 0; i < tabs.Length; i++)
			{
				if (tabs[i].Matches(tabId))
				{
					SelectTab(i);
					return;
				}
			}
		}

		public void SelectTab(int index)
		{
			if (tabs == null || tabs.Length == 0)
			{
				return;
			}

			if (index < 0 || index >= tabs.Length)
			{
				return;
			}

			if (currentIndex == index)
			{
				return;
			}

			currentIndex = index;

			for (int i = 0; i < tabs.Length; i++)
			{
				bool selected = i == index;
				ApplyTabState(tabs[i], selected, animated: true);
			}
		}

		private void SelectTabInstant(int index)
		{
			currentIndex = index;

			for (int i = 0; i < tabs.Length; i++)
			{
				bool selected = i == index;
				ApplyTabState(tabs[i], selected, animated: false);
			}
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
				tabs[i].Initialize(() => SelectTab(capturedIndex));
			}
		}

		private void ApplyTabState(IdeTabItem tab, bool selected, bool animated)
		{
			if (tab == null)
			{
				return;
			}

			CanvasGroup group = tab.GetOrCreateCanvasGroup();

			if (group != null)
			{
				Tween.StopAll(onTarget: group);

				group.interactable = selected;
				group.blocksRaycasts = selected;

				if (animated)
				{
					Tween.Alpha(
						group,
						endValue: selected ? 1f : 0f,
						duration: fadeDuration,
						ease: fadeEase
					);
				}
				else
				{
					group.alpha = selected ? 1f : 0f;
				}
			}

			if (selected && tab.TabRoot != null)
			{
				tab.TabRoot.SetAsLastSibling();
			}

			if (tab.ButtonBackground != null)
			{
				tab.ButtonBackground.color = selected
					? selectedButtonColor
					: normalButtonColor;
			}

			if (tab.ButtonLabel != null)
			{
				tab.ButtonLabel.color = selected
					? selectedTextColor
					: normalTextColor;
			}

			tab.OnSelected(selected);
		}
	}

	[System.Serializable]
	public sealed class IdeTabItem
	{
		[SerializeField] private string id;
		[SerializeField] private Button button;
		[SerializeField] private Image buttonBackground;
		[SerializeField] private TMP_Text buttonLabel;
		[SerializeField] private RectTransform tabRoot;
		[SerializeField] private TMP_InputField focusInputOnSelect;

		private UnityEngine.Events.UnityAction cachedAction;

		public RectTransform TabRoot => tabRoot;
		public Image ButtonBackground => buttonBackground;
		public TMP_Text ButtonLabel => buttonLabel;

		public bool Matches(string tabId)
		{
			return string.Equals(
				id,
				tabId,
				System.StringComparison.OrdinalIgnoreCase
			);
		}

		public void Initialize(UnityEngine.Events.UnityAction onClick)
		{
			if (button == null)
			{
				return;
			}

			cachedAction = onClick;
			button.onClick.RemoveListener(cachedAction);
			button.onClick.AddListener(cachedAction);
		}

		public void RemoveListener()
		{
			if (button == null || cachedAction == null)
			{
				return;
			}

			button.onClick.RemoveListener(cachedAction);
			cachedAction = null;
		}

		public CanvasGroup GetOrCreateCanvasGroup()
		{
			if (tabRoot == null)
			{
				return null;
			}

			CanvasGroup group = tabRoot.GetComponent<CanvasGroup>();

			if (group == null)
			{
				group = tabRoot.gameObject.AddComponent<CanvasGroup>();
			}

			return group;
		}

		public void OnSelected(bool selected)
		{
			if (!selected || focusInputOnSelect == null)
			{
				return;
			}

			focusInputOnSelect.ActivateInputField();
		}
	}
}