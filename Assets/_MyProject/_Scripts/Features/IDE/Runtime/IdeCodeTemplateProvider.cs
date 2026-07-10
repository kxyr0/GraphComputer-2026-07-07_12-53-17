namespace Game.IDE.Runtime
{
	public static class IdeCodeTemplateProvider
	{
		public static string GetDefaultCode(
			string documentId,
			string title = null,
			ModuleOperatingMode mode = ModuleOperatingMode.Balanced)
		{
			string modeValue = ModuleOperatingModeUtility.ToCodeValue(mode);

			switch ((documentId ?? string.Empty).ToLowerInvariant())
			{
				case "system_core":
					return BuildModule("SystemCore", modeValue,
@"platform = PC

require Kernel as kernel
require Data as data
optional Security as security
optional Control as control

budget {
    cpu <= 72
    memory <= 68
    storage <= 80
}

boot {
    start kernel.scheduler
    mount data.fileSystem
    scan security.firewall
}

tick every 1s {
    monitor kernel.load
    cache data.index

    if kernel.load > 80
        throttle data.index by 25
        alert ""High Load""
    end
}

export {
    stability = 100 - kernel.load * 0.25
    load = kernel.load
}");

				case "kernel":
					return BuildModule("Kernel", modeValue,
@"require SystemCore as core

budget {
    cpu <= 48
    memory <= 64
}

scheduler {
    mode = round_robin
    timeSlice = 12ms
}

boot {
    start scheduler
    watch processes
}

tick every 1s {
    monitor processQueue

    if processes > 32
        throttle scheduler by 20
        alert ""Process Pressure""
    end
}

export {
    load = processQueue.pressure
    response = scheduler.latency
}");

				case "data":
					return BuildModule("Data", modeValue,
@"require SystemCore as core

budget {
    memory <= 48
    storage <= 80
}

boot {
    mount fileSystem
    index directories
    cache fileTable
}

tick every 5s {
    scan storageUsage

    if storageUsage > 80
        flush tempFiles
        alert ""Storage Pressure""
    end
}

export {
    ioLoad = fileSystem.readLoad
    safety = backup.health
}");

				case "security":
					return BuildModule("Security", modeValue,
@"require SystemCore as core

budget {
    cpu <= 36
    memory <= 40
}

boot {
    load firewall
    protect userFiles
}

tick every 5s {
    scan incomingPackets

    if threats > 0
        block unknownPorts
        alert ""Threat Detected""
    end
}

export {
    threatLevel = firewall.risk
    encryption = security.encryption
}");

				case "control":
					return BuildModule("Control", modeValue,
@"require SystemCore as core

budget {
    cpu <= 32
    memory <= 40
}

boot {
    bind display
    bind devices
    route input to devices
}

tick every 1s {
    monitor inputDelay

    if inputDelay > 25
        reduce inputDelay by 10
        alert ""Input Delay""
    end
}

export {
    deviceSupport = devices.ready
    latency = inputDelay
}");

				case "time":
					return BuildModule("Time", modeValue,
@"boot {
    start clock
}

speed 60x");

				case "firewall":
					return BuildModule("Firewall", modeValue,
@"scanTraffic = true
allowUnknownPorts = false

require Security as security

budget {
    cpu <= 18
    memory <= 24
}

boot {
    load rules
    allow trustedApps
    block unknownPorts
}

tick every 2s {
    scan packets
    protect openPorts
}

export {
    protection = firewall.score
}");

				case "file_system":
					return BuildModule("FileSystem", modeValue,
@"require Data as data

budget {
    cpu <= 24
    memory <= 28
    storage <= 40
}

boot {
    mount data.disk
    index directories
    cache fileTable
}

tick every 5s {
    monitor storageUsage

    if storageUsage > 80
        flush tempFiles
        alert ""Storage Pressure""
    end
}

export {
    readSpeed = fileSystem.read
    writeSpeed = fileSystem.write
    fragmentation = fileSystem.fragmentation
}");

				case "memory_manager":
					return BuildModule("MemoryManager", modeValue,
@"require Kernel as kernel

budget {
    cpu <= 28
    memory <= 36
}

boot {
    start allocator
    watch processes
}

tick every 1s {
    monitor memory.pages

    if memory.pressure > 80
        recover cache
        alert ""Memory Pressure""
    end
}

export {
    processLimit = allocator.limit
    leakRisk = allocator.leakRisk
}");

				case "display_manager":
					return BuildModule("DisplayManager", modeValue,
@"require Control as control

budget {
    cpu <= 22
    memory <= 32
}

boot {
    bind display
    bind renderer
}

tick every 1s {
    monitor inputDelay

    if inputDelay > 25
        reduce inputDelay by 10
        warning ""Display Delay""
    end
}

export {
    usability = display.usability
    gpuUsage = renderer.gpuUsage
}");

				default:
					return BuildModule(
						ToModuleName(string.IsNullOrWhiteSpace(title) ? documentId : title),
						modeValue,
@"budget {
    cpu <= 32
    memory <= 32
}

boot {
    start service
}

tick every 1s {
    monitor system.load
}

export {
    status = running
}");
			}
		}

		private static string BuildModule(string moduleName, string modeValue, string body)
		{
			return $"module {moduleName}\n\nmode = {modeValue}\n\n{body}";
		}

		private static string ToModuleName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "NewModule";
			}

			string[] parts = value
				.Replace("-", " ")
				.Replace("_", " ")
				.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

			string result = string.Empty;

			foreach (string part in parts)
			{
				result += char.ToUpperInvariant(part[0]) + part.Substring(1);
			}

			return string.IsNullOrWhiteSpace(result)
				? "NewModule"
				: result;
		}
	}
}
