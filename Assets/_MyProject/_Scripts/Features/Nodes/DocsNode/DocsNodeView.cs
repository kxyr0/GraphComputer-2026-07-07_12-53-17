using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.IDE.Presentation
{
	[AddComponentMenu("Graph Computer/Nodes/Docs Node View")]
	[DisallowMultipleComponent]
	public sealed class DocsNodeView : MonoBehaviour
	{
		private const string GeneratedButtonPrefix = "Generated_DocsPage_";

		[Header("UI")]
		[SerializeField] private TMP_Text titleText;
		[SerializeField] private TMP_Text pageTitleText;
		[SerializeField] private TMP_Text descriptionText;

		[Header("Page Buttons")]
		[SerializeField] private RectTransform pagesRoot;
		[SerializeField] private Button pageButtonTemplate;
		[SerializeField] private bool hideTemplate = true;

		[Header("Optional Navigation")]
		[SerializeField] private Button nextButton;
		[SerializeField] private Button previousButton;

		[Header("Visual")]
		[SerializeField] private Color selectedButtonColor = new Color(0.18f, 0.22f, 0.32f, 1f);
		[SerializeField] private Color normalButtonColor = new Color(0.06f, 0.08f, 0.11f, 0.55f);
		[SerializeField] private Color selectedTextColor = Color.white;
		[SerializeField] private Color normalTextColor = new Color(0.78f, 0.82f, 0.88f, 1f);

		[Header("Docs")]
		[SerializeField] private string title = "DOCS";
		[SerializeField] private List<DocsPage> pages = new List<DocsPage>();

		private readonly List<Button> generatedButtons = new List<Button>();
		private int currentPageIndex;

		private void Reset()
		{
			titleText = FindTextByName("Title");
			pageTitleText = FindTextByName("PageTitle");
			descriptionText = FindTextByName("Description");

			if (pagesRoot == null)
			{
				pagesRoot = transform as RectTransform;
			}

			if (pageButtonTemplate == null)
			{
				pageButtonTemplate = GetComponentInChildren<Button>(true);
			}

			FillDefaultPages();
		}

		private void Awake()
		{
			if (titleText != null)
			{
				titleText.text = title;
			}

			EnsurePages();
			BuildPageButtons();
			ShowPage(0);
		}

		private void OnEnable()
		{
			if (nextButton != null)
			{
				nextButton.onClick.AddListener(ShowNextPage);
			}

			if (previousButton != null)
			{
				previousButton.onClick.AddListener(ShowPreviousPage);
			}
		}

		private void OnDisable()
		{
			if (nextButton != null)
			{
				nextButton.onClick.RemoveListener(ShowNextPage);
			}

			if (previousButton != null)
			{
				previousButton.onClick.RemoveListener(ShowPreviousPage);
			}
		}

		[ContextMenu("Rebuild Docs Buttons")]
		public void Rebuild()
		{
			EnsurePages();
			BuildPageButtons();
			ShowPage(Mathf.Clamp(currentPageIndex, 0, pages.Count - 1));
		}

		[ContextMenu("Fill Default Pages")]
		public void FillDefaultPages()
		{
			pages = new List<DocsPage>
			{
				new DocsPage(
					"start",
					"How modules work",
@"A node is not an upgrade by itself.

A node gives you a place to write a module.
A module becomes useful only after a successful Build.

Basic loop:
1. Connect node.
2. Open node with double click.
3. Write module code.
4. Press Build.
5. If Build succeeds, the module becomes operational.

If the code is missing, broken, or failed to build, the node stays installed but gives no real benefit."
				),

				new DocsPage(
					"module",
					"Module declaration",
@"Every file should start with a module declaration.

Example:
module SystemCore

The module name tells the build system what this code belongs to.

Good:
module SystemCore
module Kernel
module Data
module Security
module Control

Bad:
dsadasdas
kernel stuff here
system core maybe

If the module declaration is missing, Build should fail."
				),

				new DocsPage(
					"require",
					"Required dependencies",
@"require means this module needs another module to work.

Example:
require Kernel as kernel
require Data as data

If a required dependency is missing or not built, this module should not work correctly.

Use require for critical systems:
- Kernel
- Data
- File System
- Memory Manager

Example:
module SystemCore

require Kernel as kernel
require Data as data"
				),

				new DocsPage(
					"optional",
					"Optional dependencies",
@"optional means this module can work without that dependency.

Example:
optional Security as security
optional Control as control

Optional dependency should be checked before use.

Example:
if security.exists
    load security.firewall
else
    warning ""No security layer""
end

Optional modules are useful for upgrades, plugins and advanced branches."
				),

				new DocsPage(
					"budget",
					"Hardware budget",
@"budget describes hardware limits for this module.

Example:
budget {
    cpu <= 72
    memory <= 68
    storage <= 80
}

If your module does too much for old hardware, users will complain about lag, freezes or crashes.

Good code respects hardware.
Bad code assumes infinite CPU.

In old eras, budget is extremely important. A good idea can still fail if hardware cannot run it."
				),

				new DocsPage(
					"boot",
					"Boot logic",
@"boot runs when the OS starts.

Example:
boot {
    start kernel.scheduler
    mount data.fileSystem
    scan security.firewall
}

Use boot for:
- starting scheduler
- mounting file system
- loading drivers
- initializing services

Do not put heavy repeated logic here.

Bad:
boot {
    scan allFiles
    rebuild index
    encrypt disk
}

That may make boot time terrible."
				),

				new DocsPage(
					"tick",
					"Runtime loop",
@"tick runs repeatedly while the OS is alive.

Example:
tick every 1s {
    systemLoad = kernel.load + data.ioLoad

    if systemLoad > 80
        throttle data.index by 25
        alert ""High Load""
    end
}

Danger:
tick every frame
tick every 0.1s

High frequency code can destroy old hardware.

Use tick for:
- monitoring load
- reacting to pressure
- background maintenance
- scheduled checks"
				),

				new DocsPage(
					"export",
					"Export metrics",
@"export exposes calculated values to the OS.

Example:
export {
    stability = 100 - systemLoad * 0.25
    load = systemLoad
}

Prefer export over direct metric mutation.

Bad:
stability += 12
network += data.output

Better:
export {
    stability = 100 - risk - load * 0.25
    network = control.networkSpeed - security.firewallCost
}

The system should calculate results from behavior, not magic numbers."
				),

				new DocsPage(
					"system",
					"Working System Core example",
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
    scan security.firewall
}

tick every 1s {
    monitor kernel.load
    cache data.index

    if kernel.load > 80
        throttle data.index by 25
        alert ""High Load""
    end
}

export {
    stability = 100 - kernel.load * 0.25
    load = kernel.load
}"
				),

				new DocsPage(
					"kernel",
					"Working Kernel example",
@"module Kernel

require SystemCore as core

budget {
    cpu <= 48
    memory <= 64
}

scheduler {
    mode = round_robin
    timeSlice = 12ms
}

boot {
    start scheduler
    watch processes
}

tick every 1s {
    monitor processQueue

    if processes > 32
        throttle scheduler by 20
        alert ""Process Pressure""
    end
}

export {
    load = processQueue.pressure
    response = scheduler.latency
}"
				),

				new DocsPage(
					"data",
					"Working Data example",
@"module Data

require SystemCore as core

budget {
    memory <= 48
    storage <= 80
}

boot {
    mount fileSystem
    index directories
    cache fileTable
}

tick every 5s {
    scan storageUsage

    if storageUsage > 80
        flush tempFiles
        alert ""Storage Pressure""
    end
}

export {
    ioLoad = fileSystem.readLoad
    safety = backup.health
}"
				),

				new DocsPage(
					"security",
					"Working Security example",
@"module Security

require SystemCore as core

optional Firewall as firewall

budget {
    cpu <= 35
    memory <= 30
}

boot {
    load firewall
    protect userFiles
}

tick every 5s {
    scan incomingPackets

    if threats > 0
        block unknownPorts
        alert ""Threat Detected""
    end
}

export {
    threatLevel = firewall.risk
    encryption = security.encryption
}"
				),

				new DocsPage(
					"control",
					"Working Control example",
@"module Control

require SystemCore as core

budget {
    cpu <= 30
    memory <= 24
}

boot {
    bind display
    bind devices
    route input to devices
}

tick every 1s {
    monitor inputDelay

    if inputDelay > 25
        reduce inputDelay by 10
        alert ""Input Delay""
    end
}

export {
    deviceSupport = devices.ready
    latency = inputDelay
}"
				),

				new DocsPage(
					"offline",
					"Why module is offline",
@"If a module stays Offline, check this:

1. Is the node connected?
2. Did you open the correct node tab?
3. Does the code start with module Name?
4. Did Build succeed?
5. Does the module id match the node?
6. Are required dependencies built?
7. Is there an unknown statement error?

Connected node without successful build is only installed.
It is not operational.

Example:
Kernel connected but not built = Kernel Offline.

That is intended. A node gives capability. Code makes it work."
				)
			};
		}

		public void AddPage(string buttonTitle, string pageTitle, string description)
		{
			EnsurePages();

			pages.Add(new DocsPage(buttonTitle, pageTitle, description));
			BuildPageButtons();
			ShowPage(pages.Count - 1);
		}

		private void EnsurePages()
		{
			if (pages == null)
			{
				pages = new List<DocsPage>();
			}

			if (pages.Count == 0)
			{
				FillDefaultPages();
			}
		}

		private void BuildPageButtons()
		{
			ClearGeneratedButtons();

			if (pagesRoot == null || pageButtonTemplate == null || pages == null)
			{
				return;
			}

			if (hideTemplate)
			{
				pageButtonTemplate.gameObject.SetActive(false);
			}

			for (int i = 0; i < pages.Count; i++)
			{
				int capturedIndex = i;
				DocsPage page = pages[i];

				Button button = Instantiate(pageButtonTemplate, pagesRoot);
				button.name = GeneratedButtonPrefix + page.ButtonTitle;
				button.gameObject.SetActive(true);

				TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

				if (label != null)
				{
					label.text = page.ButtonTitle;
				}

				button.onClick.RemoveAllListeners();
				button.onClick.AddListener(() => ShowPage(capturedIndex));

				generatedButtons.Add(button);
			}

			RefreshButtonVisuals();
		}

		private void ClearGeneratedButtons()
		{
			generatedButtons.Clear();

			if (pagesRoot == null)
			{
				return;
			}

			for (int i = pagesRoot.childCount - 1; i >= 0; i--)
			{
				Transform child = pagesRoot.GetChild(i);

				if (pageButtonTemplate != null && child == pageButtonTemplate.transform)
				{
					continue;
				}

				if (!child.name.StartsWith(GeneratedButtonPrefix, StringComparison.Ordinal))
				{
					continue;
				}

				DestroySmart(child.gameObject);
			}
		}

		public void ShowNextPage()
		{
			if (pages == null || pages.Count == 0)
			{
				return;
			}

			ShowPage(currentPageIndex + 1);
		}

		public void ShowPreviousPage()
		{
			if (pages == null || pages.Count == 0)
			{
				return;
			}

			ShowPage(currentPageIndex - 1);
		}

		public void ShowPage(int index)
		{
			if (pages == null || pages.Count == 0)
			{
				SetTexts("empty", "No documentation pages.");
				return;
			}

			if (index < 0)
			{
				index = pages.Count - 1;
			}

			if (index >= pages.Count)
			{
				index = 0;
			}

			currentPageIndex = index;
			DocsPage page = pages[currentPageIndex];

			SetTexts(page.PageTitle, page.Description);
			RefreshButtonVisuals();
		}

		private void SetTexts(string pageTitle, string description)
		{
			if (pageTitleText != null)
			{
				pageTitleText.text = pageTitle;
			}

			if (descriptionText != null)
			{
				descriptionText.richText = false;
				descriptionText.enableWordWrapping = true;
				descriptionText.text = description;
			}
		}

		private void RefreshButtonVisuals()
		{
			for (int i = 0; i < generatedButtons.Count; i++)
			{
				Button button = generatedButtons[i];

				if (button == null)
				{
					continue;
				}

				bool selected = i == currentPageIndex;

				Image image = button.GetComponent<Image>();

				if (image != null)
				{
					image.color = selected
						? selectedButtonColor
						: normalButtonColor;
				}

				TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

				if (label != null)
				{
					label.color = selected
						? selectedTextColor
						: normalTextColor;
				}
			}
		}

		private TMP_Text FindTextByName(string objectName)
		{
			TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

			foreach (TMP_Text text in texts)
			{
				if (text != null && text.name == objectName)
				{
					return text;
				}
			}

			return null;
		}

		private void DestroySmart(GameObject target)
		{
			if (target == null)
			{
				return;
			}

			if (Application.isPlaying)
			{
				Destroy(target);
			}
			else
			{
				DestroyImmediate(target);
			}
		}

		[Serializable]
		public sealed class DocsPage
		{
			[SerializeField] private string buttonTitle;
			[SerializeField] private string pageTitle;

			[SerializeField, TextArea(4, 18)]
			private string description;

			public string ButtonTitle
			{
				get
				{
					if (!string.IsNullOrWhiteSpace(buttonTitle))
					{
						return buttonTitle;
					}

					return string.IsNullOrWhiteSpace(pageTitle)
						? "page"
						: pageTitle;
				}
			}

			public string PageTitle => string.IsNullOrWhiteSpace(pageTitle)
				? ButtonTitle
				: pageTitle;

			public string Description => description ?? string.Empty;

			public DocsPage(string buttonTitle, string pageTitle, string description)
			{
				this.buttonTitle = buttonTitle;
				this.pageTitle = pageTitle;
				this.description = description;
			}
		}
	}
}
