using System;
using UnityEngine;

namespace Game.UI.OS
{
	public enum OsInfoPageType
	{
		SystemInformation,
		HealthSystem,
		ReleaseInformation,
		Competitors
	}

	[Serializable]
	public sealed class OsInfoPageBinding
	{
		[SerializeField] private OsInfoPageType pageType;
		[SerializeField] private OsInfoPanelView panel;

		public OsInfoPageType PageType => pageType;

		public void Render(OsInfoSection section)
		{
			panel?.Render(section);
		}

		public void Clear()
		{
			panel?.Clear();
		}
	}
}
