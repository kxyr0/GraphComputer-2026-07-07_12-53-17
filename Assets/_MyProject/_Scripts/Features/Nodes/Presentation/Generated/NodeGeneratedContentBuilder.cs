using Game.Nodes.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	internal sealed class NodeGeneratedContentBuilder
	{
		private readonly NodeView owner;
		private readonly RectTransform statsRoot;
		private readonly NodeStatRowView statRowPrefab;
		private readonly RectTransform inputPortsRoot;
		private readonly RectTransform outputPortsRoot;
		private readonly GeneratedNodePortView inputPortPrefab;
		private readonly GeneratedNodePortView outputPortPrefab;
		private readonly NodeGeneratedPortRegistry registry;
		private readonly bool isAutoCompactLayout;
		private readonly float generatedRowHeight;

		public NodeGeneratedContentBuilder(
			NodeView owner,
			RectTransform statsRoot,
			NodeStatRowView statRowPrefab,
			RectTransform inputPortsRoot,
			RectTransform outputPortsRoot,
			GeneratedNodePortView inputPortPrefab,
			GeneratedNodePortView outputPortPrefab,
			NodeGeneratedPortRegistry registry,
			bool isAutoCompactLayout,
			float generatedRowHeight)
		{
			this.owner = owner;
			this.statsRoot = statsRoot;
			this.statRowPrefab = statRowPrefab;
			this.inputPortsRoot = inputPortsRoot;
			this.outputPortsRoot = outputPortsRoot;
			this.inputPortPrefab = inputPortPrefab;
			this.outputPortPrefab = outputPortPrefab;
			this.registry = registry;
			this.isAutoCompactLayout = isAutoCompactLayout;
			this.generatedRowHeight = generatedRowHeight;
		}

		public void BuildStats(NodeJsonDefinition definition)
		{
			if (statsRoot == null || statRowPrefab == null)
			{
				Debug.LogWarning(
					$"[{nameof(NodeView)}] Не назначен корень статистик или prefab строки статистики.",
					owner);

				return;
			}

			if (definition?.stats == null)
			{
				return;
			}

			foreach (NodeJsonStat stat in definition.stats)
			{
				if (stat == null)
				{
					continue;
				}

				NodeStatRowView row = CreateGenerated(statRowPrefab, statsRoot);

				if (row == null)
				{
					continue;
				}

				row.Bind(stat.label, stat.value);
				registry.RegisterGeneratedStat(row);
			}
		}

		public void BuildPorts(NodeJsonDefinition definition)
		{
			if (definition?.ports == null)
			{
				return;
			}

			foreach (NodeJsonPort port in definition.ports)
			{
				if (port == null)
				{
					continue;
				}

				bool isInput = IsInputPort(port);

				RectTransform root = isInput ? inputPortsRoot : outputPortsRoot;
				GeneratedNodePortView prefab = isInput ? inputPortPrefab : outputPortPrefab;

				if (root == null || prefab == null)
				{
					Debug.LogWarning(
						$"[{nameof(NodeView)}] Не назначен корень или prefab для порта '{port.label}'.",
						owner);

					continue;
				}

				GeneratedNodePortView view = CreateGenerated(prefab, root);

				if (view == null)
				{
					continue;
				}

				view.Initialize(owner);
				view.Bind(port);
				registry.RegisterGeneratedPort(view);
			}
		}

		public void ClearGeneratedElements()
		{
			registry.Clear();

			ClearGeneratedChildren(statsRoot);
			ClearGeneratedChildren(inputPortsRoot);
			ClearGeneratedChildren(outputPortsRoot);
		}

		public void ClearGeneratedChildren(RectTransform root)
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

		public T CreateGenerated<T>(T prefab, RectTransform parent) where T : Component
		{
			if (prefab == null)
			{
				Debug.LogError(
					$"[{nameof(NodeView)}] Prefab генерируемого элемента не назначен.",
					owner);

				return null;
			}

			if (parent == null)
			{
				Debug.LogError(
					$"[{nameof(NodeView)}] Не назначен родитель для генерируемого prefab '{prefab.name}'.",
					owner);

				return null;
			}

			T instance = Object.Instantiate(prefab);

			Transform instanceTransform = instance.transform;
			instanceTransform.SetParent(parent, false);

			if (instanceTransform is RectTransform rect && isAutoCompactLayout)
			{
				rect.localScale = Vector3.one;
				rect.localRotation = Quaternion.identity;
				rect.anchoredPosition3D = Vector3.zero;

				ForceGeneratedElementHeight(rect);
			}

			instance.name = prefab.name.Replace("(Clone)", string.Empty);
			instance.gameObject.SetActive(true);

			if (instance.GetComponent<NodeGeneratedElement>() == null)
			{
				instance.gameObject.AddComponent<NodeGeneratedElement>();
			}

			return instance;
		}

		public void ForceGeneratedElementHeight(RectTransform rect)
		{
			if (rect == null)
			{
				return;
			}

			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.pivot = new Vector2(0.5f, 1f);

			rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, generatedRowHeight);

			LayoutElement layoutElement = rect.GetComponent<LayoutElement>();

			if (layoutElement == null)
			{
				layoutElement = rect.gameObject.AddComponent<LayoutElement>();
			}

			layoutElement.ignoreLayout = false;
			layoutElement.minHeight = generatedRowHeight;
			layoutElement.preferredHeight = generatedRowHeight;
			layoutElement.flexibleHeight = 0f;
		}

		public void DestroySmart(Object target)
		{
			if (target == null)
			{
				return;
			}

			if (Application.isPlaying)
			{
				Object.Destroy(target);
			}
			else
			{
				Object.DestroyImmediate(target);
			}
		}

		private static bool IsInputPort(NodeJsonPort port)
		{
			return string.Equals(
				port.direction,
				"Input",
				System.StringComparison.OrdinalIgnoreCase);
		}
	}
}
