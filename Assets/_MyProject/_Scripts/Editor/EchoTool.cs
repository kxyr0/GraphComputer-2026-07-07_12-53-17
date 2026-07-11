using Game.Research;
using Game.Research.Runtime;
using Game.World.Time;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
	public sealed class EchoTool : EditorWindow
	{
		private const float DefaultDebugTimeSpeed = 1440f;
		private const float NormalTimeSpeed = 4f;

		private GameTimeService timeService;
		private ResearchService researchService;
		private Vector2 scrollPosition;
		private float debugTimeSpeed = DefaultDebugTimeSpeed;
		private string researchId = string.Empty;
		private bool includeLockedResearches;

		[MenuItem("Tools/Graph Computer/EchoTool")]
		public static void Open()
		{
			GetWindow<EchoTool>("EchoTool");
		}

		private void OnInspectorUpdate()
		{
			Repaint();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("EchoTool", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"Runtime debug panel. Assign scene services manually or use Find Scene Services while testing in Play Mode.",
				MessageType.Info);

			DrawServiceSection();

			EditorGUILayout.Space(8f);

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			DrawTimeSection();
			EditorGUILayout.Space(12f);
			DrawResearchSection();
			EditorGUILayout.EndScrollView();
		}

		private void DrawServiceSection()
		{
			EditorGUILayout.LabelField("Scene Services", EditorStyles.boldLabel);

			timeService = (GameTimeService)EditorGUILayout.ObjectField(
				"Time Service",
				timeService,
				typeof(GameTimeService),
				true);

			researchService = (ResearchService)EditorGUILayout.ObjectField(
				"Research Service",
				researchService,
				typeof(ResearchService),
				true);

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Find Scene Services"))
				{
					FindSceneServices();
				}

				if (GUILayout.Button("Clear References"))
				{
					timeService = null;
					researchService = null;
				}
			}

			if (!Application.isPlaying)
			{
				EditorGUILayout.HelpBox("Enter Play Mode to apply runtime debug actions.", MessageType.Warning);
			}
		}

		private void DrawTimeSection()
		{
			EditorGUILayout.LabelField("Time Debug", EditorStyles.boldLabel);

			if (timeService == null)
			{
				EditorGUILayout.HelpBox("Time Service is not assigned.", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField("Date", $"{timeService.GetDateText()} {timeService.GetClockText()}");
			EditorGUILayout.LabelField("Status", timeService.GetStatusText());
			EditorGUILayout.LabelField("Current Speed", timeService.GetSpeedText());

			debugTimeSpeed = EditorGUILayout.FloatField("Debug Speed", debugTimeSpeed);

			using (new EditorGUI.DisabledScope(!Application.isPlaying))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					DrawSpeedPresetButton("1x", 1f);
					DrawSpeedPresetButton("60x", 60f);
					DrawSpeedPresetButton("1 day/sec", 1440f);
					DrawSpeedPresetButton("7 days/sec", 10080f);
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("Apply Speed"))
					{
						ApplyTimeSpeed(debugTimeSpeed);
					}

					if (GUILayout.Button("Normal 4x"))
					{
						ApplyTimeSpeed(NormalTimeSpeed);
					}
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("Pause"))
					{
						timeService.SetPaused(true);
						Log("Time paused.");
					}

					if (GUILayout.Button("Resume"))
					{
						timeService.SetPaused(false);
						Log("Time resumed.");
					}
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("+1 day"))
					{
						timeService.DebugAdvanceDays(1);
						Log("Advanced time by 1 day.");
					}

					if (GUILayout.Button("+7 days"))
					{
						timeService.DebugAdvanceDays(7);
						Log("Advanced time by 7 days.");
					}

					if (GUILayout.Button("+30 days"))
					{
						timeService.DebugAdvanceDays(30);
						Log("Advanced time by 30 days.");
					}
				}
			}
		}

		private void DrawResearchSection()
		{
			EditorGUILayout.LabelField("Research Debug", EditorStyles.boldLabel);

			if (researchService == null)
			{
				EditorGUILayout.HelpBox("Research Service is not assigned.", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField("Current Day", researchService.CurrentAbsoluteDay.ToString());
			EditorGUILayout.LabelField("Active Research", FormatEmpty(researchService.ActiveResearchId));

			using (new EditorGUI.DisabledScope(!Application.isPlaying))
			{
				EditorGUI.BeginChangeCheck();
				bool instantOnClick = EditorGUILayout.Toggle(
					"Instant Research On Click",
					researchService.DebugCompleteOnStart);

				if (EditorGUI.EndChangeCheck())
				{
					researchService.DebugCompleteOnStart = instantOnClick;
					Log($"Instant research on click: {(instantOnClick ? "enabled" : "disabled")}.");
				}

				includeLockedResearches = EditorGUILayout.Toggle(
					"Complete Locked By Id",
					includeLockedResearches);

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("Complete Available Chain"))
					{
						int count = researchService.DebugCompleteAvailableResearches();
						Log($"Completed available research chain: {count} item(s).");
					}

					if (GUILayout.Button("Complete All"))
					{
						int count = researchService.DebugCompleteAllResearches();
						Log($"Completed all researches: {count} new item(s).");
					}
				}

				if (GUILayout.Button("Reset Research Progress"))
				{
					researchService.DebugResetResearches();
					Log("Research progress reset.");
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					researchId = EditorGUILayout.TextField("Research Id", researchId);

					if (GUILayout.Button("Complete Id", GUILayout.Width(100f)))
					{
						CompleteResearchById();
					}
				}

				DrawResearchList();
			}
		}

		private void DrawResearchList()
		{
			ResearchConfig config = researchService.Config;

			if (config == null)
			{
				EditorGUILayout.HelpBox("Research Service Config is not assigned.", MessageType.Warning);
				return;
			}

			EditorGUILayout.Space(6f);
			EditorGUILayout.LabelField("Configured Researches", EditorStyles.boldLabel);

			foreach (ResearchDefinition research in config.Researches)
			{
				if (research == null)
				{
					continue;
				}

				ResearchStatus status = researchService.GetStatus(research.Id);

				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField(research.Id, GUILayout.MinWidth(180f));
					EditorGUILayout.LabelField(status.ToString(), GUILayout.Width(90f));

					if (GUILayout.Button("Complete", GUILayout.Width(80f)))
					{
						bool completed = researchService.DebugCompleteResearch(
							research.Id,
							includeLockedResearches);

						Log(completed
							? $"Completed research '{research.Id}'."
							: $"Research '{research.Id}' was not completed.");
					}
				}
			}
		}

		private void FindSceneServices()
		{
			timeService = Object.FindFirstObjectByType<GameTimeService>(FindObjectsInactive.Include);
			researchService = Object.FindFirstObjectByType<ResearchService>(FindObjectsInactive.Include);
		}

		private void DrawSpeedPresetButton(string label, float speed)
		{
			if (GUILayout.Button(label))
			{
				debugTimeSpeed = speed;
				ApplyTimeSpeed(speed);
			}
		}

		private void ApplyTimeSpeed(float speed)
		{
			float clampedSpeed = Mathf.Max(0f, speed);
			timeService.SetTimeSpeedFromModule(clampedSpeed);
			Log($"Time speed set to {clampedSpeed:0.###}x.");
		}

		private void CompleteResearchById()
		{
			bool completed = researchService.DebugCompleteResearch(
				researchId,
				includeLockedResearches);

			Log(completed
				? $"Completed research '{researchId}'."
				: $"Research '{researchId}' was not completed.");
		}

		private static string FormatEmpty(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
		}

		private static void Log(string message)
		{
			Debug.Log($"[EchoTool] {message}");
		}
	}
}
