using System.Windows;
using Microsoft.Web.WebView2.Wpf;

namespace Metapod.Desktop;

public static class WebView2Behavior
{
    public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
        "Html", typeof(string), typeof(WebView2Behavior), new PropertyMetadata(null, OnHtmlChanged));
    public static void SetHtml(DependencyObject obj, string value) => obj.SetValue(HtmlProperty, value);
    public static string GetHtml(DependencyObject obj) => (string)obj.GetValue(HtmlProperty);
    private static async void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebView2 webView) return;
        var html = e.NewValue as string ?? "";
        try
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.NavigateToString(string.IsNullOrEmpty(html) ? "<html><body></body></html>" : html);
        }
        catch (Exception error) when (error is not OutOfMemoryException) { /* Runtime do WebView2 indisponível; a aba alterna para texto puro. */ }
    }
}
