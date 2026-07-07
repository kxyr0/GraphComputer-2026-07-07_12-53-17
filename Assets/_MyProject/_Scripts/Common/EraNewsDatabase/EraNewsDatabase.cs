using System;
using System.Collections.Generic;
using Game.World.Time;
using UnityEngine;

namespace Game.World.News
{
	[CreateAssetMenu(
		fileName = "EraNewsDatabase",
		menuName = "Graph Computer/News/Era News Database"
	)]
	public sealed class EraNewsDatabase : ScriptableObject
	{
		[SerializeField] private List<EraNewsEntry> entries = new();

		public IReadOnlyList<EraNewsEntry> Entries => entries;

		public EraNewsEntry GetCurrentNews(int currentAbsoluteDay)
		{
			if (entries == null || entries.Count == 0)
			{
				return EraNewsEntry.Empty;
			}

			EraNewsEntry best = EraNewsEntry.Empty;
			int bestDay = int.MinValue;

			foreach (EraNewsEntry entry in entries)
			{
				if (entry == null || !entry.IsValid)
				{
					continue;
				}

				int entryDay = entry.GetAbsoluteDay();

				if (entryDay > currentAbsoluteDay)
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

		[ContextMenu("Sort By Date")]
		public void SortByDate()
		{
			if (entries == null)
			{
				return;
			}

			entries.Sort((a, b) =>
			{
				if (a == null && b == null)
				{
					return 0;
				}

				if (a == null)
				{
					return 1;
				}

				if (b == null)
				{
					return -1;
				}

				return a.GetAbsoluteDay().CompareTo(b.GetAbsoluteDay());
			});
		}

		[ContextMenu("Fill 1995 Default Events")]
		public void Fill1995DefaultEvents()
		{
			entries = new List<EraNewsEntry>
			{
				new(
					1995,
					1,
					1,
					"THE WEB IS WAKING UP",
					"The internet is moving from universities and labs into homes, offices and business plans."
				),
				new(
					1995,
					2,
					15,
					"FBI ARRESTS FAMOUS HACKER",
					"Kevin Mitnick is arrested in North Carolina after a high-profile electronic manhunt. Computer security becomes headline news."
				),
				new(
					1995,
					3,
					1,
					"A LOCAL EMAIL LIST STARTS SOMETHING BIG",
					"Craig Newmark begins a small San Francisco event mailing list. It will grow into Craigslist."
				),
				new(
					1995,
					3,
					2,
					"YAHOO BECOMES A COMPANY",
					"Yahoo is incorporated and begins its rise as one of the early web's most important portals."
				),
				new(
					1995,
					4,
					30,
					"THE INTERNET GOES COMMERCIAL",
					"NSFNET backbone funding ends, marking a major step toward the commercial internet era."
				),
				new(
					1995,
					5,
					23,
					"JAVA PROMISES WRITE ONCE, RUN ANYWHERE",
					"Sun Microsystems announces Java, pushing the idea of portable software for the web."
				),
				new(
					1995,
					6,
					8,
					"PHP ENTERS THE SERVER ROOM",
					"Rasmus Lerdorf releases the first version of PHP tools, quietly planting a seed for future dynamic websites."
				),
				new(
					1995,
					7,
					16,
					"AMAZON SELLS ITS FIRST BOOK",
					"Amazon opens as an online bookstore and proves that the web can become a real marketplace."
				),
				new(
					1995,
					8,
					9,
					"NETSCAPE IPO SHOCKS WALL STREET",
					"Netscape goes public and turns the web browser into a symbol of the internet gold rush."
				),
				new(
					1995,
					8,
					24,
					"WINDOWS 95 TAKES OVER THE DESKTOP",
					"Microsoft launches Windows 95 with massive marketing. The Start button becomes a symbol of mainstream computing."
				),
				new(
					1995,
					9,
					3,
					"AUCTIONWEB OPENS THE DIGITAL MARKET",
					"Pierre Omidyar launches AuctionWeb, the project that will later become eBay."
				),
				new(
					1995,
					10,
					15,
					"VOICE STARTS MOVING THROUGH THE INTERNET",
					"Internet phone software gains attention, hinting that voice calls may someday travel through networks instead of phone lines."
				),
				new(
					1995,
					11,
					22,
					"TOY STORY PROVES CGI CAN CARRY A FILM",
					"Toy Story reaches theaters as the first feature-length film made entirely with computer animation."
				),
				new(
					1995,
					12,
					4,
					"JAVASCRIPT MAKES WEB PAGES MOVE",
					"Netscape and Sun announce JavaScript, giving websites a path toward interactive behavior."
				),
				new(
					1995,
					12,
					15,
					"ALTAVISTA INDEXES THE WEB AT SPEED",
					"AltaVista launches with powerful full-text search, showing how large the web is becoming."
				)
			};

			SortByDate();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (entries == null)
			{
				return;
			}

			foreach (EraNewsEntry entry in entries)
			{
				entry?.Validate();
			}
		}
#endif
	}

	[Serializable]
	public sealed class EraNewsEntry
	{
		public static readonly EraNewsEntry Empty = new(
			0,
			0,
			0,
			"NO SIGNAL",
			"No news available."
		);

		[SerializeField, Min(0)] private int year = 1995;
		[SerializeField, Range(1, 12)] private int month = 1;
		[SerializeField, Range(1, 30)] private int day = 1;

		[SerializeField] private string headline = "NEW EVENT";

		[SerializeField, TextArea(2, 6)]
		private string description = "Event description.";

		public int Year => year;
		public int Month => month;
		public int Day => day;

		public string Headline => headline;
		public string Description => description;

		public string DateText => $"{day:00}.{month:00}.{year}";

		public bool IsValid =>
			year > 0 &&
			month >= 1 &&
			month <= 12 &&
			day >= 1 &&
			day <= 30 &&
			!string.IsNullOrWhiteSpace(headline);

		public EraNewsEntry(
			int year,
			int month,
			int day,
			string headline,
			string description)
		{
			this.year = year;
			this.month = month;
			this.day = day;
			this.headline = headline;
			this.description = description;
			Validate();
		}

		public int GetAbsoluteDay()
		{
			return GameTimeService.GetAbsoluteDay(year, month, day);
		}

		public void Validate()
		{
			year = Mathf.Max(0, year);
			month = Mathf.Clamp(month, 1, 12);
			day = Mathf.Clamp(day, 1, 30);

			if (string.IsNullOrWhiteSpace(headline))
			{
				headline = "NEW EVENT";
			}

			if (description == null)
			{
				description = string.Empty;
			}
		}
	}
}