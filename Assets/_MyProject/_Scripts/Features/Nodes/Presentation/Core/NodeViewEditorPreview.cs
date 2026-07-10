#if UNITY_EDITOR
using System;
using Game.Nodes.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Nodes.Presentation
{
	internal sealed class NodeViewEditorPreview
	{
		private readonly NodeView owner;
		private readonly Func<Settings> getSettings;
		private readonly Func<int> getPreviewHash;
		private readonly Action<int> setPreviewHash;
		private readonly Action bindFromJsonAsset;

		public NodeViewEditorPreview(
			NodeView owner,
			Func<Settings> getSettings,
			Func<int> getPreviewHash,
			Action<int> setPreviewHash,
			Action bindFromJsonAsset)
		{
			this.owner = owner;
			this.getSettings = getSettings;
			this.getPreviewHash = getPreviewHash;
			this.setPreviewHash = setPreviewHash;
			this.bindFromJsonAsset = bindFromJsonAsset;
		}

		public void OnValidate()
		{
			if (!CanPreviewJsonInEditor())
			{
				return;
			}

			Settings settings = GetSettings();
			int previewHash = CalculateEditorPreviewHash(settings);

			if (getPreviewHash() == previewHash && HasGeneratedElements(settings))
			{
				return;
			}

			EditorApplication.delayCall -= DelayedEditorPreview;
			EditorApplication.delayCall += DelayedEditorPreview;
		}

		public void StoreCurrentHash()
		{
			setPreviewHash(CalculateEditorPreviewHash());
		}

		public bool CanPreviewJsonInEditor()
		{
			return CanPreviewJsonInEditor(GetSettings());
		}

		public int CalculateEditorPreviewHash()
		{
			return CalculateEditorPreviewHash(GetSettings());
		}

		private void DelayedEditorPreview()
		{
			if (owner == null || !CanPreviewJsonInEditor())
			{
				return;
			}

			Settings settings = GetSettings();
			int previewHash = CalculateEditorPreviewHash(settings);

			if (getPreviewHash() == previewHash && HasGeneratedElements(settings))
			{
				return;
			}

			bindFromJsonAsset?.Invoke();
			setPreviewHash(previewHash);
			EditorUtility.SetDirty(owner);
		}

		private bool CanPreviewJsonInEditor(Settings settings)
		{
			return settings.PreviewJsonInEditor
				&& !Application.isPlaying
				&& settings.JsonDefinition != null
				&& owner != null
				&& !EditorUtility.IsPersistent(owner.gameObject)
				&& !PrefabUtility.IsPartOfPrefabAsset(owner.gameObject)
				&& !EditorApplication.isCompiling
				&& !EditorApplication.isUpdating;
		}

		private int CalculateEditorPreviewHash(Settings settings)
		{
			unchecked
			{
				int hash = StableStringHash(settings.JsonDefinition != null
					? settings.JsonDefinition.Json
					: string.Empty);

				hash = CombineHash(hash, settings.GeneratedContentLayoutMode);

				if (settings.IsAutoCompactLayout)
				{
					hash = CombineHash(hash, settings.AutoNodeSizeMode);
					hash = CombineHash(hash, settings.MinimumNodeSize);
					hash = CombineHash(hash, settings.HeaderHeight);
					hash = CombineHash(hash, settings.SafeTopPadding);
					hash = CombineHash(hash, settings.SafeBottomPadding);
					hash = CombineHash(hash, settings.SafeSidePadding);
					hash = CombineHash(hash, settings.ColumnGap);
					hash = CombineHash(hash, settings.StatsToPortsGap);
					hash = CombineHash(hash, settings.GeneratedRowHeight);
					hash = CombineHash(hash, settings.GeneratedRowSpacing);
				}
				else
				{
					hash = CombineHash(hash, settings.ManualApplyJsonSize ? 1 : 0);
					hash = CombineHash(hash, settings.ManualApplyHeaderLayout ? 1 : 0);
					hash = CombineHash(hash, settings.ManualForceLayoutRebuild ? 1 : 0);

					if (settings.ManualApplyHeaderLayout)
					{
						hash = CombineHash(hash, settings.HeaderHeight);
					}
				}

				return hash;
			}
		}

		private static bool HasGeneratedElements(Settings settings)
		{
			return HasGeneratedChildren(settings.StatsRoot)
				|| HasGeneratedChildren(settings.InputPortsRoot)
				|| HasGeneratedChildren(settings.OutputPortsRoot);
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

		private static int CombineHash(int hash, int value)
		{
			unchecked
			{
				return hash * 397 ^ value;
			}
		}

		private static int CombineHash(int hash, float value)
		{
			unchecked
			{
				return hash * 397 ^ value.GetHashCode();
			}
		}

		private static int CombineHash(int hash, Vector2 value)
		{
			unchecked
			{
				hash = hash * 397 ^ value.x.GetHashCode();
				hash = hash * 397 ^ value.y.GetHashCode();

				return hash;
			}
		}

		private Settings GetSettings()
		{
			return getSettings != null ? getSettings() : default;
		}

		internal readonly struct Settings
		{
			public readonly NodeJsonDefinitionAsset JsonDefinition;
			public readonly bool PreviewJsonInEditor;
			public readonly RectTransform StatsRoot;
			public readonly RectTransform InputPortsRoot;
			public readonly RectTransform OutputPortsRoot;
			public readonly int GeneratedContentLayoutMode;
			public readonly bool IsAutoCompactLayout;
			public readonly int AutoNodeSizeMode;
			public readonly bool ManualApplyJsonSize;
			public readonly bool ManualApplyHeaderLayout;
			public readonly bool ManualForceLayoutRebuild;
			public readonly Vector2 MinimumNodeSize;
			public readonly float HeaderHeight;
			public readonly float SafeTopPadding;
			public readonly float SafeBottomPadding;
			public readonly float SafeSidePadding;
			public readonly float ColumnGap;
			public readonly float StatsToPortsGap;
			public readonly float GeneratedRowHeight;
			public readonly float GeneratedRowSpacing;

			public Settings(
				NodeJsonDefinitionAsset jsonDefinition,
				bool previewJsonInEditor,
				RectTransform statsRoot,
				RectTransform inputPortsRoot,
				RectTransform outputPortsRoot,
				int generatedContentLayoutMode,
				bool isAutoCompactLayout,
				int autoNodeSizeMode,
				bool manualApplyJsonSize,
				bool manualApplyHeaderLayout,
				bool manualForceLayoutRebuild,
				Vector2 minimumNodeSize,
				float headerHeight,
				float safeTopPadding,
				float safeBottomPadding,
				float safeSidePadding,
				float columnGap,
				float statsToPortsGap,
				float generatedRowHeight,
				float generatedRowSpacing)
			{
				JsonDefinition = jsonDefinition;
				PreviewJsonInEditor = previewJsonInEditor;
				StatsRoot = statsRoot;
				InputPortsRoot = inputPortsRoot;
				OutputPortsRoot = outputPortsRoot;
				GeneratedContentLayoutMode = generatedContentLayoutMode;
				IsAutoCompactLayout = isAutoCompactLayout;
				AutoNodeSizeMode = autoNodeSizeMode;
				ManualApplyJsonSize = manualApplyJsonSize;
				ManualApplyHeaderLayout = manualApplyHeaderLayout;
				ManualForceLayoutRebuild = manualForceLayoutRebuild;
				MinimumNodeSize = minimumNodeSize;
				HeaderHeight = headerHeight;
				SafeTopPadding = safeTopPadding;
				SafeBottomPadding = safeBottomPadding;
				SafeSidePadding = safeSidePadding;
				ColumnGap = columnGap;
				StatsToPortsGap = statsToPortsGap;
				GeneratedRowHeight = generatedRowHeight;
				GeneratedRowSpacing = generatedRowSpacing;
			}
		}
	}
}
#endif
