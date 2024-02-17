using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class TcpServer : BaseServer<Socket>
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
            
            if (string.IsNullOrEmpty(HostNameOrAddress) || HostNameOrAddress == "0.0.0.0" || HostNameOrAddress == "::/0")
            {
                localEndPoint = new IPEndPoint(IPAddress.Any, Port);
            }
            else
            {
                var ipHostInfo = await Dns.GetHostEntryAsync(HostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                localEndPoint = new IPEndPoint(ipAddress, Port);
            }
            
            Socket.Bind(localEndPoint);
            Socket.Listen(100);
            InvokeServerOpenedEvent();
            await AcceptNewClient();
        }

        public override async Task CloseAsync()
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
            var socket = await this.Socket.AcceptAsync();
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            InvokeOpenedEvent(socket);
            StartReceivingFromGameClient(socket);
            await AcceptNewClient();
        }

        private async void StartReceivingFromGameClient(Socket socket)
        {
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            try
            {
                var receivedBytes = Array.Empty<byte>();
                while (socket.Connected)
                {
                    var buffer = new ArraySegment<byte>(new byte[2048]);
                    var bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesRead == 0) break;

                    receivedBytes = receivedBytes.Concat(buffer.Array.Take(bytesRead)).ToArray();
                    var terminatorBytes = Terminator.VALUE_BYTES;
                    var terminatorIndex = receivedBytes.IndexOf(terminatorBytes);
                    while (terminatorIndex != -1)
                    {
                        var messageBytes = receivedBytes.Take(terminatorIndex).ToArray();
                        InvokeReceivedEvent(socket, messageBytes);
                        receivedBytes = receivedBytes.Skip(terminatorIndex + terminatorBytes.Length).ToArray();
                        terminatorIndex = receivedBytes.IndexOf(terminatorBytes);
                    }
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
                var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
                var ip = remoteEndPoint.Address;
                var port = remoteEndPoint.Port;
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
            var bytesWithTerminator = bytes.Concat(Terminator.VALUE_BYTES);
            var bytesArraySegment = new ArraySegment<byte>(bytesWithTerminator.ToArray());
            await socket.SendAsync(bytesArraySegment, SocketFlags.None);
            InvokeSentEvent(socket, bytes);
        }

        public void Disconnect(Socket socket)
        {
            if (socket.Connected)
                socket.Disconnect(false);
        }
    }
}
