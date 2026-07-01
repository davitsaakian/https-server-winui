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
        private const int BUFFER_SIZE = 8192; // 8 KB buffer

        public async Task<RequestModel> CreateRequestModel(Stream stream, CancellationToken cancellationToken = default)
        {
            var model = new RequestModel();

            var (headerString, leftoverBytes) = await ReadHeadersAsync(stream, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(headerString))
                return model;

            ParseHeaders(model, headerString);

            cancellationToken.ThrowIfCancellationRequested();

            await SetBody(model, stream, leftoverBytes, cancellationToken).ConfigureAwait(false);

            return model;
        }

        private async Task<(string Headers, byte[] Leftover)> ReadHeadersAsync(Stream stream, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            byte[] buffer = new byte[BUFFER_SIZE];

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
                if (bytesRead == 0)
                    break;

                ms.Write(buffer, 0, bytesRead);
                byte[] data = ms.GetBuffer();
                int length = (int)ms.Length;

                // Scan for the end of HTTP headers (\r\n\r\n)
                int index = -1;
                for (int i = 0; i <= length - 4; i++)
                {
                    if (data[i] == 13 && data[i + 1] == 10 && data[i + 2] == 13 && data[i + 3] == 10) // \r\n\r\n
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    // Headers found
                    string headers = Encoding.UTF8.GetString(data, 0, index);
                    int bodyStart = index + 4;
                    int leftoverLen = length - bodyStart;

                    byte[] leftover = new byte[leftoverLen];
                    if (leftoverLen > 0)
                    {
                        Array.Copy(data, bodyStart, leftover, 0, leftoverLen);
                    }

                    return (headers, leftover);
                }
            }

            return (ms.Length > 0 ? Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length) : "", Array.Empty<byte>());
        }

        private void ParseHeaders(RequestModel model, string headerString)
        {
            var lines = headerString.Split(["\r\n"], StringSplitOptions.None);
            if (lines.Length == 0)
                return;

            // Parse Request Line (Line 0)
            var requestLineParts = lines[0].Split(' ');
            if (requestLineParts.Length >= 2)
            {
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

            // Parse Headers (Lines 1+)
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var header = line.Split(':', 2);
                if (header.Length == 2)
                {
                    model.Headers.Add(header[0].Trim(), header[1].Trim());
                }
            }
        }

        private async Task SetBody(RequestModel model, Stream stream, byte[] leftoverBytes, CancellationToken cancellationToken)
        {
            if (!model.Headers.TryGetValue("Content-Length", out var contentLengthHeader))
                return;

            if (!int.TryParse(contentLengthHeader, out var contentLength) || contentLength <= 0)
                return;

            if (contentLength > MAX_BODY_SIZE)
                throw new InvalidOperationException($"Request body too large: {contentLength} bytes (max {MAX_BODY_SIZE})");

            byte[] bytesData = new byte[contentLength];
            int totalRead = 0;

            if (leftoverBytes.Length > 0)
            {
                int toCopy = Math.Min(leftoverBytes.Length, contentLength);
                Array.Copy(leftoverBytes, 0, bytesData, 0, toCopy);
                totalRead += toCopy;
            }

            while (totalRead < contentLength)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int bytesRead = await stream.ReadAsync(bytesData.AsMemory(totalRead, contentLength - totalRead), cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                    break;

                totalRead += bytesRead;
            }

            model.Body = bytesData;
        }
    }
}
