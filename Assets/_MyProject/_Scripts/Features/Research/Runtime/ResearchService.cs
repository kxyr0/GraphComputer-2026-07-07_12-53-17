using System;
using System.Collections.Generic;
using Game.World.Time;
using UnityEngine;

namespace Game.Research.Runtime
{
	[DisallowMultipleComponent]
	public sealed class ResearchService : MonoBehaviour, IResearchService
	{
		[SerializeField] private ResearchConfig config;
		[SerializeField] private GameTimeService timeService;
		[SerializeField] private bool allowParallelResearches;
		[SerializeField] private bool logResearchStart = true;

		[Header("Debug")]
		[SerializeField] private bool debugCompleteOnStart;

		private readonly HashSet<string> completedResearchIds = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, ResearchProgress> inProgressResearches =
			new(StringComparer.OrdinalIgnoreCase);
		private bool loggedMissingTimeService;

		public event Action Changed;

		public ResearchConfig Config => config;
		public string ActiveResearchId => GetFirstInProgressId();
		public int CurrentAbsoluteDay => timeService != null ? timeService.GetAbsoluteDay() : 0;
		public bool DebugCompleteOnStart
		{
			get => debugCompleteOnStart;
			set => debugCompleteOnStart = value;
		}

		private void Awake()
		{
			WarnIfTimeServiceMissing();
			RefreshCompletedResearches();
		}

		private void OnEnable()
		{
			WarnIfTimeServiceMissing();

			if (timeService != null)
			{
				timeService.TimeChanged += HandleTimeChanged;
			}
		}

		private void OnDisable()
		{
			if (timeService != null)
			{
				timeService.TimeChanged -= HandleTimeChanged;
			}
		}

		public void SetConfig(ResearchConfig value)
		{
			if (config == value)
			{
				return;
			}

			config = value;
			RemoveUnknownProgress();
			Changed?.Invoke();
		}

		public bool TryStartResearch(string researchId)
		{
			if (config == null || !config.TryGetDefinition(researchId, out ResearchDefinition definition))
			{
				Debug.LogWarning(
					$"[{nameof(ResearchService)}] Cannot start research '{researchId}': config is missing or id is unknown.",
					this);
				return false;
			}

			ResearchStatus status = GetStatus(definition.Id);

			if (status != ResearchStatus.Available)
			{
				Debug.LogWarning(
					$"[{nameof(ResearchService)}] Cannot start research '{definition.Id}': status is {status}.",
					this);
				return false;
			}

			if (!allowParallelResearches && inProgressResearches.Count > 0)
			{
				Debug.LogWarning(
					$"[{nameof(ResearchService)}] Cannot start research '{definition.Id}': another research is in progress.",
					this);
				return false;
			}

			if (HasInProgressMutualExclusion(definition))
			{
				Debug.LogWarning(
					$"[{nameof(ResearchService)}] Cannot start research '{definition.Id}': a mutually exclusive research is in progress.",
					this);
				return false;
			}

			int startDay = CurrentAbsoluteDay;
			int completeDay = startDay + definition.CostDays;

			if (debugCompleteOnStart)
			{
				CompleteResearch(definition.Id);

				if (logResearchStart)
				{
					Debug.Log(
						$"[{nameof(ResearchService)}] Debug completed research '{definition.Id}' immediately.",
						this);
				}

				return true;
			}

			inProgressResearches[definition.Id] = new ResearchProgress(startDay, completeDay);

			if (logResearchStart)
			{
				Debug.Log(
					$"[{nameof(ResearchService)}] Started research '{definition.Id}' for {definition.CostDays} day(s).",
					this);
			}

			Changed?.Invoke();
			RefreshCompletedResearches();

			return true;
		}

		public bool IsCompleted(string researchId)
		{
			return !string.IsNullOrWhiteSpace(researchId) && completedResearchIds.Contains(researchId);
		}

		public ResearchStatus GetStatus(string researchId)
		{
			if (config == null || !config.TryGetDefinition(researchId, out ResearchDefinition definition))
			{
				return ResearchStatus.Locked;
			}

			if (completedResearchIds.Contains(definition.Id))
			{
				return ResearchStatus.Completed;
			}

			if (HasCompletedMutualExclusion(definition))
			{
				return ResearchStatus.Excluded;
			}

			if (inProgressResearches.ContainsKey(definition.Id))
			{
				return ResearchStatus.InProgress;
			}

			return HasCompletedPrerequisites(definition)
				? ResearchStatus.Available
				: ResearchStatus.Locked;
		}

		public ResearchState GetState(string researchId)
		{
			ResearchStatus status = GetStatus(researchId);

			if (status == ResearchStatus.InProgress &&
				inProgressResearches.TryGetValue(researchId, out ResearchProgress progress))
			{
				return new ResearchState(
					researchId,
					status,
					progress.StartAbsoluteDay,
					progress.CompleteAbsoluteDay,
					Mathf.Max(0, progress.CompleteAbsoluteDay - CurrentAbsoluteDay)
				);
			}

			return ResearchState.CreateInactive(researchId, status);
		}

		public int GetRemainingDays(string researchId)
		{
			return GetState(researchId).RemainingDays;
		}

