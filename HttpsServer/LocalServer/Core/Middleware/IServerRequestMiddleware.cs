using System.Threading.Tasks;
using HttpsServer.LocalServer.Core.Models;

namespace HttpsServer.LocalServer.Core.Middleware
{
    public interface IServerRequestMiddleware
    {
        Task HandleRequest(RequestModel request);
    }
}
