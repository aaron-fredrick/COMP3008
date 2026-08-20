using System.Windows;
using Chat.Client.Shared.Converters;

namespace Chat.Client.Polling
{
    public partial class App : Application
    {
        public App()
        {
            Resources.Add("FileSizeConverter", new FileSizeConverter());
        }
    }
}
