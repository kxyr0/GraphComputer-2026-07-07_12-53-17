using System;
using System.Collections.Generic;

namespace Game.OS.Competitors
{
	public static class OsCompetitorLeaderboardBuilder
	{
		public static OsCompetitorState[] CreateRankedSnapshot(
			OsCompetitorState[] competitors,
			OsCompetitorState player,
			bool includePlayer)
		{
			List<OsCompetitorState> entries = new();

			if (competitors != null)
			{
				entries.AddRange(competitors);
			}

			if (includePlayer)
			{
				entries.Add(player);
			}

			entries.Sort(CompareByUsers);
			return entries.ToArray();
		}

		private static int CompareByUsers(OsCompetitorState left, OsCompetitorState right)
		{
			int userComparison = right.CurrentUsers.CompareTo(left.CurrentUsers);

			if (userComparison != 0)
			{
				return userComparison;
			}

			if (left.IsPlayer != right.IsPlayer)
			{
				return left.IsPlayer ? -1 : 1;
			}

			return string.Compare(
				left.CompanyName,
				right.CompanyName,
				StringComparison.OrdinalIgnoreCase
			);
		}
	}
}
