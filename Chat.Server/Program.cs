using System;
using Chat.Server.Hosting;

namespace Chat.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Chat Server - COMP3008");
            Console.WriteLine("========================");

            var serviceHost = new ChatServiceHost();

            try
            {
                serviceHost.Start();
                Console.WriteLine("\nPress any key to stop the server...");
                Console.ReadKey();
                serviceHost.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
