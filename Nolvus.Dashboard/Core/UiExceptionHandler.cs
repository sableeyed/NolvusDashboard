using System;
using Avalonia.Controls;
using Avalonia.Threading;
using Nolvus.Core.Enums;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Controls;

namespace Nolvus.Dashboard.Core
{
    public static class UiExceptionHandler
    {
        private static Func<Window?> _Owner = () => null;
        private static bool _Showing;

        public static void Install(Func<Window?> Owner)
        {
            _Owner = Owner;
            Dispatcher.UIThread.UnhandledException += OnUnhandledException;
        }

        private static async void OnUnhandledException(object? Sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            var Ex = e.Exception;

            ServiceSingleton.Logger?.Log("[UI] Unhandled exception : " + Ex.Message + Environment.NewLine + "Stack =>" + Ex.StackTrace);

            var Owner = _Owner();

            if (_Showing || Owner == null || !Owner.IsVisible)
                return;

            _Showing = true;

            try
            {
                await NolvusMessageBox.Show(Owner, "Error", Ex.Message + Environment.NewLine + Environment.NewLine + "The details have been written to the log.", MessageBoxType.Error);
            }
            catch (Exception ShowEx)
            {
                ServiceSingleton.Logger?.Log("[UI] Could not show the error : " + ShowEx.Message);
            }
            finally
            {
                _Showing = false;
            }
        }
    }
}
