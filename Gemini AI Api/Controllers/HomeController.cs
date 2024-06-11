using Gemini_AI_Api.Models;
using Gemini_AI_Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using System.Text.Json;


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
				if (request == null || string.IsNullOrEmpty(request.StartLocation) || string.IsNullOrEmpty(request.FinishLocation))
				{
					return BadRequest("Invalid request. StartLocation and FinishLocation cannot be null or empty.");
				}

				var validationResults = new List<ValidationResult>();
				var validationContext = new ValidationContext(request);
				if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
				{
					return BadRequest(string.Join("; ", validationResults.Select(vr => vr.ErrorMessage)));
				}

				var response = await _geminiService.AskQuestionAsync(request);
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
