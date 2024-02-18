using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class TcpServer : Server<SocketStream>
    {
        public string HostNameOrAddress { get; }
        public int Port { get; }
        public Socket Socket { get; }

        public TcpServer(string hostNameOrAddress, int port, int bufferSize = 2048)
        {
            HostNameOrAddress = hostNameOrAddress;
            Port = port;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            BufferSize = bufferSize;
        }

        public TcpServer(int port) : this("", port) { }

        public override async Task OpenAsync()
        {
            IPEndPoint localEndPoint;

            if (string.IsNullOrEmpty(HostNameOrAddress) || HostNameOrAddress == "0.0.0.0" ||
                HostNameOrAddress == "::/0")
            {
                localEndPoint = new IPEndPoint(IPAddress.Any, Port);
            }
            else
            {
                var ipHostInfo = await Dns.GetHostEntryAsync(HostNameOrAddress);
                var ipAddress =
                    ipHostInfo.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                localEndPoint = new IPEndPoint(ipAddress, Port);
            }

            Socket.Bind(localEndPoint);
            Socket.Listen(100);
            InvokeServerOpenedEvent();

            while (true)
                await AcceptNewClient();
        }

        public override void Close()
        {
            if (Socket != null)
            {
                Socket.Close();
                Socket.Dispose();
            }
            InvokeServerClosedEvent();
        }

        private async Task AcceptNewClient()
        {
            var socket = await Socket.AcceptAsync();
            var stream = new NetworkStream(socket);
            var client = new SocketStream(socket, stream);
            InvokeClientConnectedEvent(client);
            ProcessReceivedData(client);
        }

        private async void ProcessReceivedData(SocketStream client)
        {
            var buffer = new byte[BufferSize];
            try
            {
                while (client.Socket.Connected)
                {
                    var bytesRead = await client.Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    var incomingBytes = buffer.Take(bytesRead).ToArray();
                    InvokeClientReceivedBytesEvent(client, incomingBytes);
                }
            }
            catch (Exception exception)
            {
                InvokeClientErrorEvent(client, exception);
            }
            finally
            {
                ClientDisconnect(client);
            }
        }

        private void ClientDisconnect(SocketStream client)
        {
            try
            {
                if (client.Socket.Connected) client.Socket.Disconnect(false);
                InvokeClientDisconnectedEvent(client);
            }
            catch (Exception exception)
            {
                InvokeClientErrorEvent(client, exception);
                InvokeClientDisconnectedEvent(client);
            }
        }

        public override Task SendAsync(SocketStream client, string message)
        {
            return SendAsync(client, Encoding.UTF8.GetBytes(message));
        }

        public override async Task SendAsync(SocketStream client, byte[] bytes)
        {
            await client.Stream.WriteAsync(bytes, 0, bytes.Length);
            await client.Stream.FlushAsync();
            InvokeClientSentBytesEvent(client, bytes);
        }

        public void Disconnect(Socket socket)
        {
            if (socket.Connected)
                socket.Disconnect(false);
        }
    }
}