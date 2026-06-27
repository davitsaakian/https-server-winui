using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HttpMultipartParser;
using HttpsServerWinUI.LocalServer.Constants;
using HttpsServerWinUI.LocalServer.Core.Controllers;
using HttpsServerWinUI.LocalServer.Core.Models;
using HttpsServerWinUI.LocalServer.Core.Models.Responses;
using HttpsServerWinUI.LocalServer.Managers;

namespace HttpsServerWinUI.LocalServer.ResourcesLocalServer.Controllers
{
    internal class StorageResourceMultipartDataPostController : ServerController
    {
        private const string CONTENT_TYPE = "multipart/form-data";

        private readonly IServerFileStorageManager _storageManager;

        public StorageResourceMultipartDataPostController(IServerFileStorageManager storageManager)
        {
            _storageManager = storageManager;
        }

        public override HttpMethod Method => HttpMethod.Post;

        public override string Url => LocalServerConstants.STORAGE_ROUTE;

        public override async Task<ResponseModel> HandleRequest(RequestModel request)
        {
            if (!request.Headers.TryGetValue("Content-Type", out var contentTypeHeader) || !contentTypeHeader.StartsWith(CONTENT_TYPE))
                return new ResponseModel { StatusCode = 415, StatusMessage = "Unsupported Media Type" };

            MultipartFormDataParser multipartParser;

            using (var stream = new MemoryStream(request.Body))
            {
                multipartParser = await MultipartFormDataParser.ParseAsync(stream).ConfigureAwait(false);
            }

            foreach (var file in multipartParser.Files)
            {
                if (!IsValidFileId(file.FileName))
                    continue;

                var length = (int)file.Data.Length;
                var fileBytes = new byte[length];
                int totalRead = 0;

                while (totalRead < length)
                {
                    int bytesRead = await file.Data.ReadAsync(fileBytes, totalRead, length - totalRead).ConfigureAwait(false);
                    if (bytesRead == 0)
                        break;
                    totalRead += bytesRead;
                }

                await _storageManager.WriteFileAsync(file.FileName, fileBytes).ConfigureAwait(false);
            }

            return new SuccessResponse();
        }
    }
}
