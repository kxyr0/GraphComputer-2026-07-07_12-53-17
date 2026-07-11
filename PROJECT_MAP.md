# Project Map: GraphComputer

Обновлено: 2026-07-11.

## Кратко

`GraphComputer` - Unity-проект на Unity `6000.5.1f1`. Основная игра лежит в `Assets/_MyProject` и выглядит как интерактивный граф OS-модулей: игрок создаёт/соединяет ноды, открывает встроенную IDE, пишет DSL-код модулей и собирает его. Успешная сборка влияет на runtime-метрики OS, время, новости и пользовательские отчёты.

Главная сцена в Build Settings:

- `Assets/_MyProject/Scenes/Game.unity`

Основные пакеты:

- URP `com.unity.render-pipelines.universal` `17.5.0`
- Input System `com.unity.inputsystem` `1.19.0`
- UGUI `com.unity.ugui` `2.5.0`
- TextMesh Pro
- PrimeTween из локального архива `Assets/Plugins/PrimeTween/internal/com.kyrylokuzyk.primetween.tgz`
- Unity UI Rounded Corners
- Modern UI Pack как импортированный UI asset pack

## Корень проекта

| Путь | Назначение |
| --- | --- |
| `.git` | Git-репозиторий. |
| `.vscode` | Локальные настройки VS Code. |
| `Assets` | Все Unity-ассеты, код, сцены, prefab, сторонние ассеты. Главная рабочая зона. |
| `Packages` | Unity Package Manager: `manifest.json`, `packages-lock.json`. |
| `ProjectSettings` | Настройки Unity-проекта: версия Unity, URP, input, physics, build scenes. |
| `UserSettings` | Локальные пользовательские настройки Unity. Игнорируются git. |
| `GRAPH_OS_LANGUAGE.md` | Текущая спецификация внутреннего DSL: module, dependencies, budget, settings/mode, blocks, statements, Time speed, JSON `initialCode` и runtime tradeoffs. |
| `Library`, `Temp`, `Logs` | Сгенерированные Unity-папки. Игнорируются git, руками не редактировать. |
| `GraphComputer.slnx`, `*.csproj` | Сгенерированные IDE/Unity project-файлы. `.csproj` игнорируются git. |
| `_EMERGENCY_RECOVERY` | Снимок восстановления `Assets_snapshot`, `Library_snapshot`, `ScriptAssemblies`. Игнорируется git. |
| `_PANIC_BACKUP` | Аварийный backup compiled assemblies. Игнорируется git. |
| `_RECOVERED_FROM_VSCODE_HISTORY` | Восстановленные данные из истории VS Code. Игнорируется git. |
| `_RECOVERED_NODE_SCRIPTS` | Восстановленные версии node-скриптов. Игнорируется git, полезно только как архив/сравнение. |

## Assets

| Путь | Назначение |
| --- | --- |
| `Assets/_MyProject` | Собственный код и игровые ассеты проекта. Это главное место для работы. |
| `Assets/Modern UI Pack` | Импортированный UI kit: `Animations`, `Editor`, `Fonts`, `Prefabs`, `Resources`, `Scenes`, `Scripts`, `Textures`. |
| `Assets/TextMesh Pro` | TMP fonts/resources/shaders. Системная зависимость UI-текста. |
| `Assets/Unity-UI-Rounded-Corners` | Пакет `com.nobi.roundedcorners`, компоненты и shaders для скруглённых UI. |
| `Assets/Plugins/PrimeTween` | Локальный vendor PrimeTween, используется в UI-анимациях. |
| `Assets/___www.unityassetcollection.com___.txt` | Маркер источника asset collection. Не игровая логика. |

## Assets/_MyProject

| Путь | Назначение |
| --- | --- |
| `Art` | Собственные шейдеры, материалы и TMP font assets. |
| `Prefab` | Prefab-ы нод и UI-элементов. |
| `Scenes` | Игровые сцены. Сейчас рабочая сцена одна: `Game.unity`. |
| `Settings` | URP/Input/Volume/scene-template настройки проекта. |
| `Sounds` | Музыка и звуки. Сейчас есть 2 mp3 в `Sounds/Music`. |
| `_Scripts` | Весь собственный C#-код проекта. |

Состав `_MyProject` без `.meta`: 154 `.cs`, 38 `.asset`, 5 `.prefab`, 15 `.mat`, 15 `.shader`, 3 `.asmdef`, 3 `.ttf`, 2 `.unity`, 2 `.mp3`, 1 `.png`, 1 `.inputactions`, 1 `.scenetemplate`.

## Сборки и namespaces

Только grid-модуль вынесен в отдельные asmdef:

| Assembly | Путь | Ссылки | Назначение |
| --- | --- | --- | --- |
| `Game.Grid.Domain` | `Assets/_MyProject/_Scripts/Features/Grid/Domain/Game.Grid.Domain.asmdef` | Нет, `noEngineReferences: true` | Чистая модель сетки без UnityEngine. |
| `Game.Grid.Presentation` | `Assets/_MyProject/_Scripts/Features/Grid/Presentation/Game.Grid.Presentation.asmdef` | `Game.Grid.Domain`, `UnityEngine.UI` | UI/render сетки. |
| `Game.Grid.Composition` | `Assets/_MyProject/_Scripts/Features/Grid/Composition/Game.Grid.Composition.asmdef` | `Game.Grid.Domain`, `Game.Grid.Presentation` | Bootstrapper, связывает domain и view. |

Остальные runtime-скрипты компилируются в `Assembly-CSharp`, editor-скрипты - в `Assembly-CSharp-Editor`.

## Главные runtime-потоки

### 1. Старт сцены и нод

1. `Game.unity` содержит scene objects, сервисы и UI.
2. `NodeGraphBootstrapper` получает `NodeFactory` и список начальных `nodeId`.
3. `NodeFactory` берёт `NodeDefinition` из `NodeCatalog.asset`.
4. `NodeFactory` инстанцирует prefab, ищет `NodeView`.
5. `NodeView` как тонкий фасад применяет legacy-поля или JSON-definition и делегирует визуал, генерацию stats/ports, layout, registry, legacy ports и runtime behaviours профильным helper-классам.

### 2. Данные ноды

`NodeDefinition` - Unity `ScriptableObject`, который хранит:

- `id`, `kind`, `title`
- `prefab`
- ссылку на `NodeJsonDefinitionAsset`
- правила `isUnique`, `canMove`, `canDelete`
- legacy цвета/размер/ports

`NodeJsonDefinitionAsset` хранит JSON-строку с:

- визуальным размером и цветами
- стартовым кодом IDE в `initialCode`
- строками stats
- input/output ports
- правилами совместимости портов

### 3. Соединения нод

1. `GeneratedNodePortView` ловит pointer/drag события.
2. `NodeConnectionDragController` рисует preview line от output-порта.
3. При отпускании на input-порт проверяется `NodePortCompatibilityUtility.CanConnect`.
4. Связь сохраняется в `NodeGraphConnectionModel` как `NodeConnectionData`.
5. `NodeConnectionView` рисует bezier-линию и позволяет удалить связь правым кликом.
6. Если линию отпустили в пустоту, `NodeGraphContextMenuController` показывает меню только с совместимыми нодами и может сразу подцепить новую ноду к источнику.

