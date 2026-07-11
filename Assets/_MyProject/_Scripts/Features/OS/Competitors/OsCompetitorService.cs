using System.Collections.Generic;
using Game.OS.Competitors;
using UnityEngine;

namespace Game.OS.Competitors
{
	[DisallowMultipleComponent]
	public sealed class OsCompetitorService : MonoBehaviour
	{
		[SerializeField] private OsCompetitorConfig config;
		[SerializeField] private bool resetOnAwake = true;
		[SerializeField] private bool logGrowth;

		private readonly List<int> currentUsers = new();
		private float growthTimer;
		private bool initialized;

		public bool HasConfig => config != null;
		public int CompetitorCount => config != null ? config.CompetitorCount : 0;

		private void Awake()
		{
			if (resetOnAwake)
			{
				ResetCompetitors();
			}
		}

		private void Update()
		{
			if (config == null)
			{
				return;
			}

			EnsureInitialized();
			growthTimer += Time.unscaledDeltaTime;

			if (growthTimer < config.GrowthIntervalSeconds)
			{
				return;
			}

			growthTimer = 0f;
			AdvanceGrowthStep();
		}

		public OsCompetitorState[] CreateSnapshot()
		{
			if (config == null)
			{
				return System.Array.Empty<OsCompetitorState>();
			}

			EnsureInitialized();
			List<OsCompetitorState> snapshot = new();

			for (int i = 0; i < config.CompetitorCount; i++)
			{
				if (!config.TryGetCompetitor(i, out OsCompetitorDefinition competitor))
				{
					continue;
				}

				snapshot.Add(new OsCompetitorState(
					competitor.CompanyName,
					competitor.OperatingSystemName,
					competitor.StartingUsers,
					GetCurrentUsers(i)
				));
			}

			return snapshot.ToArray();
		}

		[ContextMenu("Reset Competitors")]
		public void ResetCompetitors()
		{
			currentUsers.Clear();

			if (config == null)
			{
				initialized = false;
				return;
			}

			for (int i = 0; i < config.CompetitorCount; i++)
			{
				currentUsers.Add(config.TryGetCompetitor(i, out OsCompetitorDefinition competitor)
					? competitor.StartingUsers
					: 0);
			}

			growthTimer = 0f;
			initialized = true;
		}

		private void EnsureInitialized()
		{
			if (!initialized || currentUsers.Count != CompetitorCount)
			{
				ResetCompetitors();
			}
		}

		private void AdvanceGrowthStep()
		{
			for (int i = 0; i < config.CompetitorCount; i++)
			{
				if (!config.TryGetCompetitor(i, out OsCompetitorDefinition competitor))
				{
					continue;
				}

				int previousUsers = GetCurrentUsers(i);
				int nextUsers = competitor.EvaluateNextUsers(previousUsers);
				SetCurrentUsers(i, nextUsers);

				if (logGrowth && nextUsers != previousUsers)
				{
					Debug.Log(
						$"[{nameof(OsCompetitorService)}] {competitor.CompanyName} {previousUsers} -> {nextUsers} users.",
						this
					);
				}
			}
		}

		private int GetCurrentUsers(int index)
		{
			return index >= 0 && index < currentUsers.Count
				? currentUsers[index]
				: 0;
		}

		private void SetCurrentUsers(int index, int users)
		{
			while (currentUsers.Count <= index)
			{
				currentUsers.Add(0);
			}

			currentUsers[index] = Mathf.Max(0, users);
		}
	}
}
