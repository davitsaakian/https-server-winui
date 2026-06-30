using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using HttpsServerWinUI.LocalServer.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace HttpsServerWinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //separate button to upload file via app side.
            //separate button to open WebView2 to upload file from it.

            //list of uploaded files and threse buttons on it:
            //button to open file with WebView2
            //button to opem file with app.
        }

        public ObservableCollection<string> Items { get; set; }

        //Move to ViewModel as Command
        //Separate AppWindow.Id as something like IWindowsDispatcher service
        //Make files upload concurrent (linq, Task.WhenAll)
        public async void UploadFileFromApp()
        {
            var picker = new FileOpenPicker(AppWindow.Id);
            var files = await picker.PickMultipleFilesAsync();
            if (files == null)
                return;

            foreach (var file in files)
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);
                var storageManager = (Application.Current as App).Services.GetService<IServerFileStorageManager>();

                if (storageFile == null || storageManager == null)
                    continue;

                using var fileStream = await storageFile.OpenReadAsync();
                var id = Guid.NewGuid().ToString();
                await storageManager.WriteFileAsync(id, fileStream);
                Items.Add(id);
            }
        }
    }
}
