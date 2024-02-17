using System;
using System.Collections.Generic;
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
            var socket = await Socket.AcceptAsync();
            InvokeOpenedEvent(socket);
            StartReceivingFromGameClient(socket);
            await AcceptNewClient();
        }

        private async void StartReceivingFromGameClient(Socket socket)
        {
            var receivedBytes = new List<byte>();
            var buffer = new ArraySegment<byte>(new byte[2048]);
            try
            {
                while (socket.Connected)
                {
                    var bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesRead == 0) break;

                    var terminatorBytes = Terminator.VALUE_BYTES;
                    var readBytes = buffer.Take(bytesRead);
                    receivedBytes.AddRange(readBytes);
                    
                    var terminatorIndexInReadBytes = readBytes.IndexOf(terminatorBytes);
                    if (terminatorIndexInReadBytes == -1) continue;
                    
                    var terminatorIndex = receivedBytes.IndexOf(terminatorBytes);
                    while (terminatorIndex != -1)
                    {
                        var messageBytes = receivedBytes.GetRange(0, terminatorIndex).ToArray();
                        InvokeReceivedEvent(socket, messageBytes);
                        receivedBytes.RemoveRange(0, terminatorIndex + terminatorBytes.Length);
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
