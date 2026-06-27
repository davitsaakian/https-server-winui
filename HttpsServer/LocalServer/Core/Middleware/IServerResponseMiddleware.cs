using System.Threading.Tasks;
using HttpsServer.LocalServer.Core.Models.Responses;

namespace HttpsServer.LocalServer.Core.Middleware
{
    public interface IServerResponseMiddleware
    {
        Task HandleResponse(ResponseModel response);
    }
}
