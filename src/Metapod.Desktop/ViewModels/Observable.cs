using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Metapod.Desktop.ViewModels;

public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool Set<T>(ref T field,T value,[CallerMemberName] string? name=null)
    { if(EqualityComparer<T>.Default.Equals(field,value)) return false; field=value; Changed(name); return true; }
    protected void Changed([CallerMemberName] string? name=null) => PropertyChanged?.Invoke(this,new(name));
}
public sealed class ActionCommand(Action<object?> action) : ICommand
{
    public bool CanExecute(object? parameter)=>true;
    public void Execute(object? parameter)=>action(parameter);
    public event EventHandler? CanExecuteChanged { add {} remove {} }
}
public interface IDesktopDialogs
{
    string? ChooseFolder();
    string[] ChooseCodeFiles();
    bool Confirm(string title,string message);
    void Copy(string text);
    void OpenUrl(string url);
}
