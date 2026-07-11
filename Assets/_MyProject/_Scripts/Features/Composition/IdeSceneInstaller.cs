using Game.IDE.Presentation;
using VContainer;
using VContainer.Unity;

namespace Game.Composition
{
	public static class IdeSceneInstaller
	{
		public static void Register(IContainerBuilder builder)
		{
			builder.RegisterComponentInHierarchy<NodeCodeRegistry>();
			builder.RegisterComponentInHierarchy<IdeRuntimeController>();
			builder.RegisterComponentInHierarchy<IdeBuildButtonController>();
			builder.RegisterComponentInHierarchy<IdeCodeTabsController>();
			builder.RegisterComponentInHierarchy<IdeBottomTabsController>();
			builder.RegisterComponentInHierarchy<IdeInputCodeEditor>();
			builder.RegisterComponentInHierarchy<IdeOutputConsoleView>();
			builder.RegisterComponentInHierarchy<IdeConsoleController>();
			builder.RegisterComponentInHierarchy<IdeAutocompleteController>();
			builder.RegisterComponentOnNewGameObject<TimeModuleSpeedApplier>(
				Lifetime.Scoped,
				nameof(TimeModuleSpeedApplier));
		}
	}
}
