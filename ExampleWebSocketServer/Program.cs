using CSharpNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleWebSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new WebSocketServer("wss://localhost:11001");
            Console.ReadKey();
        }
    }
}
