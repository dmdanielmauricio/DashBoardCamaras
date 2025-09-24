using System;
using System.IO;

namespace ANPRDashboard.Helpers
{
    public static class Logger
    {
        private static readonly string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "anpr_log.txt");

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            WriteLog("ERROR", message + (ex != null ? " | " + ex.ToString() : ""));
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // 🔴 Mostrar si el log no puede escribirse
                Console.WriteLine("No se pudo escribir en el log: " + ex.Message);
                System.Windows.MessageBox.Show("No se pudo escribir en el log: " + ex.Message);
            }
        }
    }
}

