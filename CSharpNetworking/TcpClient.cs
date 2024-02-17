using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class TcpClient : BaseClient
    {
        public string hostNameOrAddress;
        public int port;
        [NonSerialized] public Socket socket;

        public TcpClient(int port) : this("localhost", port) { }

        public TcpClient(string hostNameOrAddress, int port)
        {
            this.hostNameOrAddress = hostNameOrAddress;
            this.port = port;
        }

        public override async Task OpenAsync()
        {
            try
            {
                Console.Write($"TCPClient: Connecting to {hostNameOrAddress}:{port}...");
                var ipHostInfo = Dns.GetHostEntry(hostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList.Where((i) => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
                var localEndPoint = new IPEndPoint(ipAddress, port);
                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(localEndPoint);
                InvokeOpenedEvent();
                Console.WriteLine($"Connected!");
                var doNotWait = StartReceivingFromGameServer();
            }
            catch(Exception exception)
            {
                InvokeErrorEvent(exception);
            }
        }

        public async Task StartReceivingFromGameServer()
        {
            var receivedBytes = Array.Empty<byte>();
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            try
            {
                while (true)
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
                        InvokeReceivedEvent(messageBytes);
                        receivedBytes = receivedBytes.Skip(terminatorIndex + terminatorBytes.Length).ToArray();
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
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            var bytesWithTerminator = bytes.Concat(Terminator.VALUE_BYTES);
            var bytesArraySegment = new ArraySegment<byte>(bytesWithTerminator.ToArray());
            await socket.SendAsync(bytesArraySegment, SocketFlags.None);
            InvokeSentEvent(bytes);
        }

        public override async Task CloseAsync()
        {
            try
            {
                if (socket.Connected) socket.DisconnectAsync(new SocketAsyncEventArgs{ DisconnectReuseSocket = false });
                InvokeClosedEvent();
                Console.WriteLine($"TCPClient: Disconnected normally.");
            }
            catch (Exception exception)
            {
                InvokeErrorEvent(exception);
                InvokeClosedEvent();
                Console.WriteLine($"TCPClient: Unexpectedly disconnected. {exception.Message}");
            }
        }
    }
}
