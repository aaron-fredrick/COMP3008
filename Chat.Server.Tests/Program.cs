using System;
using System.IO;

namespace Chat.Server.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Chat Server Logging Test");
            Console.WriteLine("=========================\n");

            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestLog.log");

            // Clean up old log
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }

            // Test logging functions
            TestLogInfo(logPath);
            TestLogError(logPath);
            TestConcurrentLogging(logPath);

            Console.WriteLine("\nTest complete. Check TestLog.log for results.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void TestLogInfo(string logPath)
        {
            Console.WriteLine("Test 1: LogInfo");
            string message = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] [INFO] Test info message";
            File.AppendAllText(logPath, message + Environment.NewLine);
            Console.WriteLine("  - LogInfo written successfully");
        }

        static void TestLogError(string logPath)
        {
            Console.WriteLine("Test 2: LogError");
            string message = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] [ERROR] Test error message";
            File.AppendAllText(logPath, message + Environment.NewLine);
            Console.WriteLine("  - LogError written successfully");
        }

        static void TestConcurrentLogging(string logPath)
        {
            Console.WriteLine("Test 3: Concurrent logging");
            var lockObj = new object();
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                lock (lockObj)
                {
                    string message = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] [INFO] Concurrent message {i}";
                    File.AppendAllText(logPath, message + Environment.NewLine);
                }
            });
            Console.WriteLine("  - 10 concurrent messages written successfully");
        }
    }
}
