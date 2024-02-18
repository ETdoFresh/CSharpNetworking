using CSharpNetworking;
using System;
using System.Text;

namespace ExampleTCPClient
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            var host = "localhost";
            var port = 9999;
            var bufferSize = 2048 * 2;
            
            Console.WriteLine($"This is an example TCP Client. Press any key to connect to tcp://{host}:{port}");
            Console.ReadKey();

            var client = new TcpClient(host, port, bufferSize);
            client.Opened += () => Console.WriteLine("Connected to server");
            client.Closed += () => Console.WriteLine("Disconnected from server");
            client.Error += e => Console.WriteLine($"Error: {e.Message}");
            client.Received += bytes => Console.WriteLine($"Received: {Encoding.UTF8.GetString(bytes)}");
            client.Sent += bytes => Console.WriteLine($"Sent: {Encoding.UTF8.GetString(bytes)}");
            
            Console.WriteLine("Submit 'exit' command to stop the client.");
            
            _ = client.OpenAsync();
            while (true)
            {
                var input = Console.ReadLine();
                if (input == "exit") break;
                _ = client.SendAsync(input);
            }
            client.Close();
        }
    }
}
