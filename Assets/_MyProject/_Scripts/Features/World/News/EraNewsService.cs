using UnityEngine;
using Game.World.Time;

namespace Game.World.News
{
	public sealed class EraNewsService : MonoBehaviour
	{
		[SerializeField] private GameTimeService timeService;
		[SerializeField] private EraNewsDatabase database;

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