### 4. IDE и build

1. Двойной клик по ноде обрабатывает `IdeOpenNodeOnDoubleClick`.
2. `IdeRuntimeController` открывает IDE-окно и документ в `IdeCodeTabsController`.
3. `IdeInputCodeEditor` отображает код, подсветку, line numbers, статус caret, `Tab`-отступы и локальную undo/redo history (`Ctrl+Z`, `Ctrl+Y`, `Ctrl+Shift+Z`).
4. `IdeAutocompleteController` показывает autocomplete по текущему token.
5. `IdeBuildButtonController` сохраняет активный документ и запускает `IdeBuildAnalyzer`.
6. `IdeBuildAnalyzer` проверяет DSL: `module`, `mode`, `require`, `budget`, `export`, `if/end`, известные statements и module-specific правила.
7. `ModuleBuildSettings.Parse` извлекает `mode`, `platform`, `scanTraffic`, `allowUnknownPorts` в `IdeBuildReport`.
8. Результат уходит в `NodeCodeRegistry`, включая код, build report и module settings.
9. `TimeModuleSpeedApplier` применяет `module Time` к `GameTimeService`, если build успешный.
10. `IdeOutputConsoleView` выводит build log.

### 5. OS runtime metrics

1. `OsRuntimeMetricsService` собирает активные `NodeView`.
2. Находит root-ноду по `config.RootNodeDefinitionId`, сейчас это `system_core`.
3. BFS-обходом идёт по `NodeGraphConnectionModel.Connections`.
4. Если включены требования, польза от ноды учитывается только после успешного build в `NodeCodeRegistry`.
5. `OsRuntimeMetricsConfig` даёт base metrics, penalties, jitter, smoothing, `NodeMetricRule` и `networkEraBenchmarks`.
6. `OsRuntimeMetricsService` применяет режимы сильных нод как компромиссы: `firewall`, `file_system`, `memory_manager`, `display_manager`.
7. Для сети сервис берёт текущий absolute day из `GameTimeService`, оценивает `NetworkSpeedMbps` через `OsNetworkEraEvaluator` и добавляет штрафы устаревания к stability/load, если скорость уже слабая для текущего года.
8. Сервис рассчитывает:
   - `OsRuntimeMetrics`
   - `OsDataRuntimeMetrics`
   - `OsKernelRuntimeMetrics`
   - `OsSecurityRuntimeMetrics`
   - `OsControlRuntimeMetrics`
9. Runtime-скрипты нод (`SystemCoreNodeRuntime`, `OsBranchNodeRuntime`, `GameInfoNodeRuntime`, `UserReportsNodeRuntime`) пишут значения обратно в stats ноды через `NodeView.TrySetStatValue`.

### 6. World-сервисы

| Сервис | Путь | Назначение |
| --- | --- | --- |
| `GameTimeService` | `Features/World/GameTimeService.cs` | Игровое время: старт 1995, 30 дней в месяце, speed/paused, текст clock/date/status. |
| `EraNewsService` | `Features/World/News/EraNewsService.cs` | Берёт текущую новость по absolute day из `EraNewsDatabase`. |
| `EraNewsDatabase` | `Common/EraNewsDatabase/EraNewsDatabase.cs` | ScriptableObject со списком исторических событий, есть default events 1995. |
| `UserReportsService` | `Features/World/Reports/UserReportsService.cs` | Генерирует пользовательские отзывы по OS metrics, mood, active users, false blocks и отсутствующим обязательным модулям текущего года. |

### 7. Research

1. `ResearchConfig.asset` хранит список `ResearchDefinition`: id, tab id, title, description, cost days, grid position, prerequisites, mutual exclusions и unlock node ids.
2. `ResearchService` держит completed/in-progress sets, стартует research, слушает `GameTimeService.TimeChanged` и завершает исследования по absolute day.
3. `ResearchTreePresenter` берёт текущий tab id, клонирует `ResearchButtonView` template, раскладывает кнопки по `ResearchGridLayout` и рисует зависимости/исключения через `ResearchLinkLineView`.
4. `ResearchTabsController` переключает page CanvasGroup-ы и синхронизирует tab id с presenter.
5. `ResearchWindowController` открывает/закрывает окно через кнопки или `InputActionReference`, поддерживает fade/scale animation и защиту от text input focus.
6. `ResearchTooltipView` показывает title/body/research time и следует за курсором внутри clamp root.
7. `ResearchConfig.IsNodeUnlockedByCompletedResearch` даёт точку подключения unlock-логики нод к завершённым исследованиям.

## `_Scripts/Common`

| Путь | Назначение |
| --- | --- |
| `Common/NodeDefinitions/NodeJsonDefinitionAsset.cs` | ScriptableObject с JSON-описанием ноды, DTO `NodeJsonDefinition`, `NodeJsonStat`, `NodeJsonPort`; поддерживает `initialCode` для стартового кода IDE и `mode` для fallback-шаблона. |
| `Common/NodeDefinitions/NodeCatalog.cs` | ScriptableObject-каталог `NodeDefinition[]`, lookup по id, проверка дублей. |
| `Common/NodeCatalog/NodeCatalog.asset` | Реальный каталог нод, на него смотрит `NodeFactory` и context menu. |
| `Common/NodeDefinition/...` | `NodeDefinition.asset` для рабочих нод каталога. |
| `Common/Data/Nodes/...` | JSON-definition assets и другие перенесённые данные нод. Это источник JSON-визуала/stats/ports. |
| `Common/Data/Research/ResearchConfig.asset` | Реальный config дерева исследований: вкладки, стоимость, prerequisites, mutual exclusions и unlock-и нод. |
| `Common/Research/ResearchConfig.cs` | ScriptableObject-config исследований, lookup по id, проверка mutual exclusion и helper unlock-а нод по завершённым исследованиям. |
| `Common/Research/ResearchDefinition.cs` | Serializable definition одного исследования: id, tab id, title, description, cost days, grid position, prerequisites, mutual exclusions, unlocked node ids. |
| `Common/Research/ResearchState.cs` | Read-only runtime DTO состояния исследования: status, start/complete day, remaining days. |
| `Common/Research/ResearchStatus.cs` | Enum состояний research: `Locked`, `Available`, `InProgress`, `Completed`, `Excluded`. |
| `Common/OS/OsRuntimeMetricsConfig.cs` | Конфиг расчёта OS metrics и DTO-структуры метрик, включая побочные показатели сильных нод и настройки сетевого устаревания по эпохам. |
| `Common/OS/OsReleaseGrowthProfile.cs` | ScriptableObject-профиль прогрессии релиза: discovery, word of mouth, bad reputation churn, rating smoothing, лимит installations и audience modifiers от репутации игрока. |
| `Common/OS/OsReleaseGrowthResult.cs` | DTO результата одного шага релизной прогрессии: installations delta, rating, reception, update status. |
| `Common/OS/Requirements/OsModuleRequirementProfile.cs` | ScriptableObject-конфиг годовых требований к OS-модулям: какие module id становятся обязательными с указанного года и какой штраф к качеству релиза даёт отсутствие. |
| `Common/OS/Requirements/OsAnnualModuleRequirement.cs` | Serializable годовой набор требований, который начинает действовать с `year` и дальше остаётся обязательным. |
| `Common/OS/Requirements/OsRequiredModule.cs` | Serializable требование одного модуля: `moduleId`, display name, альтернативные id и penalty за отсутствие. |
| `Common/OS/Requirements/OsModuleRequirementEvaluation.cs` | DTO результата проверки требований: missing modules, status text и суммарный quality penalty. |
| `Common/OS/Competitors/OsCompetitorConfig.cs` | ScriptableObject-config конкурентов: interval роста и список компаний с OS, стартовыми пользователями и growth-настройками. |
| `Common/OS/Competitors/OsCompetitorDefinition.cs` | Serializable definition одного конкурента: company, operating system, starting users, flat/percent growth и max users. |
| `Common/OS/Competitors/OsCompetitorState.cs` | Read-only DTO текущего состояния конкурента или игрока для UI. |
| `Common/OS/Competitors/OsCompetitorLeaderboardBuilder.cs` | Чистый builder рейтинга competitors page: добавляет игрока к snapshot, сортирует по users и стабилизирует tie-break. |
| `Common/OS/Reputation/OsPlayerReputationProfile.cs` | ScriptableObject-баланс репутации игрока: gain/loss от качества релиза, install bonus, tier text и множители audience для будущих релизов. |
| `Common/OS/Reputation/OsReleaseAudienceModifiers.cs` | DTO audience-множителей: first wave, discovery, recommendations и bad reputation churn. |
| `Common/OS/OsNetworkEraBenchmark.cs` | Данные одного годового сетевого benchmark: дата, thresholds Mbps и максимальные штрафы. |
| `Common/OS/OsNetworkEraEvaluator.cs` | Чистая оценка скорости сети относительно текущего года: quality label, score и штрафы к stability/load. |
| `Common/OS/OsNetworkEraAssessment.cs` | Результат оценки сети для runtime metrics. |
| `Common/OS/OsNetworkEraQuality.cs` | Enum качества сети: `Obsolete`, `Aging`, `Good`, `Excellent`. |
| `Common/EraNewsDatabase/EraNewsDatabase.cs` | База новостей эпохи. |

