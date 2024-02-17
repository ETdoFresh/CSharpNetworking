﻿using System;
using CSharpNetworking;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = 9999;
            var server = new TcpServer(port);
            server.OpenAsync();
            Console.ReadKey();
        }
    }
}
