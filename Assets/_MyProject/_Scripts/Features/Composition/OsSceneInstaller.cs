using Game.Nodes.Runtime;
using Game.OS.Competitors;
using Game.OS.Release;
using Game.OS.Reputation;
using Game.World.Reports;
using VContainer;
using VContainer.Unity;

namespace Game.Composition
{
	public static class OsSceneInstaller
	{
		public static void Register(IContainerBuilder builder)
		{
			builder.RegisterComponentInHierarchy<OsRuntimeMetricsService>();
			builder.RegisterComponentInHierarchy<OsReleaseService>();
			builder.RegisterComponentInHierarchy<OsPlayerReputationService>();
			builder.RegisterComponentInHierarchy<OsCompetitorService>();
			builder.RegisterComponentOnNewGameObject<UserReportsService>(
				Lifetime.Scoped,
				nameof(UserReportsService));
		}
	}
}