## `_Scripts/Features`

### Camera

| Файл | Назначение |
| --- | --- |
| `Features/Camera/Presentation/GridCameraPanZoomController.cs` | Pan/zoom ортографической камеры: WASD/стрелки, mouse drag, wheel zoom-to-mouse, Home reset, блокировка над UI. Namespace сейчас `Features.Camera.Presentation`, не `Game...`. |

### Console

| Файл | Назначение |
| --- | --- |
| `Features/Console/Presentation/IdeConsoleController.cs` | Внутриигровая shell-консоль с prompt, history, `help`, `ls`, `cd`, `cat`, `time`, `system`; использует виртуальную FS. |
| `Features/Console/Output/IdeOutputConsoleView.cs` | Read-only build output console с rich-text цветами, max lines и autoscroll. |

### Grid

| Файл | Назначение |
| --- | --- |
| `Features/Grid/Domain/GridPosition.cs` | Value object позиции сетки. |
| `Features/Grid/Domain/GridCell.cs` | Cell model, включая walkable-state. |
| `Features/Grid/Domain/GridData.cs` | Чистая модель сетки `width x height`, хранит cells и событие `CellChanged`. |
| `Features/Grid/Presentation/CanvasGridView.cs` | UGUI `MaskableGraphic`, рисует canvas-grid, pan/zoom-aware линии. |
| `Features/Grid/Presentation/InfiniteGridRenderer.cs` | MeshRenderer-версия бесконечной world-grid, перестраивается от камеры. |
| `Features/Grid/Presentation/GraphPanoramaBackgroundRenderer.cs` | MeshRenderer для панорамного фона за графом. |
| `Features/Grid/Presentation/GridPresenter.cs` | Presenter, применяет размер domain-grid к `CanvasGridView`. |
| `Features/Grid/Composition/GridBootstrapper.cs` | Создаёт `GridData` и `GridPresenter`. |
| `Features/Grid/GridData.cs` | Legacy stub MonoBehaviour без namespace. Дублирует имя `GridData`, но не содержит логики. |

### IDE

| Файл | Назначение |
| --- | --- |
| `Features/IDE/IdeInputCodeEditor.cs` | TMP-based code editor: initial code, syntax highlight, line numbers, Tab indentation, caret status, undo/redo (`Ctrl+Z`, `Ctrl+Y`, `Ctrl+Shift+Z`); подсвечивает `mode` и значения режимов. |
| `Features/IDE/IdeAutocompleteControllerIdeAutocompleteController.cs` | Autocomplete dropdown; runtime домерживает дефолтный DSL-словарь (`mode`, режимы и т.д.) к сериализованным entries. Имя файла содержит дублирование `IdeAutocompleteControllerIdeAutocompleteController`. |
| `Features/IDE/IdeAutocompleteSuggestionView.cs` | View одной строки autocomplete. |
| `Features/IDE/Build/IdeBuildAnalyzer.cs` | Статический анализатор DSL, строит `IdeBuildReport`, ошибки/предупреждения/effects и сохраняет module settings. |
| `Features/IDE/Runtime/IdeCodeTemplateProvider.cs` | Единый источник fallback-шаблонов кода для IDE, если JSON-нода не задала `initialCode`; учитывает JSON `mode`. |
| `Features/IDE/Runtime/IdeNodeDocumentResolver.cs` | Превращает `NodeView` в IDE-документ: document id, definition id, title и initial/default code. |
| `Features/IDE/Runtime/ModuleOperatingMode.cs` | Enum и utility для режимов `balanced`, `strict`, `permissive`, `aggressive`, `safe`, `performance`. |
| `Features/IDE/Runtime/ModuleBuildSettings.cs` | DTO настроек модуля, которые build сохраняет и runtime читает; содержит parse API для `mode`, `platform`, `scanTraffic`, `allowUnknownPorts`. |
| `Features/IDE/Runtime/TimeModuleSpeedParser.cs` | Парсер `module Time` и строк `speed = 60x`, `gameSpeed: 0.5x`. |
| `Features/IDE/Runtime/TimeModuleSpeedApplier.cs` | Применяет успешный Time module к `GameTimeService`. |
| `Features/IDE/Presentation/IdeRuntimeController.cs` | Singleton-точка открытия IDE-документов. |
| `Features/IDE/Presentation/IdeOpenNodeOnDoubleClick.cs` | Ловит double-click по ноде и открывает документ через `IdeNodeDocumentResolver`. |

### Input

| Файл | Назначение |
| --- | --- |
| `Features/Input/GameInputBlocker.cs` | Глобальный static blocker ввода gameplay/camera/drag. |
| `Features/Input/BlocksCameraInput.cs` | Marker component: UI под ним блокирует camera input. |
| `Features/Input/UiPointerBlockUtility.cs` | Raycast через EventSystem, проверяет pointer over `BlocksCameraInput`. |
| `Features/Input/IdeGameplayInputBlocker.cs` | Блокирует gameplay input, когда IDE root CanvasGroup или указанные IDE groups видимы; освобождает blocker при disable/destroy. |
| `Features/Input/New Folder with Selection` | Пустая/служебная папка, по названию выглядит как случайно созданная Unity folder. |

### Selection

