using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HttpsServer.LocalServer.Core.Models;
using HttpsServer.LocalServer.Core.Models.Responses;

namespace HttpsServer.LocalServer.Core.Controllers
{
    public abstract class ServerController
    {
        public abstract HttpMethod Method { get; }
        public abstract string Url { get; }
        public abstract Task<ResponseModel> HandleRequest(RequestModel request);

        protected bool IsValidFileId(string fileId)
        {
            return !string.IsNullOrEmpty(fileId) && fileId.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !fileId.Contains("..");
        }
    }
}
