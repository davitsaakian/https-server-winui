using System;
using System.Threading.Tasks;
using HttpsServerWinUI.LocalServer.Core.Models.Responses;

namespace HttpsServerWinUI.LocalServer.Core.Middleware
{
    internal class DateHeaderMiddleware : IServerResponseMiddleware
    {
        private const string HEADER_NAME = "Date";
        private const string DATE_FORMAT = "R";

        public Task HandleResponse(ResponseModel response)
        {
            if (!response.Headers.ContainsKey(HEADER_NAME))
            {
                response.Headers[HEADER_NAME] = DateTime.UtcNow.ToString(DATE_FORMAT);
            }

            return Task.CompletedTask;
        }
    }
}