| Файл | Назначение |
| --- | --- |
| `Features/Selection/Runtime/SelectableNode.cs` | Компонент состояния выделения на ноде: `IsSelected`, `SelectionChanged`, ссылки на `NodeView` и `RectTransform`. |
| `Features/Selection/Runtime/NodeSelectionSet.cs` | Чистая runtime-структура выбранных нод без Unity-ввода и UI. |
| `Features/Selection/Runtime/NodeSelectionService.cs` | Scene service выделения: общий API для box-select, click-select и удаления. |
| `Features/Selection/Runtime/NodeDeletionService.cs` | Удаляет выбранные ноды и просит connection controller очистить все связанные линии/модель. |
| `Features/Selection/Presentation/NodeSelectBoxController.cs` | Логика drag select box, modifiers Shift/Ctrl, сравнение screen-space rect нод. |
| `Features/Selection/Presentation/SelectBoxView.cs` | Adapter для уже созданного UI RectTransform select box; ничего не создает сам. |
| `Features/Selection/Presentation/RectTransformScreenUtility.cs` | Screen-space conversion для смешанных canvas, включая world-space ноды и overlay select box. |
| `Features/Selection/Presentation/NodeSelectionClickController.cs` | Click-to-select для одной ноды, additive/toggle через Shift/Ctrl. |
| `Features/Selection/Presentation/NodeDeleteInputController.cs` | Input controller для Delete/Backspace, вызывает `NodeDeletionService`. |
| `Features/Selection/Presentation/SelectedNodePrimeTweenFeedback.cs` | PrimeTween feedback выделения: ярче header и слегка больше scale target. |
| `Features/Selection/Categories/Runtime/NodeCategoryFrameService.cs` | Создаёт frame/category вокруг текущего выделения из 2+ нод через назначенный prefab рамки; default padding 48, рамка по умолчанию интерактивно выше нод. |
| `Features/Selection/Categories/Runtime/SelectedNodeBoundsCalculator.cs` | Чистый расчёт local bounds выбранных нод относительно frame root. |
| `Features/Selection/Categories/Presentation/NodeCategoryFrameView.cs` | Adapter для prefab рамки категории: применяет title/position/size и управляет `CanvasGroup.blocksRaycasts`/raycast graphics. |
| `Features/Selection/Categories/Presentation/NodeCategoryFrameInputController.cs` | Pointer controller категории: hover, selected по ПКМ, ЛКМ сразу переводит ввод к нодам и пробрасывает клик объекту под рамкой. |

### Nodes

| Подпапка | Назначение |
| --- | --- |
| `Nodes/Definitions` | Runtime-тип `NodeDefinition` и legacy `NodePortDefinition`. |
| `Nodes/Domain` | Enums: `NodeKind`, `NodePortDirection`, `NodeSignalType`. |
| `Nodes/Composition` | `NodeFactory`, создание нод из каталога. |
| `Nodes/Presentation` | Верхний слой UI ноды: drag, stat row и runtime initializer. Подпапки держат core-view, generated UI и ports отдельно. |
| `Nodes/Presentation/Core` | `NodeView`-фасад, visual presenter, editor preview, color parser и CircleStatus presenter/evaluator. |
| `Nodes/Presentation/Generated` | Генерация JSON-driven stats/ports/layout/registry. |
| `Nodes/Presentation/Ports` | Legacy/generated port view и правила совместимости портов. |
| `Nodes/Lines/Presentation` | Создание, рисование и удаление connection-lines. |
| `Nodes/ContextMenu/Presentation` | Меню spawn нод, категории, popup animation, loose-connection flow и команда `Create Category` для выделения 2+ нод. |
| `Nodes/CodeRegistry` | Registry успешных build-результатов модулей. |
| `Nodes/Runtime` | Runtime layer нод, разнесён по graph/behaviours/user reports. |
| `Nodes/Runtime/Graph` | Connection model, connection DTO и bootstrap начальных нод. |
| `Nodes/Runtime/Behaviours` | Базовые/runtime behaviours обычных нод. |
| `Nodes/Runtime/UserReports` | Runtime updater и history-view пользовательских отчётов. |
| `Nodes/DocsNode` | Документационная нода с вкладками/страницами. |

Ключевые файлы `Nodes`:

| Файл | Назначение |
| --- | --- |
| `Nodes/Definitions/NodeDefinition.cs` | ScriptableObject ноды, связь prefab + JSON + правила. |
| `Nodes/Composition/NodeFactory.cs` | Spawn by id, unique nodes, bind `NodeView`. |
| `Nodes/Presentation/Core/NodeView.cs` | Тонкий MonoBehaviour-фасад ноды: public API, JSON asset binding, state assignment и orchestration helper-классов. Сохраняет serialized fields для prefab compatibility. |
| `Nodes/Presentation/Core/NodeViewVisualPresenter.cs` | Заголовок, цвета background/header/accent, drag-enabled state и header layout. |
| `Nodes/Presentation/Core/NodeCircleStatus.cs` | Enum состояния круглого индикатора ноды: `Healthy`, `Limit`, `Error`. |
| `Nodes/Presentation/Core/NodeCircleStatusEvaluator.cs` | Оценивает обновлённые stats ноды и выбирает худший статус для CircleStatus. |
| `Nodes/Presentation/Core/NodeCircleStatusPresenter.cs` | Применяет цвет к назначенному `Image` круглого индикатора. |
| `Nodes/Presentation/Generated/NodeGeneratedPortRegistry.cs` | Registry generated ports/stats: список портов, lookup по id, lookup stats по label, `TrySetStatValue`. |
| `Nodes/Presentation/Generated/NodeGeneratedContentBuilder.cs` | Создание и очистка generated stats/ports, маркировка `NodeGeneratedElement`, безопасное destroy в editor/runtime. |
| `Nodes/Presentation/Generated/NodeGeneratedLayoutController.cs` | ManualPrefab/AutoCompact layout, JSON size, размеры root-ов, auto-height, rebuild layout. |
| `Nodes/Presentation/Ports/NodeLegacyPortBinder.cs` | Binding/hide старых `NodePortView` для legacy `NodeDefinition`. |
| `Nodes/Presentation/NodeRuntimeBehaviourInitializer.cs` | Инициализация всех `NodeRuntimeBehaviour` на ноде. |
| `Nodes/Presentation/Core/NodeViewEditorPreview.cs` | Editor-only JSON preview scheduling/hash/rebuild logic, весь файл под `#if UNITY_EDITOR`. |
| `Nodes/Presentation/Core/NodeColorParser.cs` | Общий парсер HTML-цветов с fallback. |
| `Nodes/Presentation/NodeStatRowView.cs` | View строки stat. Поддерживает single-text/two-text режимы и цветовые состояния текста: bad red, warning orange, good green. |
| `Nodes/Presentation/Generated/GeneratedNodePortView.cs` | Generated input/output port с pointer events для drag-связей. |
| `Nodes/Presentation/NodeDragController.cs` | Перетаскивание ноды, snap to visual grid, bounds. |
| `Nodes/Presentation/Ports/NodePortCompatibilityUtility.cs` | Правила совместимости портов по JSON restrictions. |
| `Nodes/Lines/Presentation/NodeConnectionDragController.cs` | Drag preview, создание связи, loose menu, удаление связи. |
| `Nodes/Lines/Presentation/NodeConnectionView.cs` | Bezier line Graphic, raycast hit-test по кривой. |
| `Nodes/Runtime/Graph/NodeGraphConnectionModel.cs` | Список связей, события add/remove, context actions. |
| `Nodes/Runtime/Graph/NodeConnectionData.cs` | Serializable DTO одной связи. |
| `Nodes/Runtime/Graph/NodeGraphBootstrapper.cs` | Стартовый spawn нод сцены. |
| `Nodes/CodeRegistry/NodeCodeRegistry.cs` | Хранит code/build state и `ModuleBuildSettings` по document/module aliases. |
| `Nodes/Runtime/Behaviours/SystemCoreNodeRuntime.cs` | Обновляет stats `system_core` из `OsRuntimeMetricsService`. |
| `Features/OS/Runtime/OsBranchNodeRuntime.cs` | Обновляет stats `data`, `kernel`, `security`, `control` и дочерних OS-нод. |
| `Nodes/Runtime/Behaviours/GameInfoNodeRuntime.cs` | Обновляет `time` и `news` ноды. |
| `Nodes/Runtime/UserReports/UserReportsNodeRuntime.cs` | Обновляет summary stats `user_reports`. |
| `Nodes/Runtime/UserReports/UserReportsHistoryView.cs` | Большой runtime-view истории user reports. В файле есть mojibake-строки inspector labels/tooltips. |
| `Nodes/DocsNode/DocsNodeView.cs` | Встроенная документационная нода, pages/buttons/next/previous. |

