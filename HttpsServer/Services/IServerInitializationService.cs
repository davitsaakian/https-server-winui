using System.Threading.Tasks;
using Windows.Storage;

namespace HttpsServerWinUI.Services
{
    public interface IServerInitializationService
    {
        Task Initialize(StorageFile httpsCertificateFile, string httpsCertificatePassword);
    }
}
