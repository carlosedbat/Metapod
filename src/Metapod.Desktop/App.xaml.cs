using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Metapod.Desktop;

public partial class App : Application
{
    private bool reporting;
    protected override void OnStartup(StartupEventArgs e)
    {
        // Em culturas com vírgula decimal (ex.: pt-BR), o parser de DataTemplate/ControlTemplate em
        // tempo de execução usa a cultura da thread para converter Thickness e corrompe valores como "0,18,0,0".
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.InvariantCulture.IetfLanguageTag)));
        base.OnStartup(e);
        DispatcherUnhandledException += (_, error) => { error.Handled=true; Report(error.Exception); };
        try
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception error) { Report(error); }
    }
    private void Report(Exception error)
    {
        if(reporting) { Shutdown(-1); return; }
        reporting=true;
        var path = Path.Combine(Path.GetTempPath(), "Metapod-startup-error.txt");
        try { File.WriteAllText(path, error.ToString()); } catch (IOException) { }
        MessageBox.Show($"O aplicativo não conseguiu abrir. Detalhes: {path}", "Metapod", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(-1);
    }
}