### OS

| Файл | Назначение |
| --- | --- |
| `Features/OS/Runtime/OsRuntimeMetricsService.cs` | Расчёт всех OS metrics по доступным и успешно собранным нодам; применяет mode/tradeoffs сильных нод и сетевое устаревание от `GameTimeService`. Namespace `Game.Nodes.Runtime`, хотя путь `Features/OS`. |
| `Features/OS/Runtime/OsBranchNodeRuntime.cs` | Runtime updater branch-нод, пишет в stats новые побочные метрики: false blocks, fragmentation, leak risk, GPU usage. Тоже namespace `Game.Nodes.Runtime`. |
| `Features/OS/Release/OsReleaseService.cs` | Runtime service публикации OS build: Deploy читает успешный `SystemCore` build, `platform`, проверяет годовые module requirements, затем растит installations/rating через `OsReleaseGrowthProfile` и отдаёт feedback в `OsPlayerReputationService`. |
| `Features/OS/Release/OsReleaseState.cs` | Read-only DTO release-info для UI: channel, supported devices, installs, rating, latest release, status, player reputation и состояние требований модулей. |
| `Features/OS/Competitors/OsCompetitorService.cs` | Runtime service конкурентов: хранит текущих users по каждому конкуренту и постепенно растит их по `OsCompetitorConfig`. |
| `Features/OS/Reputation/OsPlayerReputationService.cs` | Runtime service репутации игрока: хранит reputation между релизами, отдаёт audience modifiers и применяет feedback от качества OS. |

### Research

| Файл | Назначение |
| --- | --- |
| `Features/Research/Runtime/IResearchService.cs` | Минимальный interface research runtime: `Changed`, config, active id, current day, start/status/state/debug API. |
| `Features/Research/Runtime/ResearchService.cs` | Runtime-сервис исследований: стартует доступные исследования, запрещает locked/excluded/in-progress, завершает по игровым дням, поддерживает debug complete/reset. |

### UserInterface

| Файл | Назначение |
| --- | --- |
| `Features/UserInterface/RuntimeTextRefresh.cs` | Общий helper для безопасного runtime-обновления `TMP_Text`, legacy `Text`, `TMP_InputField` и Modern UI Pack `ButtonManager`: ставит текст/цвет, dirty flags, mesh update и layout rebuild. |
| `Features/UserInterface/IdeWindowCanvasGroupController.cs` | Открыть/закрыть IDE-window через CanvasGroup, кнопки, Escape, PrimeTween alpha; поддерживает `excludedGroups`, `hideOnCloseGroups`, восстановление zero-scale на open и refresh child tabs/code tabs. |
| `Features/UserInterface/IdeBottomTabsController.cs` | Нижние вкладки IDE: console/output и прочие панели. |
| `Features/UserInterface/IdeCodeTabsController.cs` | Документы кода, вкладки, initial documents и default DSL snippets. |
| `Features/UserInterface/IdeCodeTabButtonView.cs` | View кнопки code-tab с select/close. |
| `Features/UserInterface/CanvasGroupShiftTabToggle.cs` | Toggle CanvasGroup по Shift+Tab; умеет не открываться, если родительские CanvasGroup скрыты. Namespace `Game.UI`. |
| `Features/UserInterface/Buttons/IdeBuildButtonController.cs` | Главная кнопка Build, запускает analyzer, registry, Time applier, output tab. |
| `Features/UserInterface/OS/OsWindowController.cs` | OS-window controller: CanvasGroup open/close, Tab input, InputAction toggle, close button, PrimeTween alpha + scale animation, выбор default OS tab; `blockInputWhenGroupsVisible` блокирует OS input при видимой IDE/другом UI. |
| `Features/UserInterface/OS/OsDeployBuildButtonManager.cs` | Кнопка Deploy Build: вызывает `OsReleaseService.DeployBuild`, но не запускает IDE build. |
| `Features/UserInterface/OS/OsInfoPresenter.cs` | Presenter OS info pages: System Information, Health System, Release Information, Competitors; выводит module requirements в Release Information, строит ranked competitors list с игроком, поддерживает page bindings и legacy panels. |
| `Features/UserInterface/OS/OsInfoPanelView.cs` | View, которая клонирует `GameObject` row template внутрь Content, поддерживает `OsInfoEntryView`, Modern UI Pack `ButtonManager` row и legacy TMP row fallback. |
| `Features/UserInterface/OS/OsInfoDefaults.cs` | Дефолтные данные OS-window: Stata OS, Health OS fallback, Release Information с цветовыми tone. |
| `Features/UserInterface/OS/OsInfoSection.cs` | Serializable section model для заголовка и entries. |
| `Features/UserInterface/OS/OsInfoEntry.cs` | Serializable row model `label/value/tone`. |
| `Features/UserInterface/OS/Tabs/OsWindowTabsController.cs` | OS page/tab controller: переключает page roots через CanvasGroup и красит кнопки вкладок. |
| `Features/UserInterface/OS/Tabs/OsWindowTabItem.cs` | Serializable binding одной OS-вкладки: id, optional `ButtonManager`/Button, visual targets и page root. |
| `Features/UserInterface/OS/Rows/OsInfoEntryView.cs` | Универсальный row-template компонент для OS info: раздельные label/value/combined text, `ButtonManager.SetText` и tone graphics. |
| `Features/UserInterface/OS/Pages/OsInfoPageBinding.cs` | Binding типа OS info page к `OsInfoPanelView`. |
| `Features/UserInterface/Notifications/NotificationView.cs` | View уведомления: принимает `NotificationEvent`, заполняет Title/Body/StatusCircle, подключает две `ButtonManager`-кнопки и прячет отсутствующие action-кнопки. |
| `Features/UserInterface/Notifications/NotificationAnimator.cs` | Анимация уведомления: fade на root `CanvasGroup` и popup scale на content root через PrimeTween. |
| `Features/UserInterface/Notifications/NotificationButtonBinder.cs` | SRP-биндинг одной `ButtonManager`-кнопки: включает override colors, ставит текст и подписывает click. |
| `Features/UserInterface/Notifications/NotificationEvent.cs` | Serializable payload уведомления: title, body, status, primary/secondary action. |
| `Features/UserInterface/Notifications/NotificationAction.cs` | Serializable action уведомления: label, close-after-click и UnityEvent callback. |
| `Features/UserInterface/Notifications/NotificationStatus.cs` | Enum статуса уведомления: info/success/warning/error. |
| `Features/UserInterface/Notifications/NotificationStatusColorPalette.cs` | Цветовая палитра для `StatusCircle` и опциональной окраски текста кнопок. |
| `Features/UserInterface/Research/ResearchWindowController.cs` | Research window: CanvasGroup open/close, Button/ButtonManager open-close, InputAction toggle, optional text-input guard, PrimeTween alpha + scale animation. |
| `Features/UserInterface/Research/ResearchTreePresenter.cs` | Presenter research tree: выбирает tab root, клонирует research button template, раскладывает по grid, создаёт dependency/mutual-exclusion lines и обновляет states. |
| `Features/UserInterface/Research/ResearchButtonView.cs` | View одной research-кнопки: title/cost/status, tooltip binder, optional in-progress material, colors per state, click start research. |
| `Features/UserInterface/Research/ResearchGridLayout.cs` | Настройки координат сетки research button-ов. |
| `Features/UserInterface/Research/ResearchScrollContentLayout.cs` | Расчёт scroll content bounds под research tree. |
| `Features/UserInterface/Research/ResearchLinkLineView.cs` | UGUI `Graphic`, рисует линию между research button-ами. |
| `Features/UserInterface/Research/ResearchTooltipBinder.cs` | Pointer binder для research button, показывает/двигает/прячет tooltip. |
| `Features/UserInterface/Research/ResearchTooltipView.cs` | Tooltip research: title/body/research time, follow cursor, clamp inside root, fade/scale через `NotificationAnimator`. |
| `Features/UserInterface/Research/Tabs/ResearchTabsController.cs` | Research tabs: переключает CanvasGroup page roots, красит tab buttons и синхронизирует tree presenter. |
| `Features/UserInterface/Research/Tabs/ResearchTabItem.cs` | Serializable binding одной research-вкладки: id, page root/group, Button/ButtonManager, visual targets. |

