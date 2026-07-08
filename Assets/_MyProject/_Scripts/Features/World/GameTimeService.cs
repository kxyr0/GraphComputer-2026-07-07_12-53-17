using System;
using UnityEngine;

namespace Game.World.Time
{
	public sealed class GameTimeService : MonoBehaviour
	{
		[SerializeField] private int startYear = 1995;
		[SerializeField, Range(1, 12)] private int startMonth = 1;
		[SerializeField, Range(1, 30)] private int startDay = 1;
		[SerializeField, Range(0, 23)] private int startHour = 8;
		[SerializeField, Range(0, 59)] private int startMinute = 0;

		[Header("Speed")]
		[SerializeField, Min(0f)] private float timeSpeed = 4f;

		[SerializeField, HideInInspector]
		private float realSecondsPerGameMinute = 0.25f;

		[SerializeField] private bool paused;

		[Header("Module Speed Limits")]
		[SerializeField, Min(0f)] private float minModuleSpeed = 0f;
		[SerializeField, Min(1f)] private float maxModuleSpeed = 86400f;

		private float gameMinuteAccumulator;

		public int Year { get; private set; }
		public int Month { get; private set; }
		public int Day { get; private set; }
		public int Hour { get; private set; }
		public int Minute { get; private set; }

		public bool Paused => paused || timeSpeed <= 0f;
		public float TimeSpeed => timeSpeed;
		public float RealSecondsPerGameMinute => realSecondsPerGameMinute;

		public event Action TimeChanged;

		private void Awake()
		{
			InitializeTime();
			InitializeSpeed();

			TimeChanged?.Invoke();
		}

		private void OnValidate()
		{
			startMonth = Mathf.Clamp(startMonth, 1, 12);
			startDay = Mathf.Clamp(startDay, 1, 30);
			startHour = Mathf.Clamp(startHour, 0, 23);
			startMinute = Mathf.Clamp(startMinute, 0, 59);

			minModuleSpeed = Mathf.Max(0f, minModuleSpeed);
			maxModuleSpeed = Mathf.Max(1f, maxModuleSpeed);

			if (maxModuleSpeed < minModuleSpeed)
			{
				maxModuleSpeed = minModuleSpeed;
			}

			if (timeSpeed <= 0f && realSecondsPerGameMinute > 0f)
			{
				timeSpeed = 1f / realSecondsPerGameMinute;
			}

			timeSpeed = Mathf.Clamp(timeSpeed, minModuleSpeed, maxModuleSpeed);
			SyncLegacySecondsFromTimeSpeed();
		}

		private void Update()
		{
			if (Paused)
			{
				return;
			}

			gameMinuteAccumulator += UnityEngine.Time.deltaTime * timeSpeed;

			int minutesToAdd = Mathf.FloorToInt(gameMinuteAccumulator);

			if (minutesToAdd <= 0)
			{
				return;
			}

			gameMinuteAccumulator -= minutesToAdd;
			AddMinutes(minutesToAdd);
		}

		public void SetPaused(bool value)
		{
			paused = value;
			TimeChanged?.Invoke();
		}

		public void SetSpeed(float secondsPerGameMinute)
		{
			if (secondsPerGameMinute <= 0f)
			{
				SetTimeSpeedFromModule(0f);
				return;
			}

			float speed = 1f / secondsPerGameMinute;
			SetTimeSpeedFromModule(speed);
		}

		public void SetTimeSpeedFromModule(float speed)
		{
			float clampedSpeed = Mathf.Clamp(
				speed,
				minModuleSpeed,
				maxModuleSpeed
			);

			timeSpeed = clampedSpeed;
			gameMinuteAccumulator = 0f;

			SyncLegacySecondsFromTimeSpeed();

			if (timeSpeed <= 0f)
			{
				paused = true;
			}
			else
			{
				paused = false;
			}

			TimeChanged?.Invoke();
		}

		public string GetClockText()
		{
			return $"{Hour:00}:{Minute:00}";
		}

		public string GetDateText()
		{
			return $"{Day:00}.{Month:00}.{Year}";
		}

		public string GetStatusText()
		{
			return Paused ? "Paused" : "Running";
		}

		public string GetSpeedText()
		{
			return $"{timeSpeed:0.###}x";
		}

		public int GetAbsoluteDay()
		{
			return GetAbsoluteDay(Year, Month, Day);
		}

		public static int GetAbsoluteDay(int year, int month, int day)
		{
			return year * 360 + (month - 1) * 30 + (day - 1);
		}

		private void InitializeTime()
		{
			Year = startYear;
			Month = startMonth;
			Day = startDay;
			Hour = startHour;
			Minute = startMinute;
		}

		private void InitializeSpeed()
		{
			if (timeSpeed <= 0f && realSecondsPerGameMinute > 0f)
			{
				timeSpeed = 1f / realSecondsPerGameMinute;
			}

			timeSpeed = Mathf.Clamp(timeSpeed, minModuleSpeed, maxModuleSpeed);
			SyncLegacySecondsFromTimeSpeed();
		}

		private void SyncLegacySecondsFromTimeSpeed()
		{
			realSecondsPerGameMinute = timeSpeed > 0f
				? 1f / timeSpeed
				: 0f;
		}

		private void AddMinutes(int minutesToAdd)
		{
			if (minutesToAdd <= 0)
			{
				return;
			}

			long absoluteMinutes =
				((long)GetAbsoluteDay() * 24L * 60L) +
				(Hour * 60L) +
				Minute +
				minutesToAdd;

			long absoluteDays = absoluteMinutes / 1440L;
			int minuteOfDay = (int)(absoluteMinutes % 1440L);

			if (minuteOfDay < 0)
			{
				minuteOfDay += 1440;
				absoluteDays--;
			}

			Year = (int)(absoluteDays / 360L);

			int dayOfYear = (int)(absoluteDays % 360L);

			if (dayOfYear < 0)
			{
				dayOfYear += 360;
				Year--;
			}

			Month = dayOfYear / 30 + 1;
			Day = dayOfYear % 30 + 1;

			Hour = minuteOfDay / 60;
			Minute = minuteOfDay % 60;

			TimeChanged?.Invoke();
		}
	}
}