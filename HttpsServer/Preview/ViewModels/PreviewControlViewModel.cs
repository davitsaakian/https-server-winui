using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HttpsServerWinUI.LocalServer.Managers;
using HttpsServerWinUI.Services;
using Microsoft.Windows.Storage.Pickers;
using Windows.Storage;

namespace HttpsServerWinUI.Preview.ViewModels
{
    public partial class PreviewControlViewModel : IDisposable
    {
        private readonly IServerFileStorageManager _serverFileStorageManager;
        private readonly WebViewControlViewModel _webViewControlViewModel;
        private readonly ICurrentWindowService _windowService;

        public PreviewControlViewModel(IServerFileStorageManager serverFileStorageManager, ICurrentWindowService windowService, WebViewControlViewModel webViewControlViewModel)
        {
            _serverFileStorageManager = serverFileStorageManager;
            _webViewControlViewModel = webViewControlViewModel;
            _windowService = windowService;
            StrongReferenceMessenger.Default.Register<object, string, string>(this, "WebMessageReceived", WebMessageReceived);
        }

        private void WebMessageReceived(object recipient, string message)
        {
            Files.Add(new FileInfo(message, GetFilePath(message)));
        }

        public ObservableCollection<FileInfo> Files { get; } = [];

        public string GetFilePath(string id)
        {
            return Path.Combine(_serverFileStorageManager.GetFolderPath(), id);
        }

        public void Dispose()
        {
            StrongReferenceMessenger.Default.Unregister<object, string>(this, "WebMessageReceived");
        }

        //Make files upload concurrent (linq, Task.WhenAll)
        [RelayCommand]
        public async Task UploadFileFromApp()
        {
            var picker = new FileOpenPicker(_windowService.WindowId);
            var files = await picker.PickMultipleFilesAsync();
            if (files == null)
                return;

            foreach (var file in files)
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);

                if (storageFile == null)
                    continue;

                using var fileStream = await storageFile.OpenReadAsync();
                var id = Guid.NewGuid().ToString();
                await _serverFileStorageManager.WriteFileAsync(id, fileStream);
                Files.Add(new FileInfo(id, GetFilePath(id)));
            }
        }

        [RelayCommand]
        public Task UploadFileFromWebView()
        {
            return _webViewControlViewModel.OpenUploadPage();
        }

        public void WebViewMessageReceived(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
                Files.Add(new FileInfo(id, GetFilePath(id)));
        }
    }
}
