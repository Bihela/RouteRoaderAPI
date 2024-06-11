namespace Gemini_AI_Api.Models
{
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
