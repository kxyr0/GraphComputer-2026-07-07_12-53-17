using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Composition
{
	[DisallowMultipleComponent]
	public sealed class GameLifetimeScope : LifetimeScope
	{
		protected override void Configure(IContainerBuilder builder)
		{
			CoreSceneInstaller.Register(builder);
			NodeSceneInstaller.Register(builder);
			IdeSceneInstaller.Register(builder);
			OsSceneInstaller.Register(builder);
			UiSceneInstaller.Register(builder);
		}
	}
}
