using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Gemini_AI_Api.Controllers;
using Gemini_AI_Api.Models;
using Gemini_AI_Api.Servies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;

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
		public async Task AskQuestion_NullRequest_ReturnsBadRequest()
		{
			QuestionRequest request = null;

			var result = await _controller.AskQuestion(request) as BadRequestObjectResult;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.StatusCode, Is.EqualTo(400));
			StringAssert.Contains("Invalid request. Request cannot be null.", result.Value.ToString());
		}



		[Test]
		public async Task AskQuestion_HttpRequestError_ReturnsBadRequest()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.UtcNow.AddDays(1),
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
		public async Task AskQuestion_JsonParsingError_ReturnsInternalServerError()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.UtcNow.AddDays(1),
				Duration = 1
			};

			_geminiServiceMock.Setup(s => s.AskQuestionAsync(request))
				.ThrowsAsync(new JsonException("Invalid JSON"));

			var result = await _controller.AskQuestion(request) as ObjectResult;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.StatusCode, Is.EqualTo(500));
			StringAssert.Contains("JSON parsing error:", result.Value.ToString());
		}

		[Test]
		public async Task AskQuestion_UnexpectedException_ReturnsInternalServerError()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.UtcNow.AddDays(1),
				Duration = 1
			};

			_geminiServiceMock.Setup(s => s.AskQuestionAsync(request))
				.ThrowsAsync(new Exception("Unexpected error"));

			var result = await _controller.AskQuestion(request) as ObjectResult;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.StatusCode, Is.EqualTo(500));
			StringAssert.Contains("Exception error:", result.Value.ToString());
		}

		[Test]
		public async Task AskQuestion_ValidRequest_ReturnsOk()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.UtcNow.AddDays(1),
				Duration = 1
			};

			var response = new GeminiResponse
			{
				Title = "Response Title",
				Description = "Response Description",
				Region = "Response Region",
				Currency = "Response Currency",
				Plan = new List<Plan>()
			};

			_geminiServiceMock.Setup(s => s.AskQuestionAsync(request))
				.ReturnsAsync(response);

			var result = await _controller.AskQuestion(request) as OkObjectResult;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.StatusCode, Is.EqualTo(200));
			Assert.That(result.Value, Is.EqualTo(response));
		}

		[Test]
		public async Task AskQuestion_ValidRequest_LoggerCalled()
		{
			var request = new QuestionRequest
			{
				StartLocation = "A",
				FinishLocation = "B",
				DepartureDate = DateTime.UtcNow.AddDays(1),
				Duration = 1
			};

			var response = new GeminiResponse
			{
				Title = "Response Title",
				Description = "Response Description",
				Region = "Response Region",
				Currency = "Response Currency",
				Plan = new List<Plan>()
			};

			_geminiServiceMock.Setup(s => s.AskQuestionAsync(request))
				.ReturnsAsync(response);

			await _controller.AskQuestion(request);

			_loggerMock.Verify(l => l.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Question asked successfully")),
				null,
				It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		}
	}
}