### World

| Файл | Назначение |
| --- | --- |
| `Features/World/GameTimeService.cs` | Игровые дата/время и скорость. |
| `Features/World/News/EraNewsService.cs` | Текущая новость по времени. |
| `Features/World/Reports/UserReportsService.cs` | Генератор пользовательских отзывов по состоянию OS, включая отдельные жалобы на false blocks и устаревшие/отсутствующие модули. |
| `Features/World/Reports/UserReportModels.cs` | DTO и enums отзывов: severity/mood/entry. |

## Ноды в текущем каталоге

`NodeCatalog.asset` сейчас ссылается на 11 `NodeDefinition` assets:

| Id | Title | Kind | Файл |
| --- | --- | --- | --- |
| `system_core` | System Core | `SystemCore = 0` | `Common/NodeDefinition/System/SystemCore.asset` |
| `data` | Data | `Data = 10` | `Common/NodeDefinition/Data/Data.asset` |
| `kernel` | Kernel | `Kernel = 11` | `Common/NodeDefinition/Kernel/Kernel.asset` |
| `security` | Security | `Security = 12` | `Common/NodeDefinition/Security/Security.asset` |
| `control` | Control | `Control = 13` | `Common/NodeDefinition/Control/Control.asset` |
| `file_system` | File System | `FileSystem = 30` | `Common/NodeDefinition/Data/FileSystem.asset` |
| `firewall` | Firewall | `Firewall = 4` | `Common/NodeDefinition/Network/Firewall.asset` |
| `time` | Time | `Time = 60` | `Common/NodeDefinition/Time/Time.asset` |
| `news` | News | `EraNews = 61` | `Common/NodeDefinition/Era News/Era News.asset` |
| `user_reports` | User Reports | `UserReports = 62` | `Common/NodeDefinition/User Reports/User Reports.asset` |
| `node.memory.manager` | Memory Manager | `Storage = 3` | `Common/NodeDefinition/Memory Manager.asset` |

Важно: JSON asset для Memory Manager в `Common/Data/Nodes/Data/Memory Manager.asset` имеет id `memory_manager`, а `NodeDefinition/Memory Manager.asset` имеет legacy id `node.memory.manager`. Это выглядит как несостыкованность между старой и новой схемой id.

## JSON node data

`Common/Data/Nodes` содержит JSON-definition assets:

Все JSON-ноды могут задавать `initialCode` и `mode`. `initialCode` является точным стартовым кодом IDE, а `mode` используется только для fallback-шаблона, если стартовый код пустой.

| Id | Title | Stats | Ports |
| --- | --- | --- | --- |
| `system_core` | System Core | System Stability, Network Speed, System Load, Memory Usage, Storage Usage | `data:Output`, `security:Output`, `control:Output`, `kernel:Output`, `user_reports:Output` |
| `data` | Data | File System, Read Speed, Data Safety, Index Size | `system_core:Input`, `file_system:Output`, `backup:Output`, `compression:Output`, `recovery:Output` |
| `kernel` | Kernel | Kernel Type, Processes Limit, Crash Risk, Response Time | `system_core:Input`, `memory:Output`, `process:Output`, `scheduler:Output`, `recovery:Output` |
| `security` | Security | Threat Level, Firewall, Vulnerabilities, Encryption | `system_core:Input`, `firewall:Output`, `permissions:Output`, `antivirus:Output`, `encryption:Output` |
| `control` | Control | Device Support, Input Delay, Audio Layer, Power Control | `system_core:Input`, `display:Output`, `sound:Output`, `devices:Output`, `power:Output` |
| `file_system` | File System | Used Space, Files Indexed, Fragmentation, Read Speed, Write Speed | `data:Input`, `storage:Output`, `directories:Output`, `permissions:Output`, `cache:Output`, `journal:Output` |
| `firewall` | Firewall | Protection, Blocked Ports, Traffic Scan, False Blocks | `security:Input`, `rules:Output`, `traffic_filter:Output`, `port_control:Output`, `security_logs:Output` |
| `memory_manager` | Memory Manager | Allocated RAM, Leak Risk, Page Faults, Access Time | `kernel:Input`, `paging:Output`, `allocation:Output`, `cache:Output`, `cleanup:Output` |
| `display_manager` | Display Manager | Resolution, Color Depth, UI Latency, GPU Usage | `control:Input`, `windows:Output`, `cursor:Output`, `renderer:Output`, `themes:Output` |
| `time` | Time | Clock, Date, Speed, Status | Нет ports |
| `news` | News | Date, Headline, Description | Нет ports |
| `user_reports` | User Reports | Active Users, Mood, Reports, Critical | `system_core:Input` |

Также рядом с JSON-нودами лежат основные config assets:

