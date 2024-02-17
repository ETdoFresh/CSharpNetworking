using System;
using System.Text;
using System.Threading.Tasks;
using CSharpNetworking;

namespace Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var port = 9999;
            var server = new TcpServer(port);
            server.ServerOpened += () => Console.WriteLine("Server started");
            server.ServerClosed += () => Console.WriteLine("Server stopped");
            server.ServerError += (e) => Console.WriteLine($"Server error: {e.Message}");
            server.ClientConnected += (client) => Console.WriteLine($"Client connected: {client.RemoteEndPoint}");
            server.ClientDisconnected += (client) => Console.WriteLine($"Client disconnected: {client.RemoteEndPoint}");
            server.ClientError += (client, e) => Console.WriteLine($"Client error: {e.Message}");
            server.ClientReceived += (client, bytes) => Console.WriteLine($"Received from {client.RemoteEndPoint}: {Encoding.UTF8.GetString(bytes)} {bytes.Length} bytes");
            server.ClientReceived += (client, bytes) => _ = server.SendAsync(client, bytes);
            server.ClientSent += (client, bytes) => Console.WriteLine($"Sent to {client.RemoteEndPoint}: {Encoding.UTF8.GetString(bytes)} {bytes.Length} bytes");
            await server.OpenAsync();
            
            Console.WriteLine("Press any key to stop the server");
            Console.ReadKey();
        }
    }
}
