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
            await OnStarting(address, port);
            _cancelTokenSource = new CancellationTokenSource();
            _listener = new TcpListener(address, port);
            _listener.Start();

            await AcceptClientsAsync().ConfigureAwait(false);
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
            cts.Dispose();
            _listener.Stop();
        }

        protected abstract Task HandleClientStreamAsync(NetworkStream stream);

        private async Task AcceptClientsAsync()
        {
            while (!_cancelTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    _ = HandleClientAsync(client);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (var clientStream = client.GetStream())
                {
                    await HandleClientStreamAsync(clientStream).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TcpServer] Unhandled client error: {ex.Message}");
            }
        }
    }
}
