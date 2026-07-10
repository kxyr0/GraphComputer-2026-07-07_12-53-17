namespace Game.Nodes.Domain
{
	public enum NodeKind
	{
		// Старые значения НЕ трогаем, чтобы не сломать уже созданные ассеты.
		SystemCore = 0,
		Downloader = 1,
		Processor = 2,
		Storage = 3,
		Firewall = 4,
		Output = 5,

		// Main OS branches
		Data = 10,
		Kernel = 11,
		Security = 12,
		Control = 13,

		// Kernel branch
		MemoryManager = 20,
		ProcessManager = 21,
		Scheduler = 22,
		Recovery = 23,

		// Data branch
		FileSystem = 30,
		Backup = 31,
		Compression = 32,
		DataRecovery = 33,

		// Security branch
		Permissions = 40,
		Antivirus = 41,
		Encryption = 42,

		// Control branch
		DisplayManager = 50,
		Sound = 51,
		Devices = 52,
		Power = 53,
		Time = 60,
		EraNews = 61,
		UserReports = 62
	}

	public enum NodePortDirection
	{
		Input,
		Output
	}

	public enum NodeSignalType
	{
		Power,
		Data,
		Sound,
		Compute,
		Network,
		Security,
		Storage,
		Control
	}
}
