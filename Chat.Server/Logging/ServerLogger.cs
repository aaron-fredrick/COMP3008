using System;
using System.IO;

namespace Chat.Server.Logging
{
    public static class ServerLogger
    {
        private static readonly object _lock = new object();
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChatServer.log");

        public static void Info(string message)
        {
            Write(LogLevel.Info, "SERVER", "INFO", message);
        }

        public static void Request(string clientType, string operation, string details)
        {
            Write(LogLevel.Request, clientType, operation, details);
        }

        public static void Success(string clientType, string operation, string details)
        {
            Write(LogLevel.Success, clientType, operation, details);
        }

        public static void Warning(string clientType, string operation, string details)
        {
            Write(LogLevel.Warning, clientType, operation, details);
        }

        public static void Error(string clientType, string operation, string details)
        {
            Write(LogLevel.Error, clientType, operation, details);
        }

        public static void Callback(string userId, string callback, string details)
        {
            Write(LogLevel.Callback, "DUPLEX", callback, $"{userId} ← {details}");
        }

        private static void Write(LogLevel level, string clientType, string operation, string details)
        {
            lock (_lock)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] [{clientType}] [{operation}] {details}";

                // Write to file
                try
                {
                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                }
                catch { }

                // Write to console with colors
                WriteToConsole(level, clientType, operation, details);
            }
        }

        private static void WriteToConsole(LogLevel level, string clientType, string operation, string details)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

            // Timestamp in dark gray
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{timestamp,-12} ");

            // Client type color
            WriteClientType(clientType);

            // Operation color based on log level
            WriteOperation(level, operation);

            // Details in white
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($" {details}");

            Console.ForegroundColor = originalColor;
        }

        private static void WriteClientType(string clientType)
        {
            ConsoleColor color = clientType == "POLLING" ? ConsoleColor.Cyan : 
                               clientType == "DUPLEX" ? ConsoleColor.Magenta : 
                               ConsoleColor.Gray;

            Console.ForegroundColor = color;
            Console.Write($"{clientType,-8} ");
            Console.ResetColor();
        }

        private static void WriteOperation(LogLevel level, string operation)
        {
            ConsoleColor color;
            switch (level)
            {
                case LogLevel.Info:
                    color = ConsoleColor.Blue;
                    break;
                case LogLevel.Request:
                    color = ConsoleColor.Gray;
                    break;
                case LogLevel.Success:
                    color = ConsoleColor.Green;
                    break;
                case LogLevel.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    color = ConsoleColor.Red;
                    break;
                case LogLevel.Callback:
                    color = ConsoleColor.Magenta;
                    break;
                default:
                    color = ConsoleColor.White;
                    break;
            }

            Console.ForegroundColor = color;
            Console.Write($"{operation,-12} ");
            Console.ResetColor();
        }

        public static void Initialize()
        {
            try
            {
                var logDir = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                File.WriteAllText(LogFilePath, $"=== Chat Server Log - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not initialize logging: {ex.Message}");
            }
        }

        public static void PrintHeader()
        {
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");
            Console.WriteLine("  COMP3008 CHAT SERVER");
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ● SERVER ONLINE");
            Console.ResetColor();
            Console.WriteLine("  Polling : http://localhost:9000/ChatService/Polling");
            Console.WriteLine("  Duplex  : net.tcp://localhost:8081/ChatService/Duplex");
            Console.WriteLine("══════════════════════════════════════════════════════════════════════");
            Console.WriteLine();
        }
    }
}
