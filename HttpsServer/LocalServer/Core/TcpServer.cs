using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HttpsServerWinUI.LocalServer.Core
{
    public abstract class TcpServer
    {
        private TcpListener _listener;
        private CancellationTokenSource _cancelTokenSource;

        public async Task Start(IPAddress address, int port)
        {
            await OnStarting(address, port).ConfigureAwait(false);

            _cancelTokenSource = new CancellationTokenSource();
            _listener = new TcpListener(address, port);
            _listener.Start();

            _ = AcceptClientsAsync(_cancelTokenSource.Token);
        }

        public virtual Task OnStarting(IPAddress address, int port)
        {
            return Task.CompletedTask;
        }

        public void Stop()
        {
            var cts = _cancelTokenSource;
            if (cts == null || cts.IsCancellationRequested)
                return;

            cts.Cancel();

            try
            {
                _listener?.Stop();
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }

            cts.Dispose();
            _cancelTokenSource = null;
        }

        protected abstract Task HandleClientStreamAsync(NetworkStream stream, CancellationToken cancellationToken);

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _ = HandleClientAsync(client, cancellationToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using (client)
                using (var clientStream = client.GetStream())
                {
                    await HandleClientStreamAsync(clientStream, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TcpServer] Unhandled client error: {ex.Message}");
            }
        }
    }
}
