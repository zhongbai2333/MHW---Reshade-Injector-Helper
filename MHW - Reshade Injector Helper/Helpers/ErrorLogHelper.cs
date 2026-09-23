using System;
using System.IO;

namespace MHW___Reshade_Injector_Helper.Helpers
{
    public static class ErrorLogHelper
    {
        private static readonly object lockObject = new object();

        public static void Log(Exception ex)
        {
            TryWrite($"{ex.Source} {ex.Message} - {ex.InnerException} - {ex.StackTrace}");
        }

        public static void Log(string message)
        {
            TryWrite(message);
        }

        public static void Log(string message, Exception ex)
        {
            Log(message);
            Log(ex);
        }

        private static void TryWrite(string message)
        {
            Console.Error.WriteLine(message);
            try
            {
                lock (lockObject)
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "ErrorLog.txt");
                    File.AppendAllText(path, $"{Environment.NewLine}{DateTime.Now:dd/MM/yyyy HH:mm:ss}{Environment.NewLine}{message}{Environment.NewLine}");
                }
            }
            catch (Exception logException)
            {
                // Logging must never mask the original failure.
                Console.Error.WriteLine($"Could not write ErrorLog.txt: {logException.Message}");
            }
        }
    }
}
