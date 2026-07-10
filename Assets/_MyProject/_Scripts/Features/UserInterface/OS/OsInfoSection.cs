using System;
using UnityEngine;

namespace Game.UI.OS
{
	[Serializable]
	public sealed class OsInfoSection
	{
		[SerializeField] private string title;
		[SerializeField] private OsInfoEntry[] entries = Array.Empty<OsInfoEntry>();

		public string Title => title ?? string.Empty;
		public OsInfoEntry[] Entries => entries ?? Array.Empty<OsInfoEntry>();
		public bool HasEntries => Entries.Length > 0;

		public OsInfoSection()
		{
		}

		public OsInfoSection(string title, OsInfoEntry[] entries)
		{
			this.title = title;
			this.entries = entries ?? Array.Empty<OsInfoEntry>();
		}
	}
}
