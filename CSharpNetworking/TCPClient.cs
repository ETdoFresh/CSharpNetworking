using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public class TCPClient : IClient
    {
        const string TERMINATOR = "\r\n";
        const string TERMINATOR_CONSOLE = "{\\r\\n}";
        public Socket socket;

        public event EventHandler OnOpen = delegate { };
        public event EventHandler<Message> OnMessage = delegate { };
        public event EventHandler OnClose = delegate { };
        public event EventHandler<Exception> OnError = delegate { };

        public TCPClient(int port) : this("localhost", port) { }

        public TCPClient(string hostNameOrAddress, int port)
        {
            var doNotWait = Connect(hostNameOrAddress, port);
        }

        public async Task Connect(string hostNameOrAddress, int port)
        {
            try
            {
                Console.Write($"TCPClient: Connecting to {hostNameOrAddress}:{port}...");
                var ipHostInfo = Dns.GetHostEntry(hostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList.Where((i) => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
                var localEndPoint = new IPEndPoint(ipAddress, port);
                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(localEndPoint);
                OnOpen.Invoke(this, null);
                Console.WriteLine($"Connected!");
                var doNotWait = StartReceivingFromGameServer();
            }
            catch(Exception exception)
            {
                OnError.Invoke(this, exception);
            }
        }

        public async Task StartReceivingFromGameServer()
        {
            var receivedMessage = "";
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

                    receivedMessage += Encoding.UTF8.GetString(buffer.Array, 0, bytesRead);
                    if (receivedMessage.Contains(TERMINATOR))
                    {
                        var messages = receivedMessage.Split(new[] { TERMINATOR }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var message in messages)
                        {
                            OnMessage.Invoke(this, new Message(message));
                            Console.WriteLine($"TCPClient: Received from {ip}:{port}: {message}{TERMINATOR_CONSOLE}");
                        }
                    }
                    while (receivedMessage.Contains(TERMINATOR))
                        receivedMessage = receivedMessage.Substring(receivedMessage.IndexOf(TERMINATOR) + TERMINATOR.Length);
                }
            }
            catch(Exception exception)
            {
                OnError.Invoke(this, exception);
            }
            finally
            {
                Disconnect();
            }
        }

        public void Send(string message)
        {
            var doNotWait = SendAsync(message);
        }

        public async Task SendAsync(string message)
        {
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message + TERMINATOR));
            await socket.SendAsync(bytes, SocketFlags.None);
            Console.WriteLine($"TCPClient: Sent to {ip}:{port}: {message}{TERMINATOR_CONSOLE}");
        }

        public void Disconnect()
        {
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            try
            {
                if (socket.Connected) socket.Disconnect(false);
                OnClose.Invoke(this, null);
                Console.WriteLine($"TCPClient: Disconnected normally.");
            }
            catch (Exception exception)
            {
                DisconnectError(socket, exception);
            }
        }

        private void DisconnectError(Socket socket, Exception exception)
        {
            OnError.Invoke(this, exception);
            OnClose.Invoke(this, null);
            Console.WriteLine($"TCPClient: Unexpectadely disconnected. {exception.Message}");
        }
    }
}
