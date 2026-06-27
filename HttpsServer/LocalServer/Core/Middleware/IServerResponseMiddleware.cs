using System.Threading.Tasks;
using HttpsServerWinUI.LocalServer.Core.Models.Responses;

namespace HttpsServerWinUI.LocalServer.Core.Middleware
{
    public interface IServerResponseMiddleware
    {
        Task HandleResponse(ResponseModel response);
    }
}
