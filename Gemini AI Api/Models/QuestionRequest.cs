﻿namespace Gemini_AI_Api.Models
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using Gemini_AI_Api.Validation;

	public class QuestionRequest
	{
		public string StartLocation { get; set; }
		public string FinishLocation { get; set; }
		public List<string> ContinuationPoints { get; set; }

		[DepartureDate]
		public DateTime DepartureDate { get; set; }

		[Range(1, int.MaxValue, ErrorMessage = "The integer value must be greater than zero.")]
		public int Duration { get; set; }
	}
}
