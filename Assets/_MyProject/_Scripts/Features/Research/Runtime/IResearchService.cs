using System;

namespace Game.Research.Runtime
{
	public interface IResearchService
	{
		event Action Changed;

		ResearchConfig Config { get; }
		string ActiveResearchId { get; }
		int CurrentAbsoluteDay { get; }

		bool IsCompleted(string researchId);
		bool TryStartResearch(string researchId);
		ResearchStatus GetStatus(string researchId);
		ResearchState GetState(string researchId);
		int GetRemainingDays(string researchId);
	}
}
