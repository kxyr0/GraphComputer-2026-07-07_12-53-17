using System;
using UnityEngine;
using Game.World.Time;

namespace Game.World.News
{
	public sealed class EraNewsService : MonoBehaviour
	{
		[SerializeField] private GameTimeService timeService;
		[SerializeField] private EraNewsEntry[] newsEntries;

		private void Reset()
		{
			newsEntries = new[]
			{
				new EraNewsEntry(
					1995,
					1,
					1,
					"New OS race begins",
					"Tech Daily",
					"Neutral",
					"Companies are preparing the next generation of desktop systems."
				),
				new EraNewsEntry(
					1995,
					2,
					10,
					"Users demand faster interfaces",
					"System Weekly",
					"Performance",
					"Slow windows and unstable drivers are becoming the main user complaint."
				),
				new EraNewsEntry(
					1995,
					4,
					5,
					"Security becomes a selling point",
					"Network Press",
					"Security",
					"Home networks are growing, and users are starting to care about protection."
				)
			};
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
			if (newsEntries == null || newsEntries.Length == 0)
			{
				return EraNewsEntry.Empty;
			}

			int currentDay = timeService != null
				? timeService.GetAbsoluteDay()
				: int.MaxValue;

			EraNewsEntry best = EraNewsEntry.Empty;
			int bestDay = int.MinValue;

			foreach (EraNewsEntry entry in newsEntries)
			{
				if (entry == null)
				{
					continue;
				}

				int entryDay = GameTimeService.GetAbsoluteDay(
					entry.Year,
					entry.Month,
					entry.Day
				);

				if (entryDay > currentDay)
				{
					continue;
				}

				if (entryDay <= bestDay)
				{
					continue;
				}

				best = entry;
				bestDay = entryDay;
			}

			return best;
		}
	}

	[Serializable]
	public sealed class EraNewsEntry
	{
		public static readonly EraNewsEntry Empty = new(
			0,
			0,
			0,
			"No signal",
			"Unknown",
			"None",
			"No era news available."
		);

		[SerializeField] private int year;
		[SerializeField] private int month;
		[SerializeField] private int day;

		[SerializeField] private string title;
		[SerializeField] private string source;
		[SerializeField] private string impact;
		[SerializeField, TextArea(2, 5)] private string body;

		public int Year => year;
		public int Month => month;
		public int Day => day;

		public string Title => title;
		public string Source => source;
		public string Impact => impact;
		public string Body => body;

		public EraNewsEntry(
			int year,
			int month,
			int day,
			string title,
			string source,
			string impact,
			string body)
		{
			this.year = year;
			this.month = month;
			this.day = day;
			this.title = title;
			this.source = source;
			this.impact = impact;
			this.body = body;
		}
	}
}