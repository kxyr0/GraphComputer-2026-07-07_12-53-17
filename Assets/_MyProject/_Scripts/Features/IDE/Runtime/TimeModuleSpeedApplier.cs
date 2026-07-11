using Game.IDE.Runtime;
using Game.World.Time;
using UnityEngine;
using VContainer;

namespace Game.IDE.Presentation
{
	[DisallowMultipleComponent]
	public sealed class TimeModuleSpeedApplier : MonoBehaviour
	{
		[SerializeField] private GameTimeService gameTimeService;
		[SerializeField] private bool logAppliedSpeed = true;

		[Inject]
		private void Construct(GameTimeService timeService)
		{
			if (gameTimeService == null)
			{
				gameTimeService = timeService;
			}
		}

		private void Awake()
		{
			if (gameTimeService == null)
			{
				gameTimeService = FindFirstObjectByType<GameTimeService>(FindObjectsInactive.Include);
			}
		}

		public void TryApply(
			string documentId,
			string code,
			bool buildSucceeded)
		{
			if (!buildSucceeded)
			{
				return;
			}

			if (!IsTimeDocument(documentId, code))
			{
				return;
			}

			if (!TimeModuleSpeedParser.TryParseSpeed(
					code,
					out float speed,
					out string error))
			{
				Debug.LogWarning($"[Time Module] {error}", this);
				return;
			}

			if (gameTimeService == null)
			{
				gameTimeService = FindFirstObjectByType<GameTimeService>(FindObjectsInactive.Include);
			}

			if (gameTimeService == null)
			{
				Debug.LogError("[Time Module] GameTimeService was not found.", this);
				return;
			}

			gameTimeService.SetTimeSpeedFromModule(speed);

			if (logAppliedSpeed)
			{
				Debug.Log($"[Time Module] Game speed applied: {speed:0.###}x", this);
			}
		}

		private static bool IsTimeDocument(string documentId, string code)
		{
			if (!string.IsNullOrWhiteSpace(documentId))
			{
				string normalizedId = documentId
					.Trim()
					.ToLowerInvariant()
					.Replace(" ", "_");

				if (normalizedId == "time" || normalizedId == "module_time")
				{
					return true;
				}
			}

			return TimeModuleSpeedParser.IsTimeModule(code);
		}
	}
}
