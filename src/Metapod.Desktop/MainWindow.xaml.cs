using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using Metapod.Claude;
using Metapod.Infrastructure;
using Metapod.Desktop.ViewModels;

namespace Metapod.Desktop;

public partial class MainWindow : Window, IDesktopDialogs
{
    private readonly MainViewModel viewModel;
    public MainWindow()
    {
        InitializeComponent();
        var history = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Metapod", "history");
        bool webViewAvailable;
        try { Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString(); webViewAvailable = true; }
        catch (Exception error) when (error is not OutOfMemoryException) { webViewAvailable = false; }
        viewModel = new MainViewModel(new ClaudeAdapter(new ProcessRunner()), new FileChanges(history), new GitHubCatalog(), this, webViewAvailable);
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.Initialize();
        Closing += (_, _) => viewModel.Stop();
    }
    public string? ChooseFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Selecionar projeto para analisar" };
        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }
    public string[] ChooseCodeFiles()
    {
        var dialog = new OpenFileDialog { Title = "Selecionar arquivos de código para o contexto", Multiselect = true, CheckFileExists = true };
        return dialog.ShowDialog(this) == true ? dialog.FileNames : [];
    }
    public bool Confirm(string title, string message) => MessageBox.Show(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
    public void Copy(string text) => Clipboard.SetText(text);
    public void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Não foi possível abrir o navegador", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
