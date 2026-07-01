using Microsoft.UI;
using Microsoft.UI.Xaml;

namespace HttpsServerWinUI.Services
{
    public interface ICurrentWindowService
    {
        void SetCurrentWindow(Window window);
        WindowId WindowId { get; }
    }
}