		public bool DebugCompleteResearch(string researchId, bool includeLocked)
		{
			if (config == null || !config.TryGetDefinition(researchId, out ResearchDefinition definition))
			{
				return false;
			}

			ResearchStatus status = GetStatus(definition.Id);

			if (!includeLocked &&
				status != ResearchStatus.Available &&
				status != ResearchStatus.InProgress)
			{
				return false;
			}

			return CompleteResearch(definition.Id);
		}

		public int DebugCompleteAvailableResearches()
		{
			if (config == null)
			{
				return 0;
			}

			int completedCount = 0;
			bool completedOnPass;

			do
			{
				completedOnPass = false;

				foreach (ResearchDefinition research in config.Researches)
				{
					if (research == null || GetStatus(research.Id) != ResearchStatus.Available)
					{
						continue;
					}

					if (CompleteResearch(research.Id))
					{
						completedCount++;
						completedOnPass = true;
					}
				}
			}
			while (completedOnPass);

			return completedCount;
		}

		public int DebugCompleteAllResearches()
		{
			if (config == null)
			{
				return 0;
			}

			int completedCount = 0;

			foreach (ResearchDefinition research in config.Researches)
			{
				if (research == null || string.IsNullOrWhiteSpace(research.Id))
				{
					continue;
				}

				bool wasCompleted = completedResearchIds.Contains(research.Id);

				inProgressResearches.Remove(research.Id);
				completedResearchIds.Add(research.Id);

				if (!wasCompleted)
				{
					completedCount++;
				}
			}

			Changed?.Invoke();
			return completedCount;
		}

		public void DebugResetResearches()
		{
			completedResearchIds.Clear();
			inProgressResearches.Clear();
			Changed?.Invoke();
		}

		private void HandleTimeChanged()
		{
			RefreshCompletedResearches();
		}

		private void RefreshCompletedResearches()
		{
			if (inProgressResearches.Count == 0)
			{
				return;
			}

			bool changed = false;
			int currentDay = CurrentAbsoluteDay;
			List<string> idsToFinish = new();

			foreach (KeyValuePair<string, ResearchProgress> pair in inProgressResearches)
			{
				if (!config.TryGetDefinition(pair.Key, out ResearchDefinition definition) ||
					HasCompletedMutualExclusion(definition))
				{
					idsToFinish.Add(pair.Key);
					changed = true;
					continue;
				}

				if (currentDay >= pair.Value.CompleteAbsoluteDay)
				{
					idsToFinish.Add(pair.Key);
					completedResearchIds.Add(pair.Key);
					changed = true;
				}
			}

			foreach (string researchId in idsToFinish)
			{
				inProgressResearches.Remove(researchId);
			}

			if (changed)
			{
				Changed?.Invoke();
			}
		}

		private bool CompleteResearch(string researchId)
		{
			if (string.IsNullOrWhiteSpace(researchId))
			{
				return false;
			}

			bool removedProgress = inProgressResearches.Remove(researchId);
			bool addedCompleted = completedResearchIds.Add(researchId);

			if (removedProgress || addedCompleted)
			{
				Changed?.Invoke();
				return true;
			}

			return false;
		}

		private bool HasCompletedPrerequisites(ResearchDefinition definition)
		{
			foreach (string prerequisiteId in definition.PrerequisiteIds)
			{
				if (string.IsNullOrWhiteSpace(prerequisiteId))
				{
					continue;
				}

				if (!completedResearchIds.Contains(prerequisiteId))
				{
					return false;
				}
			}

			return true;
		}

		private bool HasCompletedMutualExclusion(ResearchDefinition definition)
		{
			foreach (string completedId in completedResearchIds)
			{
				if (config.AreMutuallyExclusive(definition.Id, completedId))
				{
					return true;
				}
			}

			return false;
		}

		private bool HasInProgressMutualExclusion(ResearchDefinition definition)
		{
			foreach (string inProgressId in inProgressResearches.Keys)
			{
				if (config.AreMutuallyExclusive(definition.Id, inProgressId))
				{
					return true;
				}
			}

			return false;
		}

		private void RemoveUnknownProgress()
		{
			if (config == null)
			{
				inProgressResearches.Clear();
				completedResearchIds.Clear();
				return;
			}

			List<string> unknownIds = new();

			foreach (string researchId in inProgressResearches.Keys)
			{
				if (!config.TryGetDefinition(researchId, out _))
				{
					unknownIds.Add(researchId);
				}
			}

			foreach (string researchId in unknownIds)
			{
				inProgressResearches.Remove(researchId);
			}
		}

		private void WarnIfTimeServiceMissing()
		{
			if (timeService != null || loggedMissingTimeService)
			{
				return;
			}

			loggedMissingTimeService = true;
			Debug.LogWarning(
				$"[{nameof(ResearchService)}] Time Service is not assigned. Research days will use absolute day 0.",
				this);
		}

		private string GetFirstInProgressId()
		{
			foreach (string researchId in inProgressResearches.Keys)
			{
				return researchId;
			}

			return string.Empty;
		}

		private readonly struct ResearchProgress
		{
			public int StartAbsoluteDay { get; }
			public int CompleteAbsoluteDay { get; }

			public ResearchProgress(int startAbsoluteDay, int completeAbsoluteDay)
			{
				StartAbsoluteDay = startAbsoluteDay;
				CompleteAbsoluteDay = completeAbsoluteDay;
			}
		}
	}
}
