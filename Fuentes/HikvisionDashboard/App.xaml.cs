using System;
using System.IO;
using System.Windows;

namespace ANPRViewer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configurar el dominio para manejar excepciones no controladas
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception);

            if (e.IsTerminating)
            {
                MessageBox.Show(
                    "La aplicación encontró un error crítico y debe cerrarse.\nRevise los logs para más información.",
                    "Error Crítico",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);

            MessageBox.Show(
                $"Error en la interfaz de usuario:\n{e.Exception.Message}",
                "Error de UI",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );

            // Marcar como manejada para evitar que la aplicación se cierre
            e.Handled = true;
        }

        private void LogException(Exception? ex)
        {
            if (ex == null) return;

            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                         "ANPRViewer", "Logs");
                Directory.CreateDirectory(logPath);

                var logFile = Path.Combine(logPath, $"error_{DateTime.Now:yyyyMMdd}.log");
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n";

                File.AppendAllText(logFile, logEntry);
            }
            catch
            {
                // Si no se puede escribir el log, al menos mostrarlo en debug
                System.Diagnostics.Debug.WriteLine($"Unhandled Exception: {ex}");
            }
        }
    }
}