using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CSharpNetworking;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var tcpServer = new TCPServer(9999);
            Console.ReadKey();
        }
    }
}
