using Game.OS.Release;
using UnityEngine;

namespace Game.OS.Reputation
{
	[DisallowMultipleComponent]
	public sealed class OsPlayerReputationService : MonoBehaviour
	{
		[SerializeField] private OsPlayerReputationProfile profile;
		[SerializeField] private bool resetOnAwake = true;
		[SerializeField] private bool logReputationChanges;

		private float reputationPercent;

		public bool HasProfile => profile != null;
		public float CurrentReputationPercent => Mathf.Clamp(reputationPercent, 0f, 100f);
		public string CurrentTier => profile != null
			? profile.GetTierText(CurrentReputationPercent)
			: "No Profile";

		private void Awake()
		{
			if (resetOnAwake)
			{
				ResetReputation();
			}
			else
			{
				reputationPercent = Mathf.Clamp(reputationPercent, 0f, 100f);
			}
		}

		public OsReleaseAudienceModifiers GetAudienceModifiers()
		{
			return profile != null
				? profile.EvaluateAudienceModifiers(CurrentReputationPercent)
				: OsReleaseAudienceModifiers.Neutral;
		}

		public void ApplyReleaseFeedback(float releaseQuality, OsReleaseGrowthResult growth)
		{
			if (profile == null)
			{
				return;
			}

			float previous = CurrentReputationPercent;
			reputationPercent = profile.EvaluateNextReputation(
				previous,
				releaseQuality,
				growth.Reception,
				growth.InstallDelta
			);

			if (logReputationChanges && !Mathf.Approximately(previous, reputationPercent))
			{
				Debug.Log(
					$"[{nameof(OsPlayerReputationService)}] Reputation {previous:0.0}% -> {reputationPercent:0.0}% ({CurrentTier}).",
					this
				);
			}
		}

		[ContextMenu("Reset Reputation")]
		public void ResetReputation()
		{
			reputationPercent = profile != null
				? profile.InitialReputationPercent
				: OsPlayerReputationProfile.DefaultInitialReputationPercent;
		}

		public void SetProfile(OsPlayerReputationProfile newProfile, bool resetReputation)
		{
			profile = newProfile;

			if (resetReputation)
			{
				ResetReputation();
			}
		}
	}
}
