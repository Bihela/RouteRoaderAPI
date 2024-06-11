using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Gemini_AI_Api.Models;
using Gemini_AI_Api.Validation;
using NUnit.Framework;

namespace Gemini_AI_Api.Tests.Validation
{
	public class ValidationAttributesTests
	{
		[Test]
		public void DepartureDateAttribute_PastDate_ReturnsErrorMessage()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.Now.AddDays(-1),
				Duration = 1
			};

			var validationContext = new ValidationContext(request);
			var validationResults = new List<ValidationResult>();
			var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

			Assert.That(isValid, Is.False);
			Assert.That(validationResults[0].ErrorMessage, Is.EqualTo("DepartureDate must be in the future."));
		}

		[Test]
		public void DepartureDateAttribute_FutureDate_IsValid()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.Now.AddDays(1),
				Duration = 1
			};

			var validationContext = new ValidationContext(request);
			var validationResults = new List<ValidationResult>();
			var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

			Assert.That(isValid, Is.True);
		}
	}
}
