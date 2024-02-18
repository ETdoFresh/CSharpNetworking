using CSharpNetworking;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ExampleWebSocketClient
{
    public static class Program
    {
        private static async Task Main(string[] args)
        {
            //var uri = "wss://echo.websocket.org";
            var uri = "wss://localhost:11001";
            var bufferSize = 2048;
            
            Console.WriteLine($"This is an example WebSocket Client. Press any key to connect to {uri}");
            Console.ReadKey();

            var client = new WebSocketClient(uri, bufferSize);
            client.Opened += () => { Console.WriteLine("Connected to server."); };
            client.Closed += () => { Console.WriteLine("Disconnected from server."); };
            client.Error += e => { Console.WriteLine($"Error: {e.Message}"); };
            client.Received += bytes => { Console.WriteLine($"Received from server: {Encoding.UTF8.GetString(bytes)}"); };
            client.Sent += bytes => { Console.WriteLine($"Sent to server: {Encoding.UTF8.GetString(bytes)}"); };

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
