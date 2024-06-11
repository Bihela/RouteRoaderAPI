using System.ComponentModel.DataAnnotations;
using Gemini_AI_Api.Models;

namespace Gemini_AI_Api.Validation
{
	public class StartAndFinishLocationAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value is QuestionRequest request)
			{
				if (string.IsNullOrEmpty(request.StartLocation) || string.IsNullOrEmpty(request.FinishLocation))
				{
					return new ValidationResult("StartLocation and FinishLocation cannot be null or empty.");
				}
			}
			return ValidationResult.Success;
		}
	}
}
