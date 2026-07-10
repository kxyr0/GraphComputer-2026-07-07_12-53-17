using System;
using UnityEngine;

namespace Game.UI.OS
{
	[Serializable]
	public sealed class OsInfoEntry
	{
		[SerializeField] private string label;
		[SerializeField] private string value;
		[SerializeField] private OsInfoTone tone;

		public string Label => label ?? string.Empty;
		public string Value => value ?? string.Empty;
		public OsInfoTone Tone => tone;

		public OsInfoEntry()
		{
		}

		public OsInfoEntry(string label, string value)
			: this(label, value, OsInfoTone.Neutral)
		{
		}

		public OsInfoEntry(string label, string value, OsInfoTone tone)
		{
			this.label = label;
			this.value = value;
			this.tone = tone;
		}
	}

	public enum OsInfoTone
	{
		Neutral,
		Good,
		Warning,
		Bad
	}
}
