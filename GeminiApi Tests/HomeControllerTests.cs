using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Gemini_AI_Api.Controllers;
using Gemini_AI_Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Gemini_AI_Api.Tests
{
	public class HomeControllerTests
	{
		private Mock<HttpMessageHandler> _httpMessageHandlerMock;
		private Mock<ILogger<HomeController>> _loggerMock;
		private Mock<IMemoryCache> _cacheMock;
		private HomeController _controller;

		[SetUp]
		public void Setup()
		{
			_httpMessageHandlerMock = new Mock<HttpMessageHandler>();
			var httpClient = new HttpClient(_httpMessageHandlerMock.Object);

			_loggerMock = new Mock<ILogger<HomeController>>();
			_cacheMock = new Mock<IMemoryCache>();

			_controller = new HomeController(httpClient, _loggerMock.Object, _cacheMock.Object);
		}

		[Test]
		public async Task AskQuestion_InvalidRequest_ReturnsBadRequest()
		{
			var request = new QuestionRequest
			{
				StartLocation = "",
				FinishLocation = ""
			};

			var result = await _controller.AskQuestion(request) as BadRequestObjectResult;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.StatusCode, Is.EqualTo(400));
			Assert.That(result.Value, Is.EqualTo("Invalid request. StartLocation and FinishLocation cannot be null or empty."));
		}

		[Test]
		public async Task AskQuestion_HttpRequestError_ReturnsBadRequest()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.Now,
				Duration = 1
			};

			var cacheKey = $"Response_A_B_{request.DepartureDate:yyyyMMddHHmmss}_{request.Duration}";

			object cacheEntry = null;
			_cacheMock.Setup(c => c.TryGetValue(cacheKey, out cacheEntry)).Returns(false);

			var responseMessage = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.BadRequest
			};

			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>()
				)
				.ReturnsAsync(responseMessage);

			var result = await _controller.AskQuestion(request) as BadRequestObjectResult;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.StatusCode, Is.EqualTo(400));
			Assert.That(result.Value, Is.EqualTo("HTTP request error: BadRequest"));
		}

		[Test]
		public async Task AskQuestion_ValidRequest_CachedResponse_ReturnsOk()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.Now,
				Duration = 1
			};

			var cacheKey = $"Response_A_B_{request.DepartureDate:yyyyMMddHHmmss}_{request.Duration}";

			var cachedResponse = new GeminiResponse
			{
				Title = "Cached Title",
				Description = "Cached Description",
				Region = "Cached Region",
				Currency = "Cached Currency",
				Plan = new List<Plan>()
			};

			object cacheEntry = cachedResponse;
			_cacheMock.Setup(c => c.TryGetValue(cacheKey, out cacheEntry)).Returns(true);

			var result = await _controller.AskQuestion(request) as OkObjectResult;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.StatusCode, Is.EqualTo(200));
			Assert.That(result.Value, Is.EqualTo(cachedResponse));
		}

		[Test]
		public async Task AskQuestion_JsonParsingError_ReturnsInternalServerError()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.Now,
				Duration = 1
			};

			var cacheKey = $"Response_A_B_{request.DepartureDate:yyyyMMddHHmmss}_{request.Duration}";

			object cacheEntry = null;
			_cacheMock.Setup(c => c.TryGetValue(cacheKey, out cacheEntry)).Returns(false);

			var responseMessage = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent("Invalid JSON")
			};

			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>()
				)
				.ReturnsAsync(responseMessage);

			var result = await _controller.AskQuestion(request) as ObjectResult;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.StatusCode, Is.EqualTo(500));

			StringAssert.Contains("JSON parsing error:", result.Value.ToString());
		}

	}
}
