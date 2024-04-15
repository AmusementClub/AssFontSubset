using System.Reflection;

namespace AssFontSubset.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public static string WindowTitle => $"AssFontSubset v{Assembly.GetEntryAssembly()!.GetName().Version}";

}
