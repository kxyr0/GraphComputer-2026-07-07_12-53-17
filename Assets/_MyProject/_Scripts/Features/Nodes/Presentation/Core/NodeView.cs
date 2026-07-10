using System;
using System.Collections.Generic;
using Game.Nodes.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Nodes.Presentation
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public sealed class NodeView : MonoBehaviour
	{
		private enum GeneratedContentLayoutMode
		{
			ManualPrefab = 0,
			AutoCompact = 1
		}

		private enum AutoNodeSizeMode
		{
			JsonOnly = 0,
			AutoHeightWithJsonMinimum = 1
		}

		[Header("Json Source")]
		[SerializeField] private NodeJsonDefinitionAsset jsonDefinition;
		[SerializeField] private bool bindJsonOnEnable = false;
		[SerializeField] private bool previewJsonInEditor = true;

#if UNITY_EDITOR
		[SerializeField, HideInInspector] private int editorPreviewHash;
		private NodeViewEditorPreview editorPreview;
#endif

		[Header("Root")]
		[SerializeField] private RectTransform rectTransform;

		[Header("Visual")]
		[SerializeField] private Image backgroundImage;
		[SerializeField] private Image headerImage;
		[SerializeField] private Image accentImage;

		[Header("Circle Status")]
		[SerializeField] private Image circleStatusImage;
		[SerializeField] private bool updateCircleStatusFromStats = true;
		[SerializeField] private Color circleHealthyColor = new Color32(57, 255, 32, 255);
		[SerializeField] private Color circleLimitColor = new Color32(255, 165, 0, 255);
		[SerializeField] private Color circleErrorColor = new Color32(255, 42, 42, 255);

		[Header("Text")]
		[SerializeField] private TMP_Text titleText;

		[Header("Generated Stats")]
		[SerializeField] private RectTransform statsRoot;
		[SerializeField] private NodeStatRowView statRowPrefab;

		[Header("Generated Ports")]
		[SerializeField] private RectTransform inputPortsRoot;
		[SerializeField] private RectTransform outputPortsRoot;
		[SerializeField] private GeneratedNodePortView inputPortPrefab;
		[SerializeField] private GeneratedNodePortView outputPortPrefab;

		[Header("Legacy Ports")]
		[SerializeField] private NodePortView[] portViews;

		[Header("Layout Mode")]
		[SerializeField]
		private GeneratedContentLayoutMode generatedContentLayoutMode =
			GeneratedContentLayoutMode.ManualPrefab;

		[SerializeField]
		private AutoNodeSizeMode autoNodeSizeMode =
			AutoNodeSizeMode.JsonOnly;

		[Header("Manual Prefab Safety")]
		[SerializeField] private bool manualApplyJsonSize = false;
		[SerializeField] private bool manualApplyHeaderLayout = false;
		[SerializeField] private bool manualForceLayoutRebuild = false;

		[Header("Auto Compact Settings")]
		[SerializeField] private Vector2 minimumNodeSize = new Vector2(520f, 260f);

		[SerializeField, Min(1f)] private float headerHeight = 36f;

		[SerializeField, Min(0f)] private float safeTopPadding = 10f;
		[SerializeField, Min(0f)] private float safeBottomPadding = 16f;
		[SerializeField, Min(0f)] private float safeSidePadding = 32f;

		[SerializeField, Min(0f)] private float columnGap = 28f;
		[SerializeField, Min(0f)] private float statsToPortsGap = 18f;

		[SerializeField, Min(1f)] private float generatedRowHeight = 24f;
		[SerializeField, Min(0f)] private float generatedRowSpacing = 2f;

		[Header("Drag")]
		[SerializeField] private NodeDragController dragController;

		private NodeGeneratedPortRegistry generatedPortRegistry;
		private readonly NodeCircleStatusEvaluator circleStatusEvaluator = new();

		public IReadOnlyList<GeneratedNodePortView> GeneratedPorts =>
			GeneratedPortRegistry.GeneratedPorts;

		public string InstanceId { get; private set; }

		public NodeDefinition Definition { get; private set; }

		public NodeJsonDefinition JsonDefinition { get; private set; }

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

		private bool IsAutoCompactLayout =>
			generatedContentLayoutMode == GeneratedContentLayoutMode.AutoCompact;

		private bool IsJsonOnlySizeMode =>
			autoNodeSizeMode == AutoNodeSizeMode.JsonOnly;

		private NodeGeneratedPortRegistry GeneratedPortRegistry =>
			generatedPortRegistry ??= new NodeGeneratedPortRegistry();

		private void Reset()
		{
			rectTransform = transform as RectTransform;
			dragController = GetComponent<NodeDragController>();
			portViews = GetComponentsInChildren<NodePortView>(true);
		}

		private void OnEnable()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				return;
			}
