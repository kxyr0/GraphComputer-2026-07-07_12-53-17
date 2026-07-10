using System;
using System.Collections.Generic;
using System.Text;
using Game.IDE.Runtime;
using UnityEngine;

namespace Game.IDE.Presentation
{
	public sealed class NodeCodeRegistry : MonoBehaviour
	{
		public static NodeCodeRegistry Instance { get; private set; }

		private readonly Dictionary<string, NodeCodeState> statesByKey =
			new Dictionary<string, NodeCodeState>(StringComparer.OrdinalIgnoreCase);

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Debug.LogWarning(
					$"[{nameof(NodeCodeRegistry)}] Duplicate registry detected. Using newest instance: {name}",
					this);
			}

			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void SaveBuildResult(
			string documentId,
			string code,
			IdeBuildReport report)
		{
			if (report == null)
			{
				Debug.LogWarning($"[{nameof(NodeCodeRegistry)}] Cannot save null build report.", this);
				return;
			}

			NodeCodeState state = new NodeCodeState(documentId);

			state.Code = code ?? string.Empty;
			state.ModuleName = report.ModuleName ?? string.Empty;
			state.ModuleSettings = report.ModuleSettings;
			state.LastBuildSucceeded = report.IsSuccessful;
			state.WarningCount = report.Warnings.Count;
			state.ErrorCount = report.Errors.Count;

			state.StabilityDelta = report.StabilityDelta;
			state.SystemLoadDelta = report.SystemLoadDelta;
			state.TechnicalDebtDelta = report.TechnicalDebtDelta;

			RegisterAlias(documentId, state);
			RegisterAlias(report.ModuleName, state);

			// Очень важные aliases.
			// SystemCore -> system_core
			RegisterAlias(ToSnakeCase(report.ModuleName), state);

			Debug.Log(
				$"[{nameof(NodeCodeRegistry)}] Saved build. " +
				$"DocumentId='{documentId}', Module='{report.ModuleName}', " +
				$"Snake='{ToSnakeCase(report.ModuleName)}', Success={state.LastBuildSucceeded}",
				this);
		}

		public bool IsBuiltSuccessfully(string id)
		{
			if (TryGetState(id, out NodeCodeState state))
			{
				return state.LastBuildSucceeded;
			}

			return false;
		}

		public bool TryGetState(string id, out NodeCodeState state)
		{
			string key = NormalizeKey(id);

			if (string.IsNullOrWhiteSpace(key))
			{
				state = null;
				return false;
			}

			return statesByKey.TryGetValue(key, out state);
		}

		[ContextMenu("Print Build States")]
		private void PrintBuildStates()
		{
			if (statesByKey.Count == 0)
			{
				Debug.Log("[NodeCodeRegistry] Empty.", this);
				return;
			}

			foreach (KeyValuePair<string, NodeCodeState> pair in statesByKey)
			{
				Debug.Log(
					$"[NodeCodeRegistry] Key='{pair.Key}', " +
					$"Module='{pair.Value.ModuleName}', " +
					$"Success={pair.Value.LastBuildSucceeded}",
					this);
			}
		}

		private void RegisterAlias(string id, NodeCodeState state)
		{
			string key = NormalizeKey(id);

			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}

			statesByKey[key] = state;
		}

		private static string NormalizeKey(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			StringBuilder builder = new StringBuilder(value.Length);

			foreach (char character in value)
			{
				if (char.IsLetterOrDigit(character))
				{
					builder.Append(char.ToLowerInvariant(character));
				}
			}

			return builder.ToString();
		}

		private static string ToSnakeCase(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			StringBuilder builder = new StringBuilder();

			for (int i = 0; i < value.Length; i++)
			{
				char character = value[i];

				if (!char.IsLetterOrDigit(character))
				{
					continue;
				}

				if (i > 0 && char.IsUpper(character) && builder.Length > 0)
				{
					builder.Append('_');
				}

				builder.Append(char.ToLowerInvariant(character));
			}

			return builder.ToString();
		}
	}

	[Serializable]
	public sealed class NodeCodeState
	{
		public string DocumentId { get; }
		public string ModuleName { get; set; }
		public string Code { get; set; }
		public ModuleBuildSettings ModuleSettings { get; set; }

		public bool LastBuildSucceeded { get; set; }
		public int WarningCount { get; set; }
		public int ErrorCount { get; set; }

		public int StabilityDelta { get; set; }
		public int SystemLoadDelta { get; set; }
		public int TechnicalDebtDelta { get; set; }

		public NodeCodeState(string documentId)
		{
			DocumentId = documentId;
			Code = string.Empty;
			ModuleName = string.Empty;
			ModuleSettings = ModuleBuildSettings.Balanced();
		}
	}
}
