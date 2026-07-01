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
using HttpsServerWinUI.LocalServer.Core.Controllers;
using HttpsServerWinUI.LocalServer.Core.Managers;
using HttpsServerWinUI.LocalServer.Core.Middleware;
using HttpsServerWinUI.LocalServer.Core.Models;
using HttpsServerWinUI.LocalServer.Core.Models.Responses;
using HttpsServerWinUI.LocalServer.Core.Services;

namespace HttpsServerWinUI.LocalServer.Core
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

        protected override async Task HandleClientStreamAsync(NetworkStream stream, CancellationToken serverCancellationToken)
        {
            using var timeoutCts = new CancellationTokenSource(RequestTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken, timeoutCts.Token);

            using var sslStream = new SslStream(stream, false);

            try
            {
                var sslOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = _certificate,
                    ClientCertificateRequired = false,
                    EnabledSslProtocols = SslProtocols.Tls12,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                };

                await sslStream.AuthenticateAsServerAsync(sslOptions, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[HttpsServer] SSL handshake timed out or server shutting down.");
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

            // HTTP Keep-Alive Loop
            while (!linkedCts.Token.IsCancellationRequested)
            {
                timeoutCts.CancelAfter(RequestTimeout);

                ResponseModel responseModel;
                bool keepAlive = false;

                try
                {
                    var requestModel = await _requestModelCreator.CreateRequestModel(sslStream, linkedCts.Token).ConfigureAwait(false);

                    if (requestModel.Method == null)
                        break;

                    Debug.WriteLine($"[HttpsServer] {requestModel.Method} {requestModel.GetFullUrl(_baseUrl)}");

                    if (requestModel.Headers.TryGetValue("Connection", out var connHeader))
                    {
                        keepAlive = connHeader.Equals("keep-alive", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        keepAlive = true;
                    }

                    foreach (var middleware in _requestMiddleware)
                        await middleware.HandleRequest(requestModel).ConfigureAwait(false);

                    responseModel = await HandleRequest(requestModel).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("[HttpsServer] Request processing timed out or server shutting down.");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HttpsServer] Request processing failed: {ex.Message}");
                    responseModel = new ServerErrorResponse();
                    keepAlive = false;
                }

                responseModel.Headers ??= [];
                responseModel.Headers["Connection"] = keepAlive ? "keep-alive" : "close";

                foreach (var middleware in _responseMiddleware)
                    await middleware.HandleResponse(responseModel).ConfigureAwait(false);

                await SendResponse(sslStream, responseModel).ConfigureAwait(false);

                if (!keepAlive)
                    break;
            }
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
    }
}
