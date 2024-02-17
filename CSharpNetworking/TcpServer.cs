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
        public string hostNameOrAddress;
        public int port;
        [NonSerialized] public Socket socket;
        
        public TcpServer(int port) : this("", port) { }

        public TcpServer(string hostNameOrAddress, int port)
        {
            this.hostNameOrAddress = hostNameOrAddress;
            this.port = port;
        }

        public override async Task OpenAsync()
        {
            IPEndPoint localEndPoint = null;
            
            if (string.IsNullOrEmpty(hostNameOrAddress) || hostNameOrAddress == "0.0.0.0" || hostNameOrAddress == "::/0")
            {
                localEndPoint = new IPEndPoint(IPAddress.Any, port);
            }
            else
            {
                var ipHostInfo = await Dns.GetHostEntryAsync(hostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                localEndPoint = new IPEndPoint(ipAddress, port);
            }
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(localEndPoint);
            socket.Listen(100);
            InvokeServerOpenedEvent();
            await AcceptNewClient();
        }

        public override async Task CloseAsync()
        {
            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
            }
            InvokeServerClosedEvent();
        }

        private async Task AcceptNewClient()
        {
            var socket = await this.socket.AcceptAsync();
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
