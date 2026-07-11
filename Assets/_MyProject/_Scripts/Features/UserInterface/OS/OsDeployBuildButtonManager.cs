using Game.OS.Release;
using Michsky.MUIP;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI.OS
{
	[DisallowMultipleComponent]
	public sealed class OsDeployBuildButtonManager : MonoBehaviour
	{
		[SerializeField] private ButtonManager button;
		[SerializeField] private OsReleaseService releaseService;

		public ButtonManager Button => button;

		[Inject]
		private void Construct(OsReleaseService osReleaseService)
		{
			if (releaseService == null)
			{
				releaseService = osReleaseService;
			}
		}

		private void Reset()
		{
			button = GetComponent<ButtonManager>();
		}

		private void Awake()
		{
			if (button == null)
			{
				button = GetComponent<ButtonManager>();
			}

			ResolveReleaseService();
		}

		private void OnEnable()
		{
			if (button != null)
			{
				button.onClick.AddListener(DeployBuild);
			}
		}

		private void OnDisable()
		{
			if (button != null)
			{
				button.onClick.RemoveListener(DeployBuild);
			}
		}

		public void DeployBuild()
		{
			ResolveReleaseService();

			if (releaseService == null)
			{
				Debug.LogError($"[{nameof(OsDeployBuildButtonManager)}] Release service is missing.", this);
				return;
			}

			releaseService.DeployBuild();
		}

		private void ResolveReleaseService()
		{
			if (releaseService != null)
			{
				return;
			}

			releaseService = OsReleaseService.GetOrCreate();
		}
	}
}
