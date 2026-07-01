using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace HttpsServerWinUI.Services
{
    public class CurrentWindowService : ICurrentWindowService
    {
        private AppWindow _appWindow;
        public WindowId WindowId => _appWindow.Id;

        public void SetCurrentWindow(Window window)
        {
            _appWindow = window.AppWindow;
        }
    }
}
