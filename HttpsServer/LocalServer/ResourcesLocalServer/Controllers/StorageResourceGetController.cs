using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HttpsServerWinUI.LocalServer.Constants;
using HttpsServerWinUI.LocalServer.Core.Controllers;
using HttpsServerWinUI.LocalServer.Core.Models;
using HttpsServerWinUI.LocalServer.Core.Models.Responses;
using HttpsServerWinUI.LocalServer.Managers;
using MimeMapping;

namespace HttpsServerWinUI.LocalServer.ResourcesLocalServer.Controllers
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
