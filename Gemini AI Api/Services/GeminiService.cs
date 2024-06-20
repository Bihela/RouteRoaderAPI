using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Gemini_AI_Api.Models;
using Gemini_AI_Api.Servies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gemini_AI_Api.Servies
{
	public class GeminiService : IGeminiService
	{
		private readonly HttpClient _client;
		private readonly ILogger<GeminiService> _logger;
		private readonly IMemoryCache _cache;
		private readonly string _apiKey;
		private readonly string _endpoint;

		public GeminiService(HttpClient client, ILogger<GeminiService> logger, IMemoryCache cache, IConfiguration configuration)
		{
			_client = client;
			_logger = logger;
			_cache = cache;
			_apiKey = configuration["GeminiApi:ApiKey"];
			_endpoint = configuration["GeminiApi:Endpoint"];
		}

		public async Task<GeminiResponse> AskQuestionAsync(QuestionRequest request)
		{
			string cacheKey = $"Response_{request.StartLocation}_{request.FinishLocation}_{request.DepartureDate:yyyyMMddHHmmss}_{request.Duration}";

			if (_cache.TryGetValue(cacheKey, out GeminiResponse cachedResponse))
			{
				return cachedResponse;
			}

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
									   $"Departure Date: {request.DepartureDate:yyyy-MM-ddTHH:mm:ssZ}\n" +
									   $"Duration: {request.Duration}\n\n" +
									   $"When planning Duration need to make sure be equal to the Plan Days \n" +
									   $"Respond Need to be in Raw Json: \n" +
									   $"Title \n" +
									   $"Description \n" +
									   $"Region \n" +
									   $"Currency \n" +
									   $"Plan(list topics will be day,destination,distance,duration,Activities)"
							}
						}
					}
				}
			};

			var jsonContent = new StringContent(JsonSerializer.Serialize(requestContent), Encoding.UTF8, "application/json");

			_logger.LogInformation("Request content being sent to AI: {RequestContent}", JsonSerializer.Serialize(requestContent));

			int retries = 3;
			for (int attempt = 1; attempt <= retries; attempt++)
			{
				try
				{
					var response = await _client.PostAsync($"{_endpoint}?key={_apiKey}", jsonContent);
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
											throw new Exception("No valid response from the AI model.");
										}

										if (geminiResponse.Plan.Any(p => p.Destination == null || p.Distance == null || p.Duration == null))
										{
											_logger.LogError("Response contains null values in Plan. Sending a new request.");
											throw new Exception("Invalid response content.");
										}

										_cache.Set(cacheKey, geminiResponse, TimeSpan.FromSeconds(10));

										return geminiResponse;
									}
								}
							}
						}

						throw new Exception("No valid response from the AI model.");
					}
					else if (response.StatusCode == HttpStatusCode.InternalServerError && attempt < retries)
					{
						_logger.LogWarning("Received HTTP 500 error. Retrying... Attempt {Attempt}", attempt);
						await Task.Delay(1000);
					}
					else
					{
						throw new HttpRequestException($"HTTP request error: {response.StatusCode}");
					}
				}
				catch (Exception e)
				{
					_logger.LogError(e, "Error while processing response. Retrying... Attempt {Attempt}", attempt);
					if (attempt == retries)
					{
						var newResponse = await SendNewRequestAsync($"{_endpoint}?key={_apiKey}", request, retries);
						if (newResponse != null && newResponse.IsSuccessStatusCode)
						{
							var newResponseBody = await newResponse.Content.ReadAsStringAsync();
							_logger.LogInformation("Raw AI response from new request: {ResponseBody}", newResponseBody);

							var newJsonDoc = JsonDocument.Parse(newResponseBody);

							if (newJsonDoc.RootElement.TryGetProperty("candidates", out var newCandidates))
							{
								foreach (var candidate in newCandidates.EnumerateArray())
								{
									if (candidate.TryGetProperty("content", out var newContent))
									{
										if (newContent.TryGetProperty("parts", out var newPartsArray))
										{
											var newText = newPartsArray[0].GetProperty("text").GetString();

											var newJsonText = newText
												.Replace("```json", string.Empty)
												.Replace("```", string.Empty)
												.Trim();

											_logger.LogInformation("Cleaned JSON string from new request: {JsonText}", newJsonText);

											var newGeminiResponse = JsonSerializer.Deserialize<GeminiResponse>(newJsonText);
											if (newGeminiResponse == null)
											{
												_logger.LogError("Deserialized GeminiResponse from new request is null.");
												throw new Exception("No valid response from the AI model.");
											}

											_cache.Set(cacheKey, newGeminiResponse, TimeSpan.FromSeconds(10));

											return newGeminiResponse;
										}
									}
								}
							}
						}
					}
				}
			}

			throw new Exception("Failed to get a successful response after multiple retries.");
		}

		public async Task<HttpResponseMessage> SendNewRequestAsync(string endpoint, QuestionRequest request, int retries)
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
									   $"Departure Date: {request.DepartureDate:yyyy-MM-ddTHH:mm:ssZ}\n" +
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

			// Log the request content for the new request
			_logger.LogInformation("New request content being sent to AI: {RequestContent}", JsonSerializer.Serialize(requestContent));

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
