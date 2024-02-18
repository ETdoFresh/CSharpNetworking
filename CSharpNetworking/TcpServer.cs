using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class TcpServer : Server<Socket>
    {
        public string HostNameOrAddress { get; }
        public int Port { get; }
        public Socket Socket { get; }
        
        public TcpServer(int port) : this("", port) { }

        public TcpServer(string hostNameOrAddress, int port)
        {
            HostNameOrAddress = hostNameOrAddress;
            Port = port;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

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
            InvokeOpenedEvent(socket);
            ProcessReceivedData(socket);
        }

        private async void ProcessReceivedData(Socket socket)
        {
            var buffer = new byte[2048];
            try
            {
                var stream = new NetworkStream(socket);
                while (socket.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    var incomingBytes = buffer.Take(bytesRead).ToArray();
                    InvokeReceivedEvent(socket, incomingBytes);
                }
            }
            catch (Exception exception)
            {
                InvokeClientErrorEvent(socket, exception);
            }
            finally
            {
                ClientDisconnect(socket);
            }
        }

        private void ClientDisconnect(Socket socket)
        {
            try
            {
                if (socket.Connected) socket.Disconnect(false);
                InvokeClosedEvent(socket);
            }
            catch (Exception exception)
            {
                InvokeClientErrorEvent(socket, exception);
                InvokeClosedEvent(socket);
            }
        }

        public override Task SendAsync(Socket socket, string message)
        {
            return SendAsync(socket, Encoding.UTF8.GetBytes(message));
        }

        public override async Task SendAsync(Socket socket, byte[] bytes)
        {
            var stream = new NetworkStream(socket);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();
            InvokeSentEvent(socket, bytes);
        }

        public void Disconnect(Socket socket)
        {
            if (socket.Connected)
                socket.Disconnect(false);
        }
    }
}