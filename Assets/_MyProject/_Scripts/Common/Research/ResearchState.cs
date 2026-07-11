namespace Game.Research
{
	public readonly struct ResearchState
	{
		public string Id { get; }
		public ResearchStatus Status { get; }
		public int StartAbsoluteDay { get; }
		public int CompleteAbsoluteDay { get; }
		public int RemainingDays { get; }

		public bool IsLocked => Status == ResearchStatus.Locked;
		public bool IsAvailable => Status == ResearchStatus.Available;
		public bool IsInProgress => Status == ResearchStatus.InProgress;
		public bool IsCompleted => Status == ResearchStatus.Completed;
		public bool IsExcluded => Status == ResearchStatus.Excluded;

		public ResearchState(
			string id,
			ResearchStatus status,
			int startAbsoluteDay,
			int completeAbsoluteDay,
			int remainingDays)
		{
			Id = id ?? string.Empty;
			Status = status;
			StartAbsoluteDay = startAbsoluteDay;
			CompleteAbsoluteDay = completeAbsoluteDay;
			RemainingDays = remainingDays;
		}

		public static ResearchState CreateInactive(string id, ResearchStatus status)
		{
			return new ResearchState(id, status, -1, -1, 0);
		}
	}
}
