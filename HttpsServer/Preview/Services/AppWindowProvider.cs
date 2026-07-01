using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace HttpsServerWinUI.Preview.Services
{
    public class AppWindowProvider : IAppWindowProvider
    {
        public AppWindow GetAppWindow()
        {
            return null;
            //return WindowsHelper Application.Current as App;
        }
    }
}
