using System;
using System.Collections.Generic;
using System.Linq;

namespace Gemini_AI_Api.Models
{
	public class GeminiResponse
	{
		public string Title { get; set; }
		public string Description { get; set; }
		public string Region { get; set; }
		public string Currency { get; set; }
		public List<Plan> Plan { get; set; }

		public bool ContainsNullValues()
		{
			if (string.IsNullOrEmpty(Title) ||
				string.IsNullOrEmpty(Description) ||
				string.IsNullOrEmpty(Region) ||
				string.IsNullOrEmpty(Currency) ||
				Plan == null ||
				Plan.Any(p => p == null || p.ContainsNullValues()))
			{
				return true;
			}
			return false;
		}
	}

	public class Plan
	{
		public int Day { get; set; }
		public string Destination { get; set; }
		public string Distance { get; set; }
		public string Duration { get; set; }
		public List<string> Activities { get; set; }

		public bool ContainsNullValues()
		{
			if (Day == 0 ||
				string.IsNullOrEmpty(Destination) ||
				string.IsNullOrEmpty(Distance) ||
				string.IsNullOrEmpty(Duration) ||
				Activities == null ||
				Activities.Any(a => string.IsNullOrEmpty(a)))
			{
				return true;
			}
			return false;
		}
	}

	public class DayDetails
	{
		public LocationDetails Departure { get; set; }
		public LocationDetails Arrival { get; set; }
		public int Distance { get; set; }
		public List<string> Activities { get; set; }

		public bool ContainsNullValues()
		{
			if (Departure == null ||
				Arrival == null ||
				Distance == 0 ||
				Activities == null ||
				Activities.Any(a => string.IsNullOrEmpty(a)))
			{
				return true;
			}
			return false;
		}
	}

	public class LocationDetails
	{
		public string Location { get; set; }
		public string Time { get; set; }
		public bool ContainsNullValues()
		{
			if (string.IsNullOrEmpty(Location) ||
				string.IsNullOrEmpty(Time))
			{
				return true;
			}
			return false;
		}
	}
}