- `Common/Data/OS/OsRuntimeMetricsConfig.asset`
- `Common/Data/OS/OsModuleRequirementProfile.asset`
- `Common/Data/OS/OsReleaseGrowthProfile.asset`
- `Common/Data/OS/OsPlayerReputationProfile.asset`
- `Common/Data/OS/OsCompetitorConfig.asset`
- `Common/Data/Research/ResearchConfig.asset`
- `Common/Data/Nodes/Era News/EraNewsDatabase/EraNewsDatabase.asset`

## Prefab

| Файл | Назначение |
| --- | --- |
| `Prefab/Node_docs.prefab` | Документационная нода. |
| `Prefab/Node_security_Preview.prefab` | Preview/security prefab. |
| `Prefab/Node_user_reports_Preview.prefab` | Preview/user reports prefab. |
| `Prefab/DropdownPanel.prefab` | UI dropdown, используется для меню/autocomplete. |
| `Prefab/UserInterface/ResearchButton.prefab` | Template research-кнопки для `ResearchTreePresenter`. |
| `Prefab/UserInterface` | Папка UI-prefab-ов. Сейчас содержит `ResearchButton.prefab`. |

## Art

| Путь | Назначение |
| --- | --- |
| `Art/_Shaders/GraphComputerGridBackground.shader` | Фон/сеточная графика. |
| `Art/_Shaders/GraphComputerPanoramaBackground.shader` | Панорамный background графа. |
| `Art/_Shaders/GraphComputerNodePanelBackground.shader` | Фон панели ноды. |
| `Art/_Shaders/GraphComputerNodeHeader.shader` | Шейдер header ноды. |
| `Art/_Shaders/NodeConnectionFlow.shader` | Визуал connection-line flow. |
| `Art/_Shaders/ResearchProgressCompiler.shader` | Optional effect для research in progress: compiler/progress pulse. |
| `Art/_Shaders/NodeSignalBloom.shader` | Optional glow для активных/важных нод. |
| `Art/_Shaders/PacketTraceFlow.shader` | Optional data packet flow для network/data UI. |
| `Art/_Shaders/FirewallPulseBarrier.shader` | Optional pulse barrier для firewall/security. |
| `Art/_Shaders/MemoryCorruptionGlitch.shader` | Optional glitch для memory/crash/error states. |
| `Art/_Shaders/TerminalGlass.shader` | Optional glass overlay для IDE/console. |
| `Art/_Shaders/WorldDataRain.shader` | Optional data-rain background для world/OS panels. |
| `Art/_Shaders/LinkOverdrive.shader` | Optional overdrive effect для overloaded/active connections. |
| `Art/_Shaders/NeuralCircuitAura.shader` | Optional circuit aura для сложных/high-tech нод. |
| `Art/_Shaders/NodeHeatMapOverlay.shader` | Optional load/heat overlay для high load nodes. |
| `Art/Materials/M_*.mat` | Материалы для этих шейдеров: `M_GridBackground`, `M_NodeConnectionFlow`, `M_ResearchProgressCompiler`, `M_TerminalGlass`, `M_WorldDataRain` и т.д. |
| `Art/Font/NotoSans*`, `Art/Font/Literata*` | TTF и TMP SDF assets для UI/нод. |
| `Art/Textures/Spiral/...png` | Raster texture asset, сейчас лежит в подпапке `Spiral`. |

## Settings

| Файл | Назначение |
| --- | --- |
| `Settings/UniversalRP.asset` | URP pipeline asset. |
| `Settings/UniversalRenderPipelineGlobalSettings.asset` | Global URP settings. |
| `Settings/Renderer2D.asset` | 2D renderer settings. |
| `Settings/InputSystem_Actions.inputactions` | Input System action asset. |
| `Settings/DefaultVolumeProfile.asset` | Volume profile. |
| `Settings/Lit2DSceneTemplate.scenetemplate` | Scene template. |
| `Settings/Scenes/URP2DSceneTemplate.unity` | Template scene. |

## Sounds

| Путь | Назначение |
| --- | --- |
| `Sounds/Music/adiiswanto-focus-coding-lo-fi-music-475927.mp3` | Музыка/ambient. |
| `Sounds/Music/ronaldoreyz-midnight-code-drift-deep-focus-coding-ambient-464923.mp3` | Музыка/ambient. |
| `Sounds/SoundsEffects` | Папка под SFX, сейчас без файлов без `.meta`. |

## Сторонние ассеты

| Путь | Что внутри | Как использовать |
| --- | --- | --- |
| `Assets/Modern UI Pack` | UI prefabs, scripts, scenes, textures, fonts, editor tooling. | Не смешивать с first-party логикой. Использовать как vendor UI kit. |
| `Assets/TextMesh Pro` | TMP fonts/resources/shaders. | Системная зависимость текстового UI. |
| `Assets/Unity-UI-Rounded-Corners` | `ImageWithRoundedCorners`, shaders, examples. | Для скруглений UI. Лучше не править vendor-код без причины. |
| `Assets/Plugins/PrimeTween` | Installer, demo, локальный tgz package. | Используется в `IdeWindowCanvasGroupController`, `IdeBottomTabsController`, `CanvasGroupShiftTabToggle`, popup animation. |

## Recovery и backup

Эти папки есть в корне и прописаны в `.gitignore`:

- `_EMERGENCY_RECOVERY`
- `_PANIC_BACKUP`
- `_RECOVERED_FROM_VSCODE_HISTORY`
- `_RECOVERED_NODE_SCRIPTS`

`_RECOVERED_NODE_SCRIPTS` содержит старые/восстановленные версии node-файлов: `NodeView`, `NodeFactory`, `NodeGraphBootstrapper`, `NodeConnectionView`, `NodeCatalog`, `NodeDefinition` и другие. Это архив для сравнения, не рабочий runtime-код.

## Замеченные навигационные риски

- В worktree уже есть пользовательские изменения, включая перемещения/удаления старых `Common/Data/...` assets и новые `Common/Data/Nodes/...` assets. Не делать массовый cleanup без отдельной проверки.
- `Features/Grid/GridData.cs` - пустой legacy MonoBehaviour вне namespace, дублирует имя реальной модели `Game.Grid.Domain.GridData`.
- `Features/IDE/IdeAutocompleteControllerIdeAutocompleteController.cs` - имя файла с повтором, класс внутри нормальный: `IdeAutocompleteController`.
- Часть файлов содержит mojibake в русских комментариях/InspectorName/Tooltip, например `UserReportsHistoryView.cs`, `NodeTypes.cs`, `NodePortView.cs`, `NodeDragController.cs`. Это похоже на проблему кодировки уже существующих строк.
- `Features/OS/Runtime/OsRuntimeMetricsService.cs` лежит в `Features/OS`, но namespace у него `Game.Nodes.Runtime`. Это рабоче, но усложняет поиск по namespace.
- `NodeDefinition/Memory Manager.asset` использует id `node.memory.manager`, а JSON-data asset использует `memory_manager`. Метрики ожидают `memory_manager`.

## Где что менять

