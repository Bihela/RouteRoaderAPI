using Gemini_AI_Api.Models;
using Gemini_AI_Api.Servies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gemini_AI_Api.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class HomeController : Controller
	{
		private readonly IGeminiService _geminiService;
		private readonly ILogger<HomeController> _logger;

		public HomeController(IGeminiService geminiService, ILogger<HomeController> logger)
		{
			_geminiService = geminiService;
			_logger = logger;
		}

		[HttpPost]
		[SwaggerOperation(Summary = "Ask a question to the AI model")]
		[SwaggerResponse(200, "The AI's response to the question", typeof(GeminiResponse))]
		[SwaggerResponse(400, "An error occurred while making the request")]
		[SwaggerResponse(500, "An error occurred while parsing the response")]
		public async Task<IActionResult> AskQuestion([FromBody] QuestionRequest request)
		{
			try
			{
				if (request == null)
				{
					return BadRequest("Invalid request. Request cannot be null.");
				}

				var validationResults = new List<ValidationResult>();
				var validationContext = new ValidationContext(request);
				if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
				{
					return BadRequest(string.Join("; ", validationResults.Select(vr => vr.ErrorMessage)));
				}

				var response = await _geminiService.AskQuestionAsync(request);
				_logger.LogInformation("Question asked successfully.");
				return Ok(response);
			}
			catch (HttpRequestException e)
			{
				_logger.LogError(e, "HTTP request error while asking question.");
				return BadRequest($"HTTP request error: {e.Message}");
			}
			catch (JsonException e)
			{
				_logger.LogError(e, "JSON parsing error while processing response.");
				return StatusCode(500, $"JSON parsing error: {e.Message}");
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Exception error while processing response.");
				return StatusCode(500, $"Exception error: {e.Message}");
			}
		}
	}
}
