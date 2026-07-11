using UnityEngine;
using Game.World.Time;
using VContainer;

namespace Game.World.News
{
	public sealed class EraNewsService : MonoBehaviour
	{
		[SerializeField] private GameTimeService timeService;
		[SerializeField] private EraNewsDatabase database;

		[Inject]
		private void Construct(GameTimeService gameTimeService)
		{
			if (timeService == null)
			{
				timeService = gameTimeService;
			}
		}

		private void Awake()
		{
			if (timeService == null)
			{
				timeService = FindFirstObjectByType<GameTimeService>();
			}
		}

		public EraNewsEntry GetCurrentNews()
		{
			if (database == null)
			{
				return EraNewsEntry.Empty;
			}

			int currentDay = timeService != null
				? timeService.GetAbsoluteDay()
				: int.MaxValue;

			return database.GetCurrentNews(currentDay);
		}
	}
}