#endif

			if (bindJsonOnEnable && jsonDefinition != null)
			{
				BindFromJsonAsset();
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			EnsureEditorPreview().OnValidate();
		}
#endif

		[ContextMenu("Rebuild From Json")]
		public void BindFromJsonAsset()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying
				&& (EditorUtility.IsPersistent(gameObject)
					|| PrefabUtility.IsPartOfPrefabAsset(gameObject)))
			{
				Debug.LogWarning(
					$"[{nameof(NodeView)}] Перестроение из JSON пропущено для prefab asset. Откройте prefab в Prefab Mode или войдите в Play Mode перед перестроением генерируемых дочерних объектов.",
					this);

				return;
			}
#endif

			if (jsonDefinition == null)
			{
				Debug.LogWarning($"[{nameof(NodeView)}] JSON-описание не назначено.", this);
				return;
			}

			if (!jsonDefinition.TryRead(out NodeJsonDefinition definition, out string error))
			{
				Debug.LogError($"[{nameof(NodeView)}] Не удалось прочитать JSON: {error}", this);
				return;
			}

			BindPreview(definition);

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				EnsureEditorPreview().StoreCurrentHash();
			}
#endif
		}

		public void Bind(string instanceId, NodeDefinition definition, NodeJsonDefinition jsonDefinition)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			if (jsonDefinition == null)
			{
				throw new ArgumentNullException(nameof(jsonDefinition));
			}

			InstanceId = instanceId;
			Definition = definition;
			JsonDefinition = jsonDefinition;

			name = $"Node_{definition.Id}_{instanceId}";

			ApplyJsonDefinition(jsonDefinition, definition.CanMove && jsonDefinition.canMove);
		}

		public void Bind(string instanceId, NodeDefinition definition)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			InstanceId = instanceId;
			Definition = definition;
			JsonDefinition = null;

			name = $"Node_{definition.Id}_{instanceId}";

			NodeViewVisualPresenter presenter = CreateVisualPresenter();

			CreateLayoutController(presenter).SetNodeSize(definition.Size);
			presenter.SetTitle(definition.Title);
			presenter.SetVisual(
				definition.BackgroundColor,
				definition.HeaderColor,
				definition.AccentColor);
			presenter.SetCanDrag(definition.CanMove);

			CreateContentBuilder().ClearGeneratedElements();
			CreateLegacyPortBinder().BindLegacyPorts(definition);
			ResetCircleStatus();
			CreateRuntimeBehaviourInitializer().Initialize();
		}

		public bool TryGetPort(string portId, out GeneratedNodePortView portView)
		{
			return GeneratedPortRegistry.TryGetPort(portId, out portView);
		}

		public bool TryGetCompatibleInputPort(
			GeneratedNodePortView sourcePort,
			out GeneratedNodePortView inputPort)
		{
			return NodePortCompatibilityUtility.TryFindCompatibleInput(
				this,
				sourcePort,
				out inputPort);
		}

		public bool TryGetFirstInputPort(out GeneratedNodePortView portView)
		{
			return GeneratedPortRegistry.TryGetFirstInputPort(out portView);
		}

		public bool TryGetFirstOutputPort(out GeneratedNodePortView portView)
		{
			return GeneratedPortRegistry.TryGetFirstOutputPort(out portView);
		}

		public bool TrySetStatValue(string label, string value)
		{
			if (!GeneratedPortRegistry.TrySetStatValue(label, value))
			{
				return false;
			}

			RefreshCircleStatus(label, value, string.Empty);
			RefreshGeneratedLayoutAfterStatChange();
			return true;
		}

		public bool TrySetStatValue(string label, string value, string stateHint)
		{
			if (!GeneratedPortRegistry.TrySetStatValue(label, value, stateHint))
			{
				return false;
			}

			RefreshCircleStatus(label, value, stateHint);
			RefreshGeneratedLayoutAfterStatChange();
			return true;
		}

		public void SetCircleStatus(NodeCircleStatus status)
		{
			CreateCircleStatusPresenter().SetStatus(status);
		}

		private void BindPreview(NodeJsonDefinition jsonDefinition)
		{
			InstanceId = jsonDefinition.id;
			Definition = null;
			JsonDefinition = jsonDefinition;

			name = $"Node_{jsonDefinition.id}_Preview";

			ApplyJsonDefinition(jsonDefinition, jsonDefinition.canMove);
		}

		private void ApplyJsonDefinition(NodeJsonDefinition jsonDefinition, bool canDrag)
		{
			NodeViewVisualPresenter presenter = CreateVisualPresenter();

			presenter.SetTitle(jsonDefinition.title);
			presenter.SetVisual(
				NodeColorParser.ParseColor(
					jsonDefinition.backgroundColor,
					backgroundImage != null ? backgroundImage.color : Color.white),
				NodeColorParser.ParseColor(
					jsonDefinition.headerColor,
					headerImage != null ? headerImage.color : Color.white),
				NodeColorParser.ParseColor(
					jsonDefinition.accentColor,
					accentImage != null ? accentImage.color : Color.cyan));
			presenter.SetCanDrag(canDrag);

			NodeGeneratedContentBuilder contentBuilder = CreateContentBuilder();
			contentBuilder.ClearGeneratedElements();
			contentBuilder.BuildStats(jsonDefinition);
			contentBuilder.BuildPorts(jsonDefinition);

			CreateLayoutController(presenter).UpdateGeneratedLayoutAndSize(jsonDefinition);
			CreateLegacyPortBinder().HideLegacyPorts();
			ResetCircleStatus();
			CreateRuntimeBehaviourInitializer().Initialize();
		}

		private void RefreshGeneratedLayoutAfterStatChange()
		{
			NodeViewVisualPresenter presenter = CreateVisualPresenter();
			NodeGeneratedLayoutController layoutController = CreateLayoutController(presenter);

			if (JsonDefinition != null && IsAutoCompactLayout)
			{
				layoutController.UpdateGeneratedLayoutAndSize(JsonDefinition);
				return;
			}

			if (manualForceLayoutRebuild)
			{
				layoutController.RebuildGeneratedLayouts();
			}
		}

		private void ResetCircleStatus()
		{
			circleStatusEvaluator.Reset();
			CreateCircleStatusPresenter().SetStatus(circleStatusEvaluator.Current);
		}

		private void RefreshCircleStatus(string label, string value, string stateHint)
		{
			if (!updateCircleStatusFromStats)
			{
				return;
			}

			NodeCircleStatus status = circleStatusEvaluator.RecordStat(label, value, stateHint);
			CreateCircleStatusPresenter().SetStatus(status);
		}

		private NodeCircleStatusPresenter CreateCircleStatusPresenter()
		{
			return new NodeCircleStatusPresenter(
				circleStatusImage,
				circleHealthyColor,
				circleLimitColor,
				circleErrorColor);
		}

		private NodeViewVisualPresenter CreateVisualPresenter()
		{
			return new NodeViewVisualPresenter(
				backgroundImage,
				headerImage,
				accentImage,
				titleText,
				dragController,
				headerHeight);
		}

		private NodeGeneratedContentBuilder CreateContentBuilder()
		{
			return new NodeGeneratedContentBuilder(
				this,
				statsRoot,
				statRowPrefab,
				inputPortsRoot,
				outputPortsRoot,
				inputPortPrefab,
				outputPortPrefab,
				GeneratedPortRegistry,
				IsAutoCompactLayout,
				generatedRowHeight);
		}

		private NodeGeneratedLayoutController CreateLayoutController(
			NodeViewVisualPresenter presenter)
		{
			return new NodeGeneratedLayoutController(
				RectTransform,
				statsRoot,
				inputPortsRoot,
				outputPortsRoot,
				presenter,
				IsAutoCompactLayout,
				IsJsonOnlySizeMode,
				manualApplyJsonSize,
				manualApplyHeaderLayout,
				manualForceLayoutRebuild,
				minimumNodeSize,
				headerHeight,
				safeTopPadding,
				safeBottomPadding,
				safeSidePadding,
				columnGap,
				statsToPortsGap,
				generatedRowHeight,
				generatedRowSpacing);
		}

		private NodeLegacyPortBinder CreateLegacyPortBinder()
		{
			return new NodeLegacyPortBinder(portViews);
		}

		private NodeRuntimeBehaviourInitializer CreateRuntimeBehaviourInitializer()
		{
			return new NodeRuntimeBehaviourInitializer(this);
		}

#if UNITY_EDITOR
		private NodeViewEditorPreview EnsureEditorPreview()
		{
			editorPreview ??= new NodeViewEditorPreview(
				this,
				GetEditorPreviewSettings,
				() => editorPreviewHash,
				value => editorPreviewHash = value,
				BindFromJsonAsset);

			return editorPreview;
		}

		private NodeViewEditorPreview.Settings GetEditorPreviewSettings()
		{
			return new NodeViewEditorPreview.Settings(
				jsonDefinition,
				previewJsonInEditor,
				statsRoot,
				inputPortsRoot,
				outputPortsRoot,
				(int)generatedContentLayoutMode,
				IsAutoCompactLayout,
				(int)autoNodeSizeMode,
				manualApplyJsonSize,
				manualApplyHeaderLayout,
				manualForceLayoutRebuild,
				minimumNodeSize,
				headerHeight,
				safeTopPadding,
				safeBottomPadding,
				safeSidePadding,
				columnGap,
				statsToPortsGap,
				generatedRowHeight,
				generatedRowSpacing);
		}
#endif
	}
}
