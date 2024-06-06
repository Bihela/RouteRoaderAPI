﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Gemini_AI_Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Gemini_AI_Api.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class HomeController : Controller
	{
		private readonly HttpClient _client;
		private readonly ILogger<HomeController> _logger;

		public HomeController(HttpClient client, ILogger<HomeController> logger)
		{
			_client = client;
			_logger = logger;
		}

		[HttpPost]
		[SwaggerOperation(Summary = "Ask a question to the AI model")]
		[SwaggerResponse(200, "The AI's response to the question", typeof(GeminiResponse))]
		[SwaggerResponse(400, "An error occurred while making the request")]
		[SwaggerResponse(500, "An error occurred while parsing the response")]
		public async Task<IActionResult> AskQuestion([FromBody] QuestionRequest request)
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

			string apiKey = "AIzaSyB7SDV-7nIaqg8ufVsbV3OlD4Fagu7JLx4";
			string endpoint = $"https://generativelanguage.googleapis.com/v1/models/gemini-pro:generateContent?key={apiKey}";

			var requestContent = new
			{
				contents = new[]
				{
					new
					{
						role = "user",
						parts = new[]
						{
							new
							{
								text = $"Request:\n" +
									   $"Start Location: {request.StartLocation}\n" +
									   $"Finish Location: {request.FinishLocation}\n" +
									   $"Continuation Points: {(request.ContinuationPoints != null ? string.Join(", ", request.ContinuationPoints) : "None")}\n" +
									   $"Departure Date: {request.DepartureDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}\n" +
									   $"Duration: {request.Duration}\n\n" +
									   $"Respond Need to be in Raw Json:\n" +
									   $"Title\n" +
									   $"Description\n" +
									   $"Region\n" +
									   $"Currency\n" +
									   $"Plan(list topics will be day,destination,distance,duration,Activities"
							}
						}
					}
				}
			};

			var jsonContent = new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json");

			int retries = 3;
			for (int attempt = 1; attempt <= retries; attempt++)
			{
				try
				{
					var response = await _client.PostAsync(endpoint, jsonContent);
					if (response.IsSuccessStatusCode)
					{
						var responseBody = await response.Content.ReadAsStringAsync();
						_logger.LogInformation("Raw AI response: {ResponseBody}", responseBody);

						var jsonDoc = JsonDocument.Parse(responseBody);

						if (jsonDoc.RootElement.TryGetProperty("candidates", out var candidates))
						{
							foreach (var candidate in candidates.EnumerateArray())
							{
								if (candidate.TryGetProperty("content", out var content))
								{
									if (content.TryGetProperty("parts", out var partsArray))
									{
										var text = partsArray[0].GetProperty("text").GetString();

										var jsonText = text
											.Replace("```json", string.Empty)
											.Replace("```", string.Empty)
											.Trim();

										_logger.LogInformation("Cleaned JSON string: {JsonText}", jsonText);

										var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonText);
										if (geminiResponse == null)
										{
											_logger.LogError("Deserialized GeminiResponse is null.");
											return NotFound("No valid response from the AI model.");
										}

										if (geminiResponse.ContainsNullValues())
										{
											_logger.LogInformation("Null values found in the response. Retrying...");

											var retryResponse = await SendNewRequest(endpoint, request, retries);

										}
										else
										{
											_logger.LogInformation("Title: {Title}, Description: {Description}, Region: {Region}, Currency: {Currency}",
												geminiResponse.Title, geminiResponse.Description, geminiResponse.Region, geminiResponse.Currency);

											return Ok(geminiResponse);
										}
									}
								}
							}
						}

						return NotFound("No valid response from the AI model.");
					}
					else if (response.StatusCode == HttpStatusCode.InternalServerError && attempt < retries)
					{
						_logger.LogWarning("Received HTTP 500 error. Retrying... Attempt {Attempt}", attempt);
						await Task.Delay(1000);
					}
					else
					{
						return BadRequest($"HTTP request error: {response.StatusCode}");
					}
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
			}

			return BadRequest("Failed to get a successful response after multiple retries.");
		}

		private async Task<HttpResponseMessage> SendNewRequest(string endpoint, QuestionRequest request, int retries)
		{
			var requestContent = new
			{
				contents = new[]
				{
					new
					{
						role = "user",
						parts = new[]
						{
							new
							{
								text = $"Request:\n" +
									   $"Start Location: {request.StartLocation}\n" +
									   $"Finish Location: {request.FinishLocation}\n" +
									   $"Continuation Points: {(request.ContinuationPoints != null ? string.Join(", ", request.ContinuationPoints) : "None")}\n" +
									   $"Departure Date: {request.DepartureDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}\n" +
									   $"Duration: {request.Duration}\n\n" +
									   $"Respond Need to be in Raw Json:\n" +
									   $"Title\n" +
									   $"Description\n" +
									   $"Region\n" +
									   $"Currency\n" +
									   $"Plan(list topics will be day,destination,distance,duration,Activities"
							}
						}
					}
				}
			};

			var jsonContent = new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json");

			for (int attempt = 1; attempt <= retries; attempt++)
			{
				try
				{
					var response = await _client.PostAsync(endpoint, jsonContent);
					if (response.IsSuccessStatusCode)
					{
						return response;
					}
					else if (response.StatusCode == HttpStatusCode.InternalServerError && attempt < retries)
					{
						_logger.LogWarning("Received HTTP 500 error. Retrying... Attempt {Attempt}", attempt);
						await Task.Delay(1000);
					}
					else
					{
						_logger.LogError("HTTP request error: {StatusCode}", response.StatusCode);
						return response;
					}
				}
				catch (HttpRequestException e)
				{
					_logger.LogError(e, "HTTP request error while retrying.");
					await Task.Delay(1000);
				}
			}

			return null;
		}
	}
}
