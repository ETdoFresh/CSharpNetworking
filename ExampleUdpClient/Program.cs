using System;
using System.Text;
using CSharpNetworking;

namespace ExampleUdpClient
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            var host = "localhost";
            var port = 9999;
            var bufferSize = 2048;
            
            Console.WriteLine($"This is an example UDP Client.");
            Console.WriteLine($"Press any key to connect to udp://{host}:{port}");
            Console.ReadKey();

            var client = new UdpClient(host, port, bufferSize);
            client.Opened += () => Console.WriteLine("Connected to Server");
            client.Closed += () => Console.WriteLine("Disconnected from Server");
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