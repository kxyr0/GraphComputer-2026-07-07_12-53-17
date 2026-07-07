using System.Collections.Generic;
using Game.Nodes.Definitions;
using Game.Nodes.Runtime;
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
		[Header("Json Source")]
		[SerializeField] private NodeJsonDefinitionAsset jsonDefinition;
		[SerializeField] private bool bindJsonOnEnable = false;
		[SerializeField] private bool previewJsonInEditor = true;

#if UNITY_EDITOR
		[SerializeField, HideInInspector] private int editorPreviewHash;
#endif

		[Header("Root")]
		[SerializeField] private RectTransform rectTransform;

		[Header("Visual")]
		[SerializeField] private Image backgroundImage;
		[SerializeField] private Image headerImage;
		[SerializeField] private Image accentImage;

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

		[Header("Drag")]
		[SerializeField] private NodeDragController dragController;
		private readonly Dictionary<string, NodeStatRowView> generatedStatsByLabel =
			new Dictionary<string, NodeStatRowView>(System.StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, GeneratedNodePortView> generatedPortsById =
			new Dictionary<string, GeneratedNodePortView>(System.StringComparer.OrdinalIgnoreCase);

		private readonly List<GeneratedNodePortView> generatedPorts = new();
		public IReadOnlyList<GeneratedNodePortView> GeneratedPorts => generatedPorts;
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
			if (!CanPreviewJsonInEditor())
			{
				return;
			}

			int previewHash = CalculateEditorPreviewHash();

			if (editorPreviewHash == previewHash && HasGeneratedElements())
			{
				return;
			}

			EditorApplication.delayCall -= DelayedEditorPreview;
			EditorApplication.delayCall += DelayedEditorPreview;
		}

		private void DelayedEditorPreview()
		{
			if (this == null || !CanPreviewJsonInEditor())
			{
				return;
			}

			int previewHash = CalculateEditorPreviewHash();

			if (editorPreviewHash == previewHash && HasGeneratedElements())
			{
				return;
			}

			BindFromJsonAsset();
			editorPreviewHash = previewHash;
			EditorUtility.SetDirty(this);
		}

		private bool CanPreviewJsonInEditor()
		{
			return previewJsonInEditor
				&& !Application.isPlaying
				&& jsonDefinition != null
				&& !EditorUtility.IsPersistent(gameObject)
				&& !PrefabUtility.IsPartOfPrefabAsset(gameObject)
				&& !EditorApplication.isCompiling
				&& !EditorApplication.isUpdating;
		}

		private int CalculateEditorPreviewHash()
		{
			return StableStringHash(jsonDefinition != null ? jsonDefinition.Json : string.Empty);
		}

		private bool HasGeneratedElements()
		{
			return HasGeneratedChildren(statsRoot)
				|| HasGeneratedChildren(inputPortsRoot)
				|| HasGeneratedChildren(outputPortsRoot);
		}

		private static bool HasGeneratedChildren(RectTransform root)
		{
			if (root == null)
			{
				return false;
			}

			for (int i = 0; i < root.childCount; i++)
			{
				if (root.GetChild(i).GetComponent<NodeGeneratedElement>() != null)
				{
					return true;
				}
			}

			return false;
		}

		private static int StableStringHash(string value)
		{
			unchecked
			{
				const int fnvOffsetBasis = -2128831035;
				const int fnvPrime = 16777619;

				int hash = fnvOffsetBasis;

				if (string.IsNullOrEmpty(value))
				{
					return hash;
				}

				for (int i = 0; i < value.Length; i++)
				{
					hash ^= value[i];
					hash *= fnvPrime;
				}

				return hash;
			}
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
					$"[{nameof(NodeView)}] Skipped JSON rebuild on prefab asset. Open it in Prefab Mode or enter Play Mode before rebuilding generated children.",
					this);

				return;
			}
#endif

			if (jsonDefinition == null)
			{
				Debug.LogWarning($"[{nameof(NodeView)}] Json definition is missing.", this);
				return;
			}

			if (!jsonDefinition.TryRead(out NodeJsonDefinition definition, out string error))
			{
				Debug.LogError($"[{nameof(NodeView)}] Failed to read JSON: {error}", this);
				return;
			}

			BindPreview(definition);

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				editorPreviewHash = CalculateEditorPreviewHash();
			}
