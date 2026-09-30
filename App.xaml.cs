using System;
using System.Windows;
using System.Windows.Threading;

namespace MCD2SkinStudioWpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unhandled exception occurred:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
            "MCD2 Custom Skin",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show(
                $"Critical application error:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Critical Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
