using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HttpsServerWinUI.LocalServer.Managers
{
    internal class ServerFileStorageManager : IServerFileStorageManager
    {
        private const string SUBFOLDER_NAME = "ServerResources";

        private readonly StorageFolder _storageFolder = ApplicationData.Current.TemporaryFolder;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

        public async Task DeleteAllFiles()
        {
            var storageItem = await _storageFolder.TryGetItemAsync(SUBFOLDER_NAME);

            if (storageItem is StorageFolder subFolder)
                await subFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
        }

        public async Task WriteFileAsync(string id, IRandomAccessStream stream)
        {
            using var dataReader = new DataReader(stream.GetInputStreamAt(0));
            var size = (uint)stream.Size;
            await dataReader.LoadAsync(size);
            var bytes = new byte[size];
            dataReader.ReadBytes(bytes);
            await WriteFileAsync(id, bytes).ConfigureAwait(false);
        }

        public async Task WriteFileAsync(string id, byte[] bytes)
        {
            var semaphore = _semaphores.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var subFolder = await OpenOrCreateSubFolder();
                var file = await subFolder.CreateFileAsync(id, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(file, bytes);
            }
            finally
            {
                semaphore.Release();
                if (semaphore.CurrentCount == 1)
                    _semaphores.TryRemove(id, out _);
            }
        }

        public async Task<byte[]?> ReadFileAsync(string id)
        {
            var file = await GetStorageFile(id);
            if (file == null)
                return null;
            var buffer = await FileIO.ReadBufferAsync(file);
            return buffer.ToArray();
        }

        public string GetFolderPath()
        {
            return Path.Combine(_storageFolder.Path, SUBFOLDER_NAME);
        }

        private async Task<IStorageFile?> GetStorageFile(string id)
        {
            var subFolder = await OpenOrCreateSubFolder();
            var item = await subFolder.TryGetItemAsync(id);
            return item as IStorageFile;
        }

        private IAsyncOperation<StorageFolder> OpenOrCreateSubFolder()
        {
            return _storageFolder.CreateFolderAsync(SUBFOLDER_NAME, CreationCollisionOption.OpenIfExists);
        }
    }
}