| Задача | Идти сюда |
| --- | --- |
| Добавить новую ноду | `Common/Data/Nodes/...` для JSON, `Common/NodeDefinition/...` для `NodeDefinition`, затем `Common/NodeCatalog/NodeCatalog.asset`. |
| Задать стартовый код ноды | Поле `initialCode` в JSON asset из `Common/Data/Nodes/...`; формат языка описан в `GRAPH_OS_LANGUAGE.md`. |
| Задать fallback-режим ноды | Поле `mode` в JSON asset. Если `initialCode` задан, режим писать внутри DSL как `mode = strict`/`balanced`/`permissive` и т.д. |
| Изменить визуал нод | `NodeViewVisualPresenter.cs`, `NodeGeneratedLayoutController.cs`, `NodeGeneratedContentBuilder.cs`, `NodeCircleStatusPresenter.cs`, JSON assets в `Common/Data/Nodes`, назначенные prefab-ы из `Prefab`, материалы в `Art/Materials`. `NodeView.cs` лучше держать фасадом. |
| Изменить правила CircleStatus нод | `NodeCircleStatusEvaluator.cs` для порогов/ключевых слов, `NodeView.cs` для serialized цветов и ссылки на `circleStatusImage`. |
| Изменить выделение/select box/delete нод | `Features/Selection/...`, плюс `NodeConnectionDragController.DeleteConnectionsForNode` для очистки линий при удалении ноды. |
| Изменить рамки категорий выбранных нод | `Features/Selection/Categories/...` и `NodeGraphContextMenuController.cs`; prefab рамки назначается в `NodeCategoryFrameService`, hover/selected/pass-through живут в `NodeCategoryFrameInputController`. |
| Изменить цвета/состояния статов | `NodeStatRowView.cs`: правила bad/warning/good, default colors и single-text/two-text поведение. |
| Изменить правила соединений | JSON ports в `NodeJsonDefinitionAsset`, `NodePortCompatibilityUtility.cs`. |
| Изменить создание/меню нод | `NodeFactory.cs`, `NodeGraphContextMenuController.cs`, `NodeCatalog.asset`. |
| Изменить build DSL | `IdeBuildAnalyzer.cs`, `ModuleBuildSettings.cs`, `IdeCodeTemplateProvider.cs`, `TimeModuleSpeedParser.cs`, `GRAPH_OS_LANGUAGE.md`. |
| Изменить режимы/компромиссы нод | `ModuleOperatingMode.cs`, `ModuleBuildSettings.cs`, `OsRuntimeMetricsService.cs`, `OsRuntimeMetricsConfig.cs`, `GRAPH_OS_LANGUAGE.md`. |
| Изменить результат build на игру | `NodeCodeRegistry.cs`, `OsRuntimeMetricsService.cs`, `TimeModuleSpeedApplier.cs`. |
| Изменить undo/redo, Tab indent или поведение редактора кода | `Features/IDE/IdeInputCodeEditor.cs`. |
| Изменить OS-баланс | `Common/OS/OsRuntimeMetricsConfig.cs` и `Common/Data/OS/OsRuntimeMetricsConfig.asset`. |
| Изменить устаревание сети по годам | `networkEraBenchmarks` в `Common/Data/OS/OsRuntimeMetricsConfig.asset`; логика оценки лежит в `Common/OS/OsNetworkEraEvaluator.cs`. |
| Изменить требования модулей по годам | `Common/Data/OS/OsModuleRequirementProfile.asset`; чистая логика лежит в `Common/OS/Requirements/...`, а `OsReleaseService.cs` применяет штраф к качеству релиза и статусу OS. |
| Изменить время/новости | `GameTimeService.cs`, `EraNewsService.cs`, `EraNewsDatabase.cs`, соответствующие assets. |
| Изменить пользовательские отзывы | `UserReportsService.cs`, `UserReportsNodeRuntime.cs`, `UserReportsHistoryView.cs`. |
| Изменить публикацию OS build | `Features/OS/Release/OsReleaseService.cs`, `Features/OS/Reputation/OsPlayerReputationService.cs`, `Common/OS/OsReleaseGrowthProfile.cs`, `Common/Data/OS/OsReleaseGrowthProfile.asset`, `Common/Data/OS/OsPlayerReputationProfile.asset`, `ModuleBuildSettings.cs`, `OsInfoPresenter.cs`, `OsDeployBuildButtonManager.cs`. |
| Изменить прогрессию installations/rating | `Common/Data/OS/OsReleaseGrowthProfile.asset`; формула лежит в `Common/OS/OsReleaseGrowthProfile.cs`, а бонусы аудитории приходят из `Common/OS/Reputation/OsReleaseAudienceModifiers.cs`. |
| Изменить репутацию/популярность игрока | `Common/Data/OS/OsPlayerReputationProfile.asset` для баланса, `Features/OS/Reputation/OsPlayerReputationService.cs` для runtime state. |
| Изменить конкурентов OS | `Common/Data/OS/OsCompetitorConfig.asset` для списка и баланса роста, `Features/OS/Competitors/OsCompetitorService.cs` для runtime state, `Common/OS/Competitors/OsCompetitorLeaderboardBuilder.cs` для сортировки рейтинга, `OsInfoPresenter.cs` для вывода игрока и конкурентов. |
| Изменить research tree | `Common/Data/Research/ResearchConfig.asset` для контента, `Common/Research/...` для моделей, `Features/Research/Runtime/ResearchService.cs` для прогресса, `Features/UserInterface/Research/...` для окна/дерева/tooltip. |
| Подключить unlock нод через исследования | `ResearchConfig.IsNodeUnlockedByCompletedResearch`, `ResearchService.IsCompleted`, затем место spawn/filter логики нод (`NodeFactory`, `NodeGraphContextMenuController`, `NodeCatalog.asset`). |
| Изменить grid/camera | `CanvasGridView.cs`, `InfiniteGridRenderer.cs`, `GraphPanoramaBackgroundRenderer.cs`, `GridCameraPanZoomController.cs`. |
| Изменить IDE UI | `IdeWindowCanvasGroupController.cs`, `IdeBottomTabsController.cs`, `IdeCodeTabsController.cs`, `IdeInputCodeEditor.cs`, prefabs/UI сцены. Для дочерних панелей, которые должны закрываться вместе с IDE, использовать `hideOnCloseGroups`. |
| Изменить блокировку gameplay/OS input под UI | `Features/Input/GameInputBlocker.cs`, `IdeGameplayInputBlocker.cs`, `CanvasGroupShiftTabToggle.cs`, `OsWindowController.blockInputWhenGroupsVisible`, `ResearchWindowController.toggleInputAction`. |
| Изменить OS окно/OS info UI | `Features/UserInterface/OS/OsWindowController.cs`, `OsInfoPresenter.cs`, `OsInfoPanelView.cs`, `Tabs/OsWindowTabsController.cs`, `Rows/OsInfoEntryView.cs`, `Pages/OsInfoPageBinding.cs`, UI сцены. |
| Изменить уведомления | `Features/UserInterface/Notifications/NotificationView.cs`, `NotificationAnimator.cs`, `NotificationButtonBinder.cs`; визуальный prefab/scene object собирается вручную и назначается в инспекторе. |
| Изменить программное обновление UI-текста | `Features/UserInterface/RuntimeTextRefresh.cs`; новые UI-скрипты должны использовать helper вместо прямого `.text = ...`, если текст меняется из кода. |
| Изменить shader/material visuals | `Assets/_MyProject/Art/_Shaders` и `Assets/_MyProject/Art/Materials`; шейдеры optional и не должны быть обязательными для логики. |
