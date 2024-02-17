using CSharpNetworking;
using System;

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
            client.OnOpen += () => { client.Send("Hello World!"); };
            client.Open();
            Console.ReadKey();
            
            client.Send("Hello World 2!");
            Console.ReadKey();

            client.Send("Hello World 3!");
            Console.ReadKey();

            client.Close();
        }
    }
}
