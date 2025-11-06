using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Globalization;

namespace TextFieldDemo;

using System.Globalization;
public partial class App : Application
{
    public override void Initialize()
    {
        CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
        CultureInfo.CurrentUICulture = new CultureInfo("ru-RU");
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}