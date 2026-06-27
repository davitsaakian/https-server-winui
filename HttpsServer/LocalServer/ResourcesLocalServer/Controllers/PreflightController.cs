using System.Net.Http;
using System.Threading.Tasks;
using HttpsServerWinUI.LocalServer.Core.Controllers;
using HttpsServerWinUI.LocalServer.Core.Models;
using HttpsServerWinUI.LocalServer.Core.Models.Responses;

namespace HttpsServerWinUI.LocalServer.ResourcesLocalServer.Controllers
{
    internal class PreflightController : ServerController
    {
        public override HttpMethod Method => HttpMethod.Options;

        public override string Url => "/";

        public override Task<ResponseModel> HandleRequest(RequestModel request)
        {
            return Task.FromResult<ResponseModel>(new SuccessResponse());
        }
    }
}
