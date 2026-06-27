using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HttpsServer.LocalServer.Constants;
using HttpsServer.LocalServer.Core.Controllers;
using HttpsServer.LocalServer.Core.Models;
using HttpsServer.LocalServer.Core.Models.Responses;
using HttpsServer.LocalServer.Managers;
using MimeMapping;

namespace HttpsServer.LocalServer.ResourcesLocalServer.Controllers
{
    internal class StorageResourceGetController : ServerController
    {
        private readonly IServerFileStorageManager _storageManager;

        public StorageResourceGetController(IServerFileStorageManager storageManager)
        {
            _storageManager = storageManager;
        }

        public override HttpMethod Method => HttpMethod.Get;

        public override string Url => LocalServerConstants.STORAGE_ROUTE;

        public override async Task<ResponseModel> HandleRequest(RequestModel request)
        {
            var fileId = request.QueryParameters.Get(LocalServerConstants.FILE_ID_QUERY_NAME);

            if (!IsValidFileId(fileId))
                return new BadRequestResponse();

            var file = await _storageManager.ReadFileAsync(fileId).ConfigureAwait(false);
            if (file == null)
                return new NotFoundResponse();

            var mimeType = MimeUtility.GetMimeMapping(fileId);

            return new SuccessResponse
            {
                Body = file,
                Headers = new Dictionary<string, string> { { "Content-Type", mimeType } },
            };
        }
    }
}
