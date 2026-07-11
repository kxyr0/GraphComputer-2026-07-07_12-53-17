using Game.UI.OS;
using Game.UI.Research;
using VContainer;
using VContainer.Unity;

namespace Game.Composition
{
	public static class UiSceneInstaller
	{
		public static void Register(IContainerBuilder builder)
		{
			builder.RegisterComponentInHierarchy<OsInfoPresenter>();
			builder.RegisterComponentInHierarchy<OsDeployBuildButtonManager>();
			builder.RegisterComponentInHierarchy<ResearchTreePresenter>();
			builder.RegisterComponentInHierarchy<ResearchWindowController>();
		}
	}
}
