using Game.Research.Runtime;
using Game.World.News;
using Game.World.Time;
using VContainer;
using VContainer.Unity;

namespace Game.Composition
{
	public static class CoreSceneInstaller
	{
		public static void Register(IContainerBuilder builder)
		{
			builder.RegisterComponentInHierarchy<GameTimeService>();
			builder.RegisterComponentInHierarchy<EraNewsService>();
			builder.RegisterComponentInHierarchy<ResearchService>()
				.AsSelf()
				.As<IResearchService>();
		}
	}
}
