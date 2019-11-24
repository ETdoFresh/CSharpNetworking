using CSharpNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleWebSocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = "wss://localhost:11001";
            Console.WriteLine($"This is an example WebSocket Client. Press any key to connect to {uri}");
            Console.ReadKey();

            var client = new WebSocketClient(uri);
            client.OnConnected += (object sender, EventArgs e) => { client.Send("Hello World!"); };
            Console.ReadKey();
            
            client.Send("Hello World 2!");
            Console.ReadKey();

            client.Send("Hello World 3!");
            Console.ReadKey();

            client.Disconnect();
        }
    }
}
