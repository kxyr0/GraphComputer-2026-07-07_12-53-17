# Research Tabs

Research Tree is a separate HUD UI inside `HUDCanvas`. It is not connected to
`GraphWorld`, gameplay nodes, or node connections. White and red lines are UI
elements only.

## Data

Research balance lives in:

`Assets/_MyProject/_Scripts/Common/Data/Research/ResearchConfig.asset`

Each research item contains:

- `id`: stable identifier used by prerequisites and mutual exclusions.
- `tabId`: research tab id. It must match `ResearchTabsController` tab id and
  `ResearchTreePresenter` tab HUD root id.
- `title` and `description`: UI text.
- `costDays`: research price in in-game days.
- `gridPosition`: invisible UI grid position. `x = 0`, `y = 0` is the center of
  the assigned `Buttons Root`.
- `prerequisiteIds`: white dependency lines. The research is locked until these
  ids are completed.
- `mutualExclusionIds`: red choice lines. When one research is completed, the
  mutually exclusive alternatives become `Excluded`.

## Runtime

Put `ResearchService` on a scene object and assign explicitly:

- `Config`: `ResearchConfig.asset`.
- `Time Service`: scene `GameTimeService`.

There is no runtime component autosearch in the research service. If
`Time Service` is missing, the service logs a warning and uses absolute day `0`.

When research starts, the service stores:

`completeAbsoluteDay = currentAbsoluteDay + costDays`

On each `GameTimeService.TimeChanged`, completed researches move to `Completed`.
Mutually exclusive researches become `Excluded`.

## Scene Setup

On `HUDCanvas > Research Tree`, add `ResearchTreePresenter` and assign:

1. `Config`: `ResearchConfig.asset`.
2. `Research Service`: scene `ResearchService`.
3. `Tab Hud Roots`: one element per research tab.
4. `Research Button Template`: fallback `ResearchButtonView` template, or set it
   per tab.
5. `Link Line Template`: `ResearchLinkLineView` template.
6. `Tooltip View`: scene `ResearchTooltipView`, if tooltips are enabled.

For each `Tab Hud Roots` element:

- `Tab Id`: exact id, for example `system_core`, `kernel`, `security`, `data`.
- `Scroll Rect`: the tab `Scroll View` component, if this tab is scrollable.
- `Scroll Content Root`: `Scroll View > Viewport > Content`.
- `Buttons Root`: the concrete container for generated research buttons in this
  tab. For scrolling tabs, keep it inside `Scroll Content Root`.
- `Link Lines Root`: the concrete container for generated UI lines in this tab.
  For scrolling tabs, keep it inside `Scroll Content Root` too. Put this object
  behind `Buttons Root` in the hierarchy manually. The presenter never changes
  sibling order.
- `Research Button Template`: optional per-tab override.

The same tab id must be used in three places:

- `ResearchConfig.asset` -> research item `tabId`.
- `ResearchTabsController` -> `ResearchTabItem.id`.
- `ResearchTreePresenter` -> `ResearchTabHudRoot.Tab Id`.

Do not use ids like `system_core_tab` in one place and `system_core` in another.
That creates an empty tree, and the presenter will log an error or warning.

## Scroll View

`ResearchTreePresenter` does not create or move Scroll View objects. It only
uses the explicit references from `Tab Hud Roots`.

For a scrollable tab, use this hierarchy:

- `Scroll View`
- `Scroll View > Viewport`
- `Scroll View > Viewport > Content`
- `Scroll View > Viewport > Content > Lines`
- `Scroll View > Viewport > Content > ResearchButtons`

Assign:

- `Scroll Rect`: `Scroll View`.
- `Scroll Content Root`: `Content`.
- `Buttons Root`: `ResearchButtons`.
- `Link Lines Root`: `Lines`.

At rebuild time, `ResearchScrollContentLayout` calculates the required content
size from all research `gridPosition` values, sets `ScrollRect.content`, resizes
`Content`, and stretches `Buttons Root` / `Link Lines Root` if they are children
of `Content`.

If `Buttons Root` or `Link Lines Root` is outside `Content`, the presenter logs a
warning because that root will not move with the scroll content.

`x = 0`, `y = 0` remains the center of the scroll content. Larger positive or
negative grid coordinates expand the scrollable area symmetrically.

## Button Template

The button template must already have `ResearchButtonView` assigned manually.
The presenter does not add it and does not search child components.

Minimum scene object structure:

- `ResearchButton`
- `Background`
- `Title Research`

On `ResearchButtonView`, assign manually:

- `Button` or `Button Manager`.
- `Title Text`.
- `Background Graphic`.
- `Tooltip Binder`, if tooltips are enabled.

Optional fields such as `Cost Text`, `Status Text`, `Frame Graphic`, and
`Icon Graphic` can be left empty.

## Tooltip

`ResearchTooltipView` follows the same idea as `NotificationView`, but without
buttons and without status circle.

Assign manually:

- `Tooltip Group`: tooltip `CanvasGroup`.
- `Content Root`: tooltip `RectTransform`.
- `Title Text`.
- `Body Text`.
- `Research Time Text`: separate TMP text for `Research Time: X days`.
- `Cursor Offset`: screen-space offset from the cursor.
- `Clamp Root`: optional root rect, usually the HUD canvas/root panel, used to
  keep the tooltip on screen.
- `Animator`.

`ResearchTooltipBinder` must be assigned on the button template through
`ResearchButtonView.Tooltip Binder`. On hover and pointer move it calls:

`ResearchTooltipView.Show(definition, state, cursorScreenPosition, eventCamera)`

## Diagnostics

`ResearchTreePresenter` no longer creates fallback roots, searches components,
or creates a missing service. If something is not assigned, it logs a concrete
`Debug.LogWarning` or `Debug.LogError`.

Common empty-tree causes:

- `Tab Hud Roots` has `system_core_tab`, but selected tab/config uses
  `system_core`.
- `Buttons Root` is not assigned for the selected tab.
- `Research Button Template` is not a `ResearchButtonView`.
- `ResearchConfig.asset` has no researches with the selected `tabId`.
- `ResearchTabsController.Tree Presenter` is not assigned while `Sync Tree Tab`
  is enabled.
- Scroll View is assigned, but `Scroll Content Root` is empty or roots are
  outside `Content`.
