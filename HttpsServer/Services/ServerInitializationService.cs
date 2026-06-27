using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using HttpsServerWinUI.LocalServer.Constants;
using HttpsServerWinUI.LocalServer.Core;
using HttpsServerWinUI.LocalServer.Core.Managers;
using HttpsServerWinUI.LocalServer.Core.Middleware;
using HttpsServerWinUI.LocalServer.Managers;
using HttpsServerWinUI.LocalServer.ResourcesLocalServer.Controllers;
using Windows.Storage;

namespace HttpsServerWinUI.Services
{
    internal class ServerInitializationService : IServerInitializationService
    {
        private readonly HttpsServer _resourcesServer;
        private readonly IServerFileStorageManager _tempFileStorageManager;

        public ServerInitializationService(
            HttpsServer httpsServer,
            IServerFileStorageManager tempFileStorageManager,
            CorsMiddleware corsMiddleware,
            PreflightController preflightController,
            StorageResourceGetController storageResourceGetController,
            StorageResourceMultipartDataPostController storageResourceMultipartDataPostController
        )
        {
            _resourcesServer = httpsServer;

            _resourcesServer.AddMiddleware(corsMiddleware);

            _resourcesServer.AddController(storageResourceMultipartDataPostController);
            _resourcesServer.AddController(storageResourceGetController);
            _resourcesServer.AddController(preflightController);

            _tempFileStorageManager = tempFileStorageManager;
        }

        public async Task Initialize(StorageFile httpsCertificateFile, string httpsCertificatePassword)
        {
            var certificateArray = (await FileIO.ReadBufferAsync(httpsCertificateFile)).ToArray();
            var httpsCertificate = new X509Certificate2(certificateArray, httpsCertificatePassword, X509KeyStorageFlags.UserKeySet);

            await StartServer(httpsCertificate);
        }

        private async Task StartServer(X509Certificate2 httpsCertificate)
        {
            var serverAddress = LocalServerConstants.LOCAL_ADDRESS;
            var serverPort = LocalServerConstants.PORT;

            HttpsCertificateManager.Instance.AddCertificate(serverAddress, httpsCertificate);
            await _tempFileStorageManager.DeleteAllFiles();

            _ = _resourcesServer.Start(serverAddress, serverPort);
        }
    }
}
