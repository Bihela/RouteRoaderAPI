namespace Gemini_AI_Api.Models
{
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
}
