namespace Gemini_AI_Api.Models
{
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
}
