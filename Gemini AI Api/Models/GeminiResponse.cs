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
}
