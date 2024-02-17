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
    public class TcpClient : Client
    {
        public string HostNameOrAddress { get; }
        public int Port { get; }
        public Socket Socket { get; }

        public TcpClient(int port) : this("localhost", port) { }

        public TcpClient(string hostNameOrAddress, int port)
        {
            HostNameOrAddress = hostNameOrAddress;
            Port = port;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public override async Task OpenAsync()
        {
            try
            {
                var ipHostInfo = await Dns.GetHostEntryAsync(HostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
                var localEndPoint = new IPEndPoint(ipAddress, Port);
                await Socket.ConnectAsync(localEndPoint);
                InvokeOpenedEvent();
                await StartReceivingFromGameServer();
            }
            catch(Exception exception)
            {
                InvokeErrorEvent(exception);
            }
        }

        public async Task StartReceivingFromGameServer()
        {
            var receivedBytes = new List<byte>();
            var buffer = new ArraySegment<byte>(new byte[2048]);
            try
            {
                while (true)
                {
                    var bytesRead = await Socket.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesRead == 0) break;

                    var terminatorBytes = Terminator.VALUE_BYTES;
                    var readBytes = buffer.Take(bytesRead);
                    receivedBytes.AddRange(readBytes);
                    
                    // Only check buffer/new data to prevent unnecessary searching through the entire receivedBytes
                    var terminatorIndexInBuffer = readBytes.IndexOf(terminatorBytes);
                    if (terminatorIndexInBuffer == -1) continue;
                    
                    var terminatorIndex = receivedBytes.IndexOf(terminatorBytes);
                    while (terminatorIndex != -1)
                    {
                        var messageBytes = receivedBytes.GetRange(0, terminatorIndex).ToArray();
                        InvokeReceivedEvent(messageBytes);
                        receivedBytes.RemoveRange(0, terminatorIndex + terminatorBytes.Length);
                        terminatorIndex = receivedBytes.IndexOf(terminatorBytes);
                    }
                }
            }
            catch(Exception exception)
            {
                InvokeErrorEvent(exception);
            }
            finally
            {
                await CloseAsync();
            }
        }

        public override Task SendAsync(string message)
        {
            return SendAsync(Encoding.UTF8.GetBytes(message));
        }

        public override async Task SendAsync(byte[] bytes)
        {
            var remoteEndPoint = (IPEndPoint)Socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            var bytesWithTerminator = bytes.Concat(Terminator.VALUE_BYTES);
            var bytesArraySegment = new ArraySegment<byte>(bytesWithTerminator.ToArray());
            await Socket.SendAsync(bytesArraySegment, SocketFlags.None);
            InvokeSentEvent(bytes);
        }

        public override async Task CloseAsync()
        {
            try
            {
                if (Socket.Connected) Socket.DisconnectAsync(new SocketAsyncEventArgs{ DisconnectReuseSocket = false });
                InvokeClosedEvent();
            }
            catch (Exception exception)
            {
                InvokeErrorEvent(exception);
                InvokeClosedEvent();
            }
        }
    }
}
