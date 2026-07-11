#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using Game.Nodes.Presentation;
using Game.World.Reports;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Nodes.Runtime
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NodeView))]
	public sealed class UserReportsHistoryView : NodeRuntimeBehaviour
	{
		[Header("Режим работы")]
		[Tooltip("Главный рубильник всей автоматической подготовки. Если выключено, скрипт не ищет ссылки, не создаёт сервис, не создаёт UI и не трогает layout.")]
		[SerializeField, InspectorName("Автоматический режим")]
		private bool automaticMode;

		[Tooltip("Если включено, UI будет обновляться по таймеру. Если выключено, можно обновлять вручную через RefreshNow().")]
		[SerializeField, InspectorName("Авто-обновление UI")]
		private bool refreshAutomatically = true;

		[Tooltip("Если включено вместе с автоматическим режимом, скрипт сам ищет ScrollRect, Content, Template, SummaryRoot и Title.")]
		[SerializeField, InspectorName("Авто-поиск ссылок")]
		private bool autoResolveReferences = true;

		[Tooltip("Если включено вместе с автоматическим режимом, при пустом сервисе будет вызван UserReportsService.GetOrCreate().")]
		[SerializeField, InspectorName("Авто-создание сервиса")]
		private bool autoCreateReportsService;

		[Tooltip("Если включено вместе с автоматическим режимом, при обновлении будет вызван EnsureReport(). Оставь выключенным, если отчёты должны приходить только из игровой логики.")]
		[SerializeField, InspectorName("Авто-создание отчётов")]
		private bool autoCreateReports;

		[Tooltip("Если включено вместе с автоматическим режимом, скрипт сам создаёт summary-строки.")]
		[SerializeField, InspectorName("Авто-создание Summary")]
		private bool autoCreateSummaryRows;

		[Tooltip("Если включено вместе с автоматическим режимом, скрипт сам создаёт шаблон строки отчёта, если он не назначен.")]
		[SerializeField, InspectorName("Авто-создание шаблона отчёта")]
		private bool autoCreateReportTemplate;

		[Tooltip("Если включено вместе с автоматическим режимом, скрипт перезаписывает RectTransform и LayoutGroup параметрами из инспектора.")]
		[SerializeField, InspectorName("Применять авто-layout")]
		private bool autoApplyLayout;

		[Tooltip("Если включено вместе с автоматическим режимом, скрипт применяет шрифты, цвета и размеры из инспектора.")]
		[SerializeField, InspectorName("Применять авто-стили")]
		private bool autoApplyStyles = true;

		[Tooltip("Если включено вместе с автоматическим режимом, layout и стили будут применяться во время игры каждый refresh. В ручной настройке лучше держать выключенным.")]
		[SerializeField, InspectorName("Обновлять настройки во время игры")]
		private bool applySettingsDuringRuntime;

		[Header("Сервисы")]
		[Tooltip("Сервис, который хранит отчёты пользователей. В ручном режиме обязательно назначить руками.")]
		[SerializeField, InspectorName("Сервис отчётов")]
		private UserReportsService reportsService;

		[Header("UI ссылки")]
		[Tooltip("ScrollRect, внутри которого отображается история отчётов.")]
		[SerializeField, InspectorName("Scroll View")]
		private ScrollRect scrollRect;

		[Tooltip("RectTransform объекта Content внутри Scroll View. Сюда создаются строки отчётов.")]
		[SerializeField, InspectorName("Content отчётов")]
		private RectTransform reportsContent;

		[Tooltip("TMP-текст, который используется как шаблон для каждой строки отчёта. Объект будет скрыт во время игры.")]
		[SerializeField, InspectorName("Шаблон строки отчёта")]
		private TMP_Text reportRowTemplate;

		[Tooltip("Контейнер для summary-строк.")]
		[SerializeField, InspectorName("Корень Summary")]
		private RectTransform summaryRoot;

		[Tooltip("Текст заголовка ноды.")]
		[SerializeField, InspectorName("Текст заголовка")]
		private TMP_Text titleText;

		[Header("Summary ссылки")]
		[SerializeField, InspectorName("Текст активных пользователей")]
		private TMP_Text activeUsersText;

		[SerializeField, InspectorName("Текст настроения")]
		private TMP_Text moodText;

		[SerializeField, InspectorName("Текст количества отчётов")]
		private TMP_Text reportsText;

		[SerializeField, InspectorName("Текст критических отчётов")]
		private TMP_Text criticalText;

		[Header("Обновление")]
		[Tooltip("Как часто view перечитывает сервис и обновляет UI, в секундах.")]
		[SerializeField, Min(0.05f), InspectorName("Интервал обновления")]
		private float refreshInterval = 0.25f;

		[Tooltip("Сколько последних отчётов показывать в Scroll View.")]
		[SerializeField, Min(1), InspectorName("Максимум видимых отчётов")]
		private int maxVisibleReports = 24;

		[Tooltip("Если включено, новые отчёты отображаются сверху списка.")]
		[SerializeField, InspectorName("Новые сверху")]
		private bool newestFirst = true;

		[Tooltip("Если включено, Scroll View автоматически держит последний отчёт видимым.")]
		[SerializeField, InspectorName("Держать последний отчёт видимым")]
		private bool keepLatestVisible = true;

		[Header("Фильтр ноды")]
		[Tooltip("Id ноды, на которой этот runtime-view должен работать.")]
		[SerializeField, InspectorName("Id ноды отчётов")]
		private string userReportsNodeId = "user_reports";

		[Header("Summary: подписи")]
		[SerializeField, InspectorName("Подпись активных пользователей")]
		private string activeUsersLabel = "Активные пользователи";

		[SerializeField, InspectorName("Подпись настроения")]
		private string moodLabel = "Настроение";

		[SerializeField, InspectorName("Подпись отчётов")]
		private string reportsLabel = "Отчёты";

		[SerializeField, InspectorName("Подпись критических")]
		private string criticalLabel = "Критические";

		[Header("Заголовок")]
		[Tooltip("TMP Font Asset для заголовка. Если пусто, используется текущий шрифт объекта.")]
		[SerializeField, InspectorName("Шрифт заголовка")]
		private TMP_FontAsset titleFont;

		[Tooltip("Размер шрифта заголовка.")]
		[SerializeField, Min(1f), InspectorName("Размер заголовка")]
		private float titleFontSize = 36f;

		[Tooltip("Цвет заголовка.")]
		[SerializeField, InspectorName("Цвет заголовка")]
		private Color titleColor = Color.white;

		[Tooltip("Выравнивание заголовка.")]
		[SerializeField, InspectorName("Выравнивание заголовка")]
		private TextAlignmentOptions titleAlignment = TextAlignmentOptions.Left;

		[Header("Summary: RectTransform")]
		[SerializeField, InspectorName("Отступ сверху")]
		private float summaryTop = 163.2f;

		[SerializeField, InspectorName("Изменение ширины")]
		private float summaryWidthDelta = 356.4f;

		[SerializeField, Min(0f), InspectorName("Высота")]
		private float summaryHeight = 300f;

		[Header("Summary: Layout")]
		[SerializeField, InspectorName("Расстояние между строками")]
		private float summarySpacing = 50f;

		[SerializeField, InspectorName("Отступ слева")]
		private int summaryPaddingLeft;

		[SerializeField, InspectorName("Отступ справа")]
		private int summaryPaddingRight;

		[SerializeField, InspectorName("Отступ сверху")]
		private int summaryPaddingTop;

		[SerializeField, InspectorName("Отступ снизу")]
		private int summaryPaddingBottom;

		[Header("Summary: Текст")]
		[SerializeField, InspectorName("Шрифт Summary")]
		private TMP_FontAsset summaryFont;

		[SerializeField, Min(1f), InspectorName("Размер Summary")]
		private float summaryFontSize = 48.4f;

		[SerializeField, Min(1f), InspectorName("Высота строки Summary")]
		private float summaryLineHeight = 18f;

		[SerializeField, InspectorName("Цвет Summary")]
		private Color summaryColor = Color.white;

		[SerializeField, InspectorName("Выравнивание Summary")]
		private TextAlignmentOptions summaryAlignment = TextAlignmentOptions.Left;

		[SerializeField, InspectorName("Межбуквенный интервал Summary")]
		private float summaryCharacterSpacing;

		[SerializeField, InspectorName("Межстрочный интервал Summary")]
		private float summaryLineSpacing;

		[Header("Scroll View: RectTransform")]
		[SerializeField, InspectorName("Left")]
		private float scrollLeft = -178.2f;

		[SerializeField, InspectorName("Right")]
		private float scrollRight = -178.2f;

		[SerializeField, InspectorName("Top")]
		private float scrollTop = 242.6f;

		[SerializeField, InspectorName("Bottom")]
		private float scrollBottom = -265.485f;

		[SerializeField, InspectorName("Правый отступ Viewport")]
		private float viewportRightPadding = 12f;

		[Header("Scroll View: Поведение")]
		[SerializeField, InspectorName("Горизонтальная прокрутка")]
		private bool scrollHorizontal;

		[SerializeField, InspectorName("Вертикальная прокрутка")]
		private bool scrollVertical = true;

		[SerializeField, InspectorName("Тип движения")]
		private ScrollRect.MovementType scrollMovementType = ScrollRect.MovementType.Clamped;

		[SerializeField, InspectorName("Инерция")]
		private bool scrollInertia = true;

		[SerializeField, InspectorName("Затухание инерции")]
		private float scrollDecelerationRate = 0.135f;

		[SerializeField, InspectorName("Чувствительность прокрутки")]
		private float scrollSensitivity = 18f;

		[Header("Content: Layout")]
		[SerializeField, InspectorName("Расстояние между отчётами")]
		private float reportSpacing = 96.1f;

		[SerializeField, InspectorName("Отступ слева")]
		private int reportPaddingLeft = 8;

		[SerializeField, InspectorName("Отступ справа")]
		private int reportPaddingRight = 12;

		[SerializeField, InspectorName("Отступ сверху")]
		private int reportPaddingTop = 8;

		[SerializeField, InspectorName("Отступ снизу")]
		private int reportPaddingBottom = 12;

		[Header("Строки отчётов: Текст")]
		[SerializeField, InspectorName("Шрифт отчёта")]
		private TMP_FontAsset reportFont;

		[SerializeField, Min(1f), InspectorName("Размер отчёта")]
		private float reportFontSize = 41f;

		[SerializeField, Min(1f), InspectorName("Высота строки отчёта")]
		private float reportRowHeight = 54f;

		[SerializeField, InspectorName("Цвет отчёта")]
		private Color reportColor = Color.white;

		[SerializeField, InspectorName("Выравнивание отчёта")]
		private TextAlignmentOptions reportAlignment = TextAlignmentOptions.TopLeft;

		[SerializeField, InspectorName("Межбуквенный интервал отчёта")]
		private float reportCharacterSpacing;

		[SerializeField, InspectorName("Межстрочный интервал отчёта")]
		private float reportLineSpacing;

		private readonly List<TMP_Text> reportRows = new();

		private NodeView nodeView;
		private float refreshTimer;
		private int renderedTotalReports = -1;
		private string lastSummaryKey = string.Empty;

		private bool ShouldAutoResolveReferences => automaticMode && autoResolveReferences;
		private bool ShouldAutoCreateReportsService => automaticMode && autoCreateReportsService;
		private bool ShouldAutoCreateReports => automaticMode && autoCreateReports;
		private bool ShouldAutoCreateSummaryRows => automaticMode && autoCreateSummaryRows;
		private bool ShouldAutoCreateReportTemplate => automaticMode && autoCreateReportTemplate;
		private bool ShouldAutoApplyLayout => automaticMode && autoApplyLayout;
		private bool ShouldAutoApplyStyles => automaticMode && autoApplyStyles;
		private bool ShouldApplySettingsDuringRuntime => automaticMode && applySettingsDuringRuntime;

		[Inject]
		private void Construct(UserReportsService service)
		{
			if (reportsService == null)
			{
				reportsService = service;
			}
		}

		private void Awake()
		{
			nodeView = GetComponent<NodeView>();

			if (ShouldAutoResolveReferences)
			{
				ResolveViewReferences();
			}
		}

		private void Start()
		{
			if (!IsUserReportsNode())
			{
				return;
			}

			if (ShouldAutoResolveReferences)
			{
				ResolveViewReferences();
			}

			ResolveServices();

			if (ShouldAutoApplyLayout)
			{
				ApplyLayout();
			}

			if (ShouldAutoApplyStyles)
			{
				ApplyTitleStyle();
			}

			PrepareSummaryRows();
			PrepareReportTemplate();
			Refresh(true);
		}

		private void Update()
		{
			if (!refreshAutomatically || !IsUserReportsNode())
			{
				return;
			}

			refreshTimer += UnityEngine.Time.unscaledDeltaTime;

			if (refreshTimer < refreshInterval)
			{
				return;
			}

			refreshTimer = 0f;

			if (ShouldApplySettingsDuringRuntime)
			{
				ApplyLiveSettings();
			}

			Refresh(false);
		}

		protected override void OnInitialized()
		{
			nodeView = View != null ? View : nodeView;

			if (ShouldAutoResolveReferences)
			{
				ResolveViewReferences();
			}
		}

		public void RefreshNow()
		{
			Refresh(true);
		}

		[ContextMenu("User Reports/Отключить всю автоматику")]
		private void DisableAllAutomation()
		{
			automaticMode = false;
			autoResolveReferences = false;
			autoCreateReportsService = false;
			autoCreateReports = false;
			autoCreateSummaryRows = false;
			autoCreateReportTemplate = false;
			autoApplyLayout = false;
			autoApplyStyles = false;
			applySettingsDuringRuntime = false;

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		private void ResolveServices()
		{
			if (reportsService != null)
			{
				return;
			}

			if (ShouldAutoCreateReportsService)
			{
				reportsService = UserReportsService.GetOrCreate();
			}
		}

		private void ResolveViewReferences()
		{
			if (scrollRect == null)
			{
				scrollRect = GetComponentInChildren<ScrollRect>(true);
			}

			if (reportsContent == null && scrollRect != null)
			{
				reportsContent = scrollRect.content;
			}

			if (reportRowTemplate == null && reportsContent != null)
			{
				reportRowTemplate = reportsContent.GetComponentInChildren<TMP_Text>(true);
			}

			if (summaryRoot == null)
			{
				Transform summaryTransform = transform.Find("VerticalLayout");
				summaryRoot = summaryTransform as RectTransform;
			}

			if (titleText == null)
			{
				titleText = FindTextByName("TitleNodeText");
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

		private void ApplyLiveSettings()
		{
			if (ShouldAutoApplyLayout)
			{
				ApplyLayout();
				ApplySummaryLayoutGroup();
				ApplyReportLayoutGroup();
			}

			if (ShouldAutoApplyStyles)
			{
				ApplyTitleStyle();
				ApplySummaryStyles();
				ApplyReportTemplateStyle();
				ApplyReportRowsStyles();
			}
		}

		private void ApplyLayout()
		{
			ApplySummaryLayout();
			ApplyScrollLayout();
			ApplyContentLayout();
		}

		private void ApplySummaryLayout()
		{
			if (summaryRoot == null)
			{
				return;
			}

			summaryRoot.anchorMin = new Vector2(0f, 1f);
			summaryRoot.anchorMax = new Vector2(1f, 1f);
			summaryRoot.pivot = new Vector2(0.5f, 1f);
			summaryRoot.anchoredPosition = new Vector2(0f, summaryTop);
			summaryRoot.sizeDelta = new Vector2(summaryWidthDelta, summaryHeight);
		}

		private void ApplyScrollLayout()
		{
			if (scrollRect == null)
			{
				return;
			}

			scrollRect.horizontal = scrollHorizontal;
			scrollRect.vertical = scrollVertical;
			scrollRect.movementType = scrollMovementType;
			scrollRect.inertia = scrollInertia;
			scrollRect.decelerationRate = scrollDecelerationRate;
			scrollRect.scrollSensitivity = scrollSensitivity;

			if (scrollRect.transform is RectTransform scrollTransform)
			{
				scrollTransform.anchorMin = Vector2.zero;
				scrollTransform.anchorMax = Vector2.one;
				scrollTransform.pivot = new Vector2(0.5f, 0.5f);
				scrollTransform.offsetMin = new Vector2(scrollLeft, scrollBottom);
				scrollTransform.offsetMax = new Vector2(-scrollRight, -scrollTop);
			}

			RectTransform viewport = scrollRect.viewport;

			if (viewport != null)
			{
				viewport.anchorMin = Vector2.zero;
				viewport.anchorMax = Vector2.one;
				viewport.offsetMin = Vector2.zero;
				viewport.offsetMax = new Vector2(-viewportRightPadding, 0f);
			}
		}

		private void ApplyTitleStyle()
		{
			ConfigureText(
				titleText,
				titleFontSize,
				titleAlignment,
				false,
				titleFont,
				titleColor,
				0f,
				0f,
				ShouldAutoApplyStyles,
				false
			);
		}

		private void ApplyContentLayout()
		{
			if (reportsContent == null)
			{
				return;
			}

			reportsContent.anchorMin = new Vector2(0f, 1f);
			reportsContent.anchorMax = new Vector2(1f, 1f);
			reportsContent.pivot = new Vector2(0.5f, 1f);
			reportsContent.anchoredPosition = Vector2.zero;
			reportsContent.sizeDelta = Vector2.zero;
		}

		private void PrepareSummaryRows()
		{
			if (summaryRoot == null)
			{
				return;
			}

			if (ShouldAutoApplyLayout)
			{
				ApplySummaryLayoutGroup();
			}

			if (ShouldAutoCreateSummaryRows)
			{
				ClearChildren(summaryRoot);

				activeUsersText = CreateSummaryText("Summary_ActiveUsers", activeUsersLabel, false);
				moodText = CreateSummaryText("Summary_Mood", moodLabel, false);
				reportsText = CreateSummaryText("Summary_Reports", reportsLabel, false);
				criticalText = CreateSummaryText("Summary_Critical", criticalLabel, false);
				return;
			}

			if (ShouldAutoApplyStyles)
			{
				ApplySummaryStyles();
			}
		}

		private void ApplySummaryLayoutGroup()
		{
			if (summaryRoot == null)
			{
				return;
			}

			VerticalLayoutGroup layout = summaryRoot.GetComponent<VerticalLayoutGroup>();

			if (layout == null)
			{
				return;
			}

			layout.spacing = summarySpacing;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = false;
			layout.padding = new RectOffset(
				summaryPaddingLeft,
				summaryPaddingRight,
				summaryPaddingTop,
				summaryPaddingBottom
			);
		}

		private TMP_Text CreateSummaryText(string objectName, string label, bool wrap)
		{
			TMP_Text text = CreateText(summaryRoot, objectName);
			ApplySummaryTextStyle(text, wrap);
			Game.UI.RuntimeTextRefresh.SetText(text, $"{label}: -");
			return text;
		}

		private void ApplySummaryStyles()
		{
			ApplySummaryTextStyle(activeUsersText, false);
			ApplySummaryTextStyle(moodText, false);
			ApplySummaryTextStyle(reportsText, false);
			ApplySummaryTextStyle(criticalText, false);
		}

		private void ApplySummaryTextStyle(TMP_Text text, bool wrap)
		{
			ConfigureText(
				text,
				summaryFontSize,
				summaryAlignment,
				wrap,
				summaryFont,
				summaryColor,
				summaryCharacterSpacing,
				summaryLineSpacing,
				ShouldAutoApplyStyles,
				ShouldAutoApplyLayout
			);

			if (ShouldAutoApplyLayout)
			{
				SetPreferredHeight(text, summaryLineHeight);
			}
		}

		private void PrepareReportTemplate()
		{
			if (reportsContent == null)
			{
				return;
			}

			if (reportRowTemplate == null)
			{
				if (!ShouldAutoCreateReportTemplate)
				{
					return;
				}

				reportRowTemplate = CreateText(reportsContent, "ReportTemplate");
			}

			if (ShouldAutoApplyStyles || ShouldAutoApplyLayout)
			{
				ApplyReportTextStyle(reportRowTemplate);
			}

			ClearReportRows();

			if (ShouldAutoApplyLayout)
			{
				ApplyReportLayoutGroup();
			}
		}

		private void ApplyReportLayoutGroup()
		{
			if (reportsContent == null)
			{
				return;
			}

			VerticalLayoutGroup layout = reportsContent.GetComponent<VerticalLayoutGroup>();

			if (layout == null)
			{
				return;
			}

			layout.spacing = reportSpacing;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = false;
			layout.padding = new RectOffset(
				reportPaddingLeft,
				reportPaddingRight,
				reportPaddingTop,
				reportPaddingBottom
			);
		}

		private void ApplyReportTemplateStyle()
		{
			ApplyReportTextStyle(reportRowTemplate);
		}

		private void ApplyReportRowsStyles()
		{
			foreach (TMP_Text row in reportRows)
			{
				ApplyReportTextStyle(row);
			}
		}

		private void ApplyReportTextStyle(TMP_Text text)
		{
			ConfigureText(
				text,
				reportFontSize,
				reportAlignment,
				true,
				reportFont,
				reportColor,
				reportCharacterSpacing,
				reportLineSpacing,
				ShouldAutoApplyStyles,
				ShouldAutoApplyLayout
			);

			if (ShouldAutoApplyLayout)
			{
				SetPreferredHeight(text, reportRowHeight);
			}
		}

		private void Refresh(bool force)
		{
			if (reportsService == null)
			{
				ResolveServices();
			}

			if (reportsService == null)
			{
				UpdateSummary("0", "Неизвестно", "0", "0");
				return;
			}

			if (ShouldAutoCreateReports)
			{
				reportsService.EnsureReport();
			}

			UpdateSummary(
				reportsService.ActiveUsers.ToString(),
				reportsService.Mood.ToString(),
				reportsService.ReportCount.ToString(),
				reportsService.CriticalCount.ToString()
			);

			if (force || renderedTotalReports != reportsService.ReportCount)
			{
				RebuildReportRows();
			}
		}

		private void UpdateSummary(
			string activeUsers,
			string mood,
			string reports,
			string critical)
		{
			string summaryKey =
				$"{activeUsersLabel}|{moodLabel}|{reportsLabel}|{criticalLabel}|{activeUsers}|{mood}|{reports}|{critical}";

			if (summaryKey == lastSummaryKey)
			{
				return;
			}

			lastSummaryKey = summaryKey;

			SetSummary(activeUsersText, activeUsersLabel, activeUsers);
			SetSummary(moodText, moodLabel, mood);
			SetSummary(reportsText, reportsLabel, reports);
			SetSummary(criticalText, criticalLabel, critical);
		}

		private static void SetSummary(TMP_Text text, string label, string value)
		{
			if (text != null)
			{
				Game.UI.RuntimeTextRefresh.SetText(text, $"{label}: {value}");
			}
		}

		private void RebuildReportRows()
		{
			if (reportsContent == null || reportRowTemplate == null || reportsService == null)
			{
				return;
			}

			ClearReportRows();

			IReadOnlyList<UserReportEntry> reports = reportsService.Reports;
			int visibleCount = Mathf.Min(maxVisibleReports, reports.Count);

			for (int i = 0; i < visibleCount; i++)
			{
				int sourceIndex = newestFirst ? reports.Count - 1 - i : i;
				UserReportEntry entry = reports[sourceIndex];

				TMP_Text row = Instantiate(reportRowTemplate, reportsContent);
				row.name = $"Report_{reportsService.ReportCount - i}";
				row.gameObject.SetActive(true);

				if (ShouldAutoApplyStyles || ShouldAutoApplyLayout)
				{
					ApplyReportTextStyle(row);
				}

				Game.UI.RuntimeTextRefresh.SetText(row, FormatReport(entry));
				reportRows.Add(row);
			}

			renderedTotalReports = reportsService.ReportCount;

			if (ShouldAutoApplyLayout)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(reportsContent);
			}

			if (keepLatestVisible && scrollRect != null)
			{
				Canvas.ForceUpdateCanvases();
				scrollRect.verticalNormalizedPosition = newestFirst ? 1f : 0f;
			}
		}

		private static string FormatReport(UserReportEntry entry)
		{
			if (entry == null)
			{
				return "[Неизвестно] Пустой отчёт.";
			}

			string date = string.IsNullOrWhiteSpace(entry.DateText)
				? "Неизвестная дата"
				: entry.DateText;

			string authorName = string.IsNullOrWhiteSpace(entry.AuthorName)
				? "anonymous_user"
				: entry.AuthorName;

			return $"{date}  @{authorName}: {entry.Message}";
		}

		private void ClearReportRows()
		{
			for (int i = reportRows.Count - 1; i >= 0; i--)
			{
				if (reportRows[i] != null)
				{
					DestroyObject(reportRows[i].gameObject);
				}
			}

			reportRows.Clear();

			if (reportsContent == null)
			{
				return;
			}

			for (int i = reportsContent.childCount - 1; i >= 0; i--)
			{
				Transform child = reportsContent.GetChild(i);

				if (reportRowTemplate != null && child == reportRowTemplate.transform)
				{
					continue;
				}

				if (!child.name.StartsWith("Report_"))
				{
					continue;
				}

				DestroyObject(child.gameObject);
			}
		}

		private TMP_Text CreateText(RectTransform parent, string objectName)
		{
			GameObject textObject = new GameObject(objectName, typeof(RectTransform));
			textObject.transform.SetParent(parent, false);
			return textObject.AddComponent<TextMeshProUGUI>();
		}

		private static void ConfigureText(
			TMP_Text text,
			float fontSize,
			TextAlignmentOptions alignment,
			bool wrap,
			TMP_FontAsset fontAsset,
			Color color,
			float characterSpacing,
			float lineSpacing,
			bool applyStyle,
			bool applyRectTransform)
		{
			if (text == null)
			{
				return;
			}

			if (applyStyle)
			{
				if (fontAsset != null)
				{
					text.font = fontAsset;
				}

				text.fontSize = fontSize;
				text.color = color;
				text.alignment = alignment;
				text.richText = false;
				text.raycastTarget = false;
				text.margin = Vector4.zero;
				text.overflowMode = TextOverflowModes.Overflow;
				text.characterSpacing = characterSpacing;
				text.lineSpacing = lineSpacing;
				text.textWrappingMode = wrap
					? TextWrappingModes.Normal
					: TextWrappingModes.NoWrap;
			}

			if (applyRectTransform && text.transform is RectTransform rectTransform)
			{
				rectTransform.anchorMin = new Vector2(0f, 1f);
				rectTransform.anchorMax = new Vector2(1f, 1f);
				rectTransform.pivot = new Vector2(0f, 1f);
				rectTransform.sizeDelta = Vector2.zero;
			}
		}

		private static void SetPreferredHeight(TMP_Text text, float height)
		{
			if (text == null)
			{
				return;
			}

			LayoutElement layoutElement = text.GetComponent<LayoutElement>();

			if (layoutElement == null)
			{
				layoutElement = text.gameObject.AddComponent<LayoutElement>();
			}

			layoutElement.minHeight = height;
			layoutElement.preferredHeight = height;
			layoutElement.flexibleHeight = 0f;
			layoutElement.flexibleWidth = 1f;
		}

		private void ClearChildren(RectTransform root)
		{
			for (int i = root.childCount - 1; i >= 0; i--)
			{
				DestroyObject(root.GetChild(i).gameObject);
			}
		}

		private static void DestroyObject(GameObject target)
		{
			if (target == null)
			{
				return;
			}

			if (Application.isPlaying)
			{
				Destroy(target);
				return;
			}

			DestroyImmediate(target);
		}

		private bool IsUserReportsNode()
		{
			NodeView view = nodeView != null ? nodeView : View;

			if (view == null)
			{
				return true;
			}

			if (view.Definition != null)
			{
				return view.Definition.Id == userReportsNodeId;
			}

			if (view.JsonDefinition != null)
			{
				return view.JsonDefinition.id == userReportsNodeId;
			}

			return true;
		}
	}
}
