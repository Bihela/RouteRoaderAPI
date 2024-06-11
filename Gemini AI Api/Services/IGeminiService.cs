using System.Threading.Tasks;
using System.Net.Http;
using Gemini_AI_Api.Models;

namespace Gemini_AI_Api.Servies;

public interface IGeminiService
{
    Task<GeminiResponse> AskQuestionAsync(QuestionRequest request);
    Task<HttpResponseMessage> SendNewRequestAsync(string endpoint, QuestionRequest request, int retries);
}
