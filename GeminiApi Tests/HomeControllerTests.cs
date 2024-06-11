using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Gemini_AI_Api.Controllers;
using Gemini_AI_Api.Models;
using Gemini_AI_Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Text.Json;


namespace Gemini_AI_Api.Tests
{
	public class HomeControllerTests
	{
		private Mock<IGeminiService> _geminiServiceMock;
		private Mock<ILogger<HomeController>> _loggerMock;
		private HomeController _controller;

		[SetUp]
		public void Setup()
		{
			_geminiServiceMock = new Mock<IGeminiService>();
			_loggerMock = new Mock<ILogger<HomeController>>();

			_controller = new HomeController(_geminiServiceMock.Object, _loggerMock.Object);
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

			_geminiServiceMock.Setup(s => s.AskQuestionAsync(request))
				.ThrowsAsync(new HttpRequestException("BadRequest"));

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

			var cachedResponse = new GeminiResponse
			{
				Title = "Cached Title",
				Description = "Cached Description",
				Region = "Cached Region",
				Currency = "Cached Currency",
				Plan = new List<Plan>()
			};

			_geminiServiceMock.Setup(s => s.AskQuestionAsync(request))
				.ReturnsAsync(cachedResponse);

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

			_geminiServiceMock.Setup(s => s.AskQuestionAsync(request))
				.ThrowsAsync(new JsonException("Invalid JSON"));

			var result = await _controller.AskQuestion(request) as ObjectResult;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.StatusCode, Is.EqualTo(500));

			StringAssert.Contains("JSON parsing error:", result.Value.ToString());
		}
	}
}
