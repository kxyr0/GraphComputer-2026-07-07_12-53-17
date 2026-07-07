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
		[SerializeField, Min(0.01f)] private float realSecondsPerGameMinute = 0.25f;
		[SerializeField] private bool paused;

		private float accumulator;

		public int Year { get; private set; }
		public int Month { get; private set; }
		public int Day { get; private set; }
		public int Hour { get; private set; }
		public int Minute { get; private set; }

		public bool Paused => paused;
		public float RealSecondsPerGameMinute => realSecondsPerGameMinute;

		public event Action TimeChanged;

		private void Awake()
		{
			Year = startYear;
			Month = startMonth;
			Day = startDay;
			Hour = startHour;
			Minute = startMinute;

			TimeChanged?.Invoke();
		}

		private void Update()
		{
			if (paused)
			{
				return;
			}

			accumulator += UnityEngine.Time.deltaTime;

			while (accumulator >= realSecondsPerGameMinute)
			{
				accumulator -= realSecondsPerGameMinute;
				AddMinute();
			}
		}

		public void SetPaused(bool value)
		{
			paused = value;
			TimeChanged?.Invoke();
		}

		public void SetSpeed(float secondsPerGameMinute)
		{
			realSecondsPerGameMinute = Mathf.Max(0.01f, secondsPerGameMinute);
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
			return paused ? "Paused" : "Running";
		}

		public string GetSpeedText()
		{
			return $"{1f / realSecondsPerGameMinute:0.0}x";
		}

		public int GetAbsoluteDay()
		{
			return GetAbsoluteDay(Year, Month, Day);
		}

		public static int GetAbsoluteDay(int year, int month, int day)
		{
			return year * 360 + (month - 1) * 30 + (day - 1);
		}

		private void AddMinute()
		{
			Minute++;

			if (Minute >= 60)
			{
				Minute = 0;
				Hour++;
			}

			if (Hour >= 24)
			{
				Hour = 0;
				Day++;
			}

			if (Day > 30)
			{
				Day = 1;
				Month++;
			}

			if (Month > 12)
			{
				Month = 1;
				Year++;
			}

			TimeChanged?.Invoke();
		}
	}
}