#endif
		}

		private void BindPreview(NodeJsonDefinition jsonDefinition)
		{
			InstanceId = jsonDefinition.id;
			Definition = null;
			JsonDefinition = jsonDefinition;

			name = $"Node_{jsonDefinition.id}_Preview";

			ApplyJsonDefinition(jsonDefinition, jsonDefinition.canMove);
		}

		public void Bind(string instanceId, NodeDefinition definition, NodeJsonDefinition jsonDefinition)
		{
			if (definition == null)
			{
				throw new System.ArgumentNullException(nameof(definition));
			}

			if (jsonDefinition == null)
			{
				throw new System.ArgumentNullException(nameof(jsonDefinition));
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
				throw new System.ArgumentNullException(nameof(definition));
			}

			InstanceId = instanceId;
			Definition = definition;
			JsonDefinition = null;

			name = $"Node_{definition.Id}_{instanceId}";

			RectTransform.sizeDelta = definition.Size;

			SetTitle(definition.Title);
			SetVisual(
				definition.BackgroundColor,
				definition.HeaderColor,
				definition.AccentColor
			);

			SetCanDrag(definition.CanMove);

			ClearGeneratedElements();
			BindLegacyPorts(definition);
			InitializeBehaviours();
		}

		public bool TryGetPort(string portId, out GeneratedNodePortView portView)
		{
			if (string.IsNullOrWhiteSpace(portId))
			{
				portView = null;
				return false;
			}

			return generatedPortsById.TryGetValue(portId.Trim(), out portView);
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
			foreach (GeneratedNodePortView generatedPort in generatedPorts)
			{
				if (generatedPort != null && generatedPort.IsInput)
				{
					portView = generatedPort;
					return true;
				}
			}

			portView = null;
			return false;
		}

		public bool TryGetFirstOutputPort(out GeneratedNodePortView portView)
		{
			foreach (GeneratedNodePortView generatedPort in generatedPorts)
			{
				if (generatedPort != null && generatedPort.IsOutput)
				{
					portView = generatedPort;
					return true;
				}
			}

			portView = null;
			return false;
		}

		private void ApplyJsonDefinition(NodeJsonDefinition jsonDefinition, bool canDrag)
		{
			RectTransform.sizeDelta = jsonDefinition.size;

			SetTitle(jsonDefinition.title);

			SetVisual(
				ParseColor(jsonDefinition.backgroundColor, backgroundImage != null ? backgroundImage.color : Color.white),
				ParseColor(jsonDefinition.headerColor, headerImage != null ? headerImage.color : Color.white),
				ParseColor(jsonDefinition.accentColor, accentImage != null ? accentImage.color : Color.cyan)
			);

			SetCanDrag(canDrag);

			ClearGeneratedElements();
			BuildStats(jsonDefinition);
			BuildPorts(jsonDefinition);

			HideLegacyPorts();
			InitializeBehaviours();
		}

		private void SetTitle(string title)
		{
			if (titleText != null)
			{
				titleText.text = title;
			}
		}

		private void SetVisual(Color background, Color header, Color accent)
		{
			if (backgroundImage != null)
			{
				backgroundImage.color = background;
			}

			if (headerImage != null)
			{
				headerImage.color = header;
			}

			if (accentImage != null)
			{
				accentImage.color = accent;
			}
		}

		private void SetCanDrag(bool canDrag)
		{
			if (dragController != null)
			{
				dragController.SetCanDrag(canDrag);
			}
		}

		private void BuildStats(NodeJsonDefinition definition)
		{
			if (statsRoot == null || statRowPrefab == null)
			{
				Debug.LogWarning($"[{nameof(NodeView)}] Stats root or stat row prefab is missing.", this);
				return;
			}

			if (definition.stats == null)
			{
				return;
			}

			foreach (NodeJsonStat stat in definition.stats)
			{
				NodeStatRowView row = CreateGenerated(statRowPrefab, statsRoot);

				if (row == null)
				{
					continue;
				}

				row.Bind(stat.label, stat.value);
				RegisterGeneratedStat(row);
			}
		}
		public bool TrySetStatValue(string label, string value)
		{
			if (string.IsNullOrWhiteSpace(label))
			{
				return false;
			}

			if (!generatedStatsByLabel.TryGetValue(label.Trim(), out NodeStatRowView row))
			{
				return false;
			}

			row.SetValue(value);
			return true;
		}

		private void RegisterGeneratedStat(NodeStatRowView row)
		{
			if (row == null || string.IsNullOrWhiteSpace(row.Label))
			{
				return;
			}

			generatedStatsByLabel[row.Label.Trim()] = row;
		}
		private void BuildPorts(NodeJsonDefinition definition)
		{
			if (definition.ports == null)
			{
				return;
			}

			foreach (NodeJsonPort port in definition.ports)
			{
				bool isInput = IsInputPort(port);

				RectTransform root = isInput ? inputPortsRoot : outputPortsRoot;
				GeneratedNodePortView prefab = isInput ? inputPortPrefab : outputPortPrefab;

				if (root == null || prefab == null)
				{
					Debug.LogWarning(
						$"[{nameof(NodeView)}] Missing root or prefab for port '{port.label}'.",
						this);

					continue;
				}

				GeneratedNodePortView view = CreateGenerated(prefab, root);

				if (view == null)
				{
					continue;
				}

				view.Initialize(this);
				view.Bind(port);
				RegisterGeneratedPort(view);
			}
		}

		private void RegisterGeneratedPort(GeneratedNodePortView portView)
		{
			if (portView == null || string.IsNullOrWhiteSpace(portView.PortId))
			{
				return;
			}

			generatedPorts.Add(portView);
			generatedPortsById[portView.PortId] = portView;
		}

		private static bool IsInputPort(NodeJsonPort port)
		{
			return string.Equals(
				port.direction,
				"Input",
				System.StringComparison.OrdinalIgnoreCase);
		}

		private void ClearGeneratedElements()
		{
			generatedPortsById.Clear();
			generatedPorts.Clear();
			generatedStatsByLabel.Clear();

			ClearGeneratedChildren(statsRoot);
			ClearGeneratedChildren(inputPortsRoot);
			ClearGeneratedChildren(outputPortsRoot);
		}

		private void ClearGeneratedChildren(RectTransform root)
		{
			if (root == null)
			{
				return;
			}

			for (int i = root.childCount - 1; i >= 0; i--)
			{
				Transform child = root.GetChild(i);

				if (child.GetComponent<NodeGeneratedElement>() == null)
				{
					continue;
				}

				DestroySmart(child.gameObject);
			}
		}

		private T CreateGenerated<T>(T prefab, RectTransform parent) where T : Component
		{
			if (prefab == null)
			{
				Debug.LogError($"[{nameof(NodeView)}] Generated prefab is missing.", this);
				return null;
			}

			if (parent == null)
			{
				Debug.LogError($"[{nameof(NodeView)}] Generated parent is missing for prefab '{prefab.name}'.", this);
				return null;
			}

			T instance = Instantiate(prefab);

			Transform instanceTransform = instance.transform;
			instanceTransform.SetParent(parent, false);

			if (instanceTransform is RectTransform rect)
			{
				rect.localScale = Vector3.one;
				rect.localRotation = Quaternion.identity;
				rect.anchoredPosition3D = Vector3.zero;
			}

			instance.name = prefab.name.Replace("(Clone)", string.Empty);
			instance.gameObject.SetActive(true);

			if (instance.GetComponent<NodeGeneratedElement>() == null)
			{
				instance.gameObject.AddComponent<NodeGeneratedElement>();
			}

			return instance;
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

		private void BindLegacyPorts(NodeDefinition definition)
		{
			if (portViews == null || definition.Ports == null)
			{
				return;
			}

			for (int i = 0; i < portViews.Length; i++)
			{
				if (portViews[i] == null)
				{
					continue;
				}

				if (i < definition.Ports.Count)
				{
					portViews[i].Bind(definition.Ports[i]);
				}
				else
				{
					portViews[i].Hide();
				}
			}
		}

		private void HideLegacyPorts()
		{
			if (portViews == null)
			{
				return;
			}

			foreach (NodePortView portView in portViews)
			{
				if (portView != null)
				{
					portView.Hide();
				}
			}
		}

		private void InitializeBehaviours()
		{
			NodeRuntimeBehaviour[] behaviours = GetComponents<NodeRuntimeBehaviour>();

			foreach (NodeRuntimeBehaviour behaviour in behaviours)
			{
				if (behaviour != null)
				{
					behaviour.Initialize(this);
				}
			}
		}

		private static Color ParseColor(string value, Color fallback)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return fallback;
			}

			return ColorUtility.TryParseHtmlString(value, out Color color)
				? color
				: fallback;
		}
	}
}
