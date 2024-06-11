using System.Threading.Tasks;
using System.Net.Http;

namespace Gemini_AI_Api.Models
{
	public interface IGeminiService
	{
		Task<GeminiResponse> AskQuestionAsync(QuestionRequest request);
		Task<HttpResponseMessage> SendNewRequestAsync(string endpoint, QuestionRequest request, int retries);
	}
}
