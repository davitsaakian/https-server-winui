using HttpsServerWinUI.LocalServer.Core.Models.Responses;
using System.Threading.Tasks;

namespace HttpsServerWinUI.LocalServer.Core.Middleware
{
    internal class CorsMiddleware : IServerResponseMiddleware
    {
        private readonly string[] _allowedOrigins = { "*" };
        private readonly string[] _allowedMethods = { "GET", "POST", "OPTIONS" };
        private readonly string[] _allowedHeaders = { "*" };

        public Task HandleResponse(ResponseModel response)
        {
            if (response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                return Task.CompletedTask;

            response.Headers["Access-Control-Allow-Origin"] = string.Join(", ", _allowedOrigins);
            response.Headers["Access-Control-Allow-Methods"] = string.Join(", ", _allowedMethods);
            response.Headers["Access-Control-Allow-Headers"] = string.Join(", ", _allowedHeaders);
            response.Headers["Access-Control-Allow-Credentials"] = "true";

            return Task.CompletedTask;
        }
    }
}
