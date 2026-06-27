using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HttpsServer.LocalServer.Core.Controllers;
using HttpsServer.LocalServer.Core.Managers;
using HttpsServer.LocalServer.Core.Middleware;
using HttpsServer.LocalServer.Core.Models;
using HttpsServer.LocalServer.Core.Models.Responses;
using HttpsServer.LocalServer.Core.Services;

namespace HttpsServer.LocalServer.Core
{
    public class HttpsServer : TcpServer
    {
        private X509Certificate2 _certificate;
        private string _baseUrl;

        private readonly List<ServerController> _controllers;
        private readonly List<IServerRequestMiddleware> _requestMiddleware;
        private readonly List<IServerResponseMiddleware> _responseMiddleware;

        private readonly ResponseStringCreator _responseStringCreator;
        private readonly RequestModelCreator _requestModelCreator;

        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

        public HttpsServer()
        {
            _responseStringCreator = new ResponseStringCreator();
            _requestModelCreator = new RequestModelCreator();

            _requestMiddleware = [];
            _responseMiddleware = [new DateHeaderMiddleware(), new ContentLengthHeaderMiddleware()];

            _controllers = [];
        }

        public override Task OnStarting(IPAddress address, int port)
        {
            _certificate =
                HttpsCertificateManager.Instance.GetCertificate(address)
                ?? throw new ArgumentException($"Please ensure that a certificate is added to the HttpsCertificateManager for the address: {address}");
            _baseUrl = $"https://{address}:{port}";
            return Task.CompletedTask;
        }

        public void AddController(ServerController controller)
        {
            _controllers.Add(controller);
        }

        public void AddMiddleware(IServerRequestMiddleware middleware)
        {
            _requestMiddleware.Insert(0, middleware);
        }

        public void AddMiddleware(IServerResponseMiddleware middleware)
        {
            _responseMiddleware.Insert(0, middleware);
        }

        protected override async Task HandleClientStreamAsync(NetworkStream stream)
        {
            using var cts = new CancellationTokenSource(RequestTimeout);
            using var sslStream = new SslStream(stream, false);
            try
            {
                await AuthenticateSslStream(sslStream).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[HttpsServer] SSL handshake timed out");
                return;
            }
            catch (AuthenticationException ex)
            {
                Debug.WriteLine($"[HttpsServer] SSL authentication failed: {ex.InnerException?.Message ?? ex.Message}");
                return;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[HttpsServer] Connection lost during SSL handshake: {ex.Message}");
                return;
            }

            ResponseModel responseModel;
            try
            {
                var requestModel = await _requestModelCreator.CreateRequestModel(sslStream, cts.Token).ConfigureAwait(false);

                if (requestModel.Method != null)
                    Debug.WriteLine($"[HttpsServer] {requestModel.Method} {requestModel.GetFullUrl(_baseUrl)}");

                foreach (var middleware in _requestMiddleware)
                    await middleware.HandleRequest(requestModel).ConfigureAwait(false);

                responseModel = await HandleRequest(requestModel).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[HttpsServer] Request processing timed out");
                responseModel = new ServerErrorResponse();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HttpsServer] Request processing failed: {ex.Message}");
                responseModel = new ServerErrorResponse();
            }

            foreach (var middleware in _responseMiddleware)
                await middleware.HandleResponse(responseModel).ConfigureAwait(false);

            await SendResponse(sslStream, responseModel).ConfigureAwait(false);
        }

        private Task<ResponseModel> HandleRequest(RequestModel requestModel)
        {
            if (requestModel.Method == null)
                return Task.FromResult<ResponseModel>(new BadRequestResponse());

            var controller = FindController(requestModel);
            if (controller == null)
                return Task.FromResult<ResponseModel>(new NotFoundResponse());

            return controller.HandleRequest(requestModel);
        }

        private ServerController? FindController(RequestModel requestModel)
        {
            return _controllers.Find(controller =>
            {
                var methodMatches = controller.Method == requestModel.Method;

                var urlMatches = requestModel.LocalUrl?.StartsWith(controller.Url, StringComparison.OrdinalIgnoreCase) ?? false;

                return methodMatches && urlMatches;
            });
        }

        private async Task SendResponse(SslStream stream, ResponseModel response)
        {
            try
            {
                var responseBytes = _responseStringCreator.CreateResponseBytes(response);
                await stream.WriteAsync(responseBytes).ConfigureAwait(false);
            }
            catch (AuthenticationException ex)
            {
                Debug.WriteLine($"[HttpsServer] SSL error during response: {ex.Message}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[HttpsServer] Client disconnected during response: {ex.Message}");
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"[HttpsServer] Socket error during response: {ex.Message}");
            }
        }

        private Task AuthenticateSslStream(SslStream stream)
        {
            return stream.AuthenticateAsServerAsync(_certificate, clientCertificateRequired: false, enabledSslProtocols: SslProtocols.Tls12, checkCertificateRevocation: false);
        }
    }
}
