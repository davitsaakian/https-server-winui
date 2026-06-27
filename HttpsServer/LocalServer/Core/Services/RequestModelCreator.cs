using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HttpsServerWinUI.LocalServer.Core.Models;

namespace HttpsServerWinUI.LocalServer.Core.Services
{
    internal class RequestModelCreator
    {
        private const int MAX_BODY_SIZE = 500 * 1024 * 1024; // 500 MB

        public async Task<RequestModel> CreateRequestModel(Stream stream, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            var model = new RequestModel();

            await SetMainRequestInfo(model, reader).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await SetHeaders(model, reader).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await SetBody(model, stream, cancellationToken).ConfigureAwait(false);

            return model;
        }

        private async Task SetMainRequestInfo(RequestModel model, StreamReader reader)
        {
            var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(requestLine))
                return;

            var requestLineParts = requestLine.Split(' ');

            if (requestLineParts.Length < 2)
                return;

            var method = requestLineParts[0];
            var url = requestLineParts[1];

            string path = url;
            string? query = null;

            var urlParts = url.Split('?', 2);
            if (urlParts.Length == 2)
            {
                path = urlParts[0];
                query = urlParts[1];
            }

            var queryParameters = string.IsNullOrEmpty(query) ? [] : HttpUtility.ParseQueryString(query);

            model.Method = new HttpMethod(method);
            model.LocalUrl = path;
            model.QueryParameters = queryParameters;
        }

        private async Task SetHeaders(RequestModel model, StreamReader reader)
        {
            string line;
            while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync().ConfigureAwait(false)))
            {
                var header = line.Split(':', 2);
                if (header.Length != 2)
                    continue;
                var name = header[0].Trim();
                var value = header[1].Trim();

                model.Headers.Add(name, value);
            }
        }

        private async Task SetBody(RequestModel model, Stream stream, CancellationToken cancellationToken = default)
        {
            if (!model.Headers.TryGetValue("Content-Length", out var contentLengthHeader))
                return;

            if (!int.TryParse(contentLengthHeader, out var contentLength) || contentLength <= 0)
                return;

            if (contentLength > MAX_BODY_SIZE)
                throw new InvalidOperationException($"Request body too large: {contentLength} bytes (max {MAX_BODY_SIZE})");

            byte[] bytesData = new byte[contentLength];
            int totalRead = 0;

            while (totalRead < contentLength)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int bytesRead = await stream.ReadAsync(bytesData, totalRead, contentLength - totalRead).ConfigureAwait(false);
                if (bytesRead == 0)
                    break;

                totalRead += bytesRead;
            }

            model.Body = bytesData;
        }
    }
}
