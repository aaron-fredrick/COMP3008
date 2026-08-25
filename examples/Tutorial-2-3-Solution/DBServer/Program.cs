// Alex Starling - Distributed Computing - 2021
using System;
using System.ServiceModel;

namespace DBServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //Start the server
            Console.WriteLine("Welcome");
            var tcp = new NetTcpBinding();
            //Bind the interface
            //Create the host
            var host = new ServiceHost(typeof(DataServer));
            host.AddServiceEndpoint(typeof(DBInterface.DataServerInterface), tcp, "net.tcp://0.0.0.0:8100/DataService");
            host.Open();
            //Hold the server open until someone does something
            Console.WriteLine("System Online");
            Console.ReadLine();
            //Close the host
            host.Close();
        }
    }
}
