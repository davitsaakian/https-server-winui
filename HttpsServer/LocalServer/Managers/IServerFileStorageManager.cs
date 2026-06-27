using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace HttpsServer.LocalServer.Managers
{
    public interface IServerFileStorageManager
    {
        Task DeleteAllFiles();
        Task WriteFileAsync(string id, IRandomAccessStream stream);
        Task WriteFileAsync(string id, byte[] bytes);
        Task<byte[]?> ReadFileAsync(string id);
        Task<string?> GetFilePath(string id);
    }
}
