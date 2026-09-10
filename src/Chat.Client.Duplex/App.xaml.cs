using System.Diagnostics;
using System.Windows;
namespace Chat.Client.Duplex {
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
        }
    }
}
