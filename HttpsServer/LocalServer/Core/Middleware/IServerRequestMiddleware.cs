using System.Threading.Tasks;
using HttpsServerWinUI.LocalServer.Core.Models;

namespace HttpsServerWinUI.LocalServer.Core.Middleware
{
    public interface IServerRequestMiddleware
    {
        Task HandleRequest(RequestModel request);
    }
}
