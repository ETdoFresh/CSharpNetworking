using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using CSharpNetworking;

namespace ExampleUdpServer
{
    public static class Program
    {
        private static readonly List<SocketReceiveFromResult> Clients = new List<SocketReceiveFromResult>();
        
        private static void Main(string[] args)
        {
            var ip = "0.0.0.0";
            var port = 9999;
            var bufferSize = 2048;

            Console.WriteLine($"This is an example UDP Server.");
            Console.WriteLine($"Starting server on udp://{ip}:{port}..."); 

            var server = new UdpServer(ip, port, bufferSize);
            server.ServerOpened += () => Console.WriteLine("Server started!");
            server.ServerClosed += () => Console.WriteLine("Server stopped!");
            server.ServerError += (e) => Console.WriteLine($"Server error: {e.Message}");
            server.ClientConnected += (client) => Console.WriteLine($"Client connected: {client.RemoteEndPoint}");
            server.ClientDisconnected += (client) => Console.WriteLine($"Client disconnected: {client.RemoteEndPoint}");
            server.ClientError += (client, e) => Console.WriteLine($"Client error: {e.Message}");
            server.ClientReceivedBytes += (client, bytes) => Console.WriteLine($"Received from {client.RemoteEndPoint}: {Encoding.UTF8.GetString(bytes)} {bytes.Length} bytes");
            server.ClientReceivedBytes += (client, bytes) => _ = server.SendAsync(client, bytes);
            server.ClientSentBytes += (client, bytes) => Console.WriteLine($"Sent to {client.RemoteEndPoint}: {Encoding.UTF8.GetString(bytes)} {bytes.Length} bytes");

            server.ClientConnected += (client) => Clients.Add(client);
            server.ClientDisconnected += (client) => Clients.Remove(client);
            
            server.ClientConnected += (client) => _ = server.SendAsync(client, Encoding.UTF8.GetBytes("Welcome to an echo server!"));

            Console.WriteLine("Submit 'exit' command to stop the server.");
            
            _ = server.OpenAsync();
            while (true)
            {
                var input = Console.ReadLine();
                if (input == "exit") break;
                if (IsBroadcastRandomCharactersRequest(input, out var size))
                {
                    BroadcastRandomCharacters(server, size);
                    continue;
                }
                Broadcast(server, input);
            }
            server.Close();
        }
        
        private static void Broadcast(Server<SocketReceiveFromResult> server, string message)
        {
            foreach (var client in Clients) 
                _ = server.SendAsync(client, message);
        }

        private static bool IsBroadcastRandomCharactersRequest(string input, out int size)
        {
            size = 16;
            if (!input.StartsWith("random")) return false;
            var parts = input.Split(' ');
            if (parts.Length == 1) return true;
            return parts.Length == 2 && int.TryParse(parts[1], out size);
        }

        private static void BroadcastRandomCharacters(Server<SocketReceiveFromResult> server, int size)
        {
            var random = new Random();
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var bytes = new byte[size];
            for (var i = 0; i < size; i++)
                bytes[i] = (byte)characters[random.Next(characters.Length)];
            Broadcast(server, Encoding.UTF8.GetString(bytes));
        }
    }
}