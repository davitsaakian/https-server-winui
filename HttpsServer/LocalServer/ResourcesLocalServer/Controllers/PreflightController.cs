using System.Net.Http;
using System.Threading.Tasks;
using HttpsServer.LocalServer.Core.Controllers;
using HttpsServer.LocalServer.Core.Models;
using HttpsServer.LocalServer.Core.Models.Responses;

namespace HttpsServer.LocalServer.ResourcesLocalServer.Controllers
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
