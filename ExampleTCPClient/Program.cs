using CSharpNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var client = new TCPClient(port);
            Console.ReadKey();
            client.Close();
        }
    }
}
