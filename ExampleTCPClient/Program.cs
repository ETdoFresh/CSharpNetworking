using CSharpNetworking;
using System;

namespace ExampleTCPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = "localhost";
            var port = 9999;
            Console.WriteLine($"This is an example TCP Client. Press any key to connect to tcp://{host}:{port}");
            Console.ReadKey();

            var client = new TcpClient(port);
            client.Opened += () => Console.WriteLine("Connected to server");
            client.Closed += () => Console.WriteLine("Disconnected from server");
            client.OpenAsync();
            Console.ReadKey();
            client.CloseAsync();
        }
    }
}
