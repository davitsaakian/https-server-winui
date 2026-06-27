using System.Threading.Tasks;
using HttpsServer.LocalServer.Core.Models.Responses;

namespace HttpsServer.LocalServer.Core.Middleware
{
    internal class ContentLengthHeaderMiddleware : IServerResponseMiddleware
    {
        private const string HEADER_NAME = "Content-Length";
        private const string EMPTY_CONTENT_LENGTH = "0";

        public Task HandleResponse(ResponseModel response)
        {
            if (!response.Headers.ContainsKey(HEADER_NAME))
            {
                response.Headers[HEADER_NAME] = response.Body?.Length.ToString() ?? EMPTY_CONTENT_LENGTH;
            }

            return Task.CompletedTask;
        }
    }
}
