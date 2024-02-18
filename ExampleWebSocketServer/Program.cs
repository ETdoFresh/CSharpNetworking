using CSharpNetworking;
using System;

namespace ExampleWebSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = "wss://localhost:11001";
            var bufferSize = 2048;
            
            var server = new WebSocketServer(uri, bufferSize);
            server.OpenAsync();
            Console.ReadKey();
        }
    }
}
