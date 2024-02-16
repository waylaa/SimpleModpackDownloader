using Serilog;
using SimpleModpackDownloader.ViewModels;
using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace SimpleModpackDownloader.Views;

/// <summary>
/// Interaction logic for MainView.xaml
/// </summary>
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = new MainViewViewModel();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(x => x.File(Path.GetFullPath("log.txt", App.LogsDirectory), rollingInterval: RollingInterval.Day))
            .WriteTo.Async(x => x.RichTextBox(Logger, formatProvider: CultureInfo.InvariantCulture))
            .CreateLogger();
    }
}
