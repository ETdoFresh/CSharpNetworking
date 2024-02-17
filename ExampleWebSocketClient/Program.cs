using CSharpNetworking;
using System;
using System.Text;

namespace ExampleWebSocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = "wss://echo.websocket.org";
            Console.WriteLine($"This is an example WebSocket Client. Press any key to connect to {uri}");
            Console.ReadKey();

            var client = new WebSocketClient(uri);
            client.Opened += () => { Console.WriteLine("Connected to server."); };
            client.Opened += () => { client.SendAsync("Hello World!"); };
            client.Received += bytes => { Console.WriteLine($"Received from server: {Encoding.UTF8.GetString(bytes)}"); };
            client.Sent += bytes => { Console.WriteLine($"Sent to server: {Encoding.UTF8.GetString(bytes)}"); };
            client.Closed += () => { Console.WriteLine("Disconnected from server."); };
            client.OpenAsync();
            Console.ReadKey();
            
            client.SendAsync("Hello World 2!");
            Console.ReadKey();

            client.SendAsync("Hello World 3!");
            Console.ReadKey();

            client.CloseAsync();
        }
    }
}
