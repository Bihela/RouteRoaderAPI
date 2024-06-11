using System;
using System.ComponentModel.DataAnnotations;

namespace Gemini_AI_Api.Validation
{
	public class DepartureDateAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value is DateTime date)
			{
				if (date < DateTime.Now)
				{
					return new ValidationResult("DepartureDate must be in the future.");
				}
			}
			return ValidationResult.Success;
		}
	}
}
