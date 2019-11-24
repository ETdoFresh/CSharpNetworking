using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public class TCPServer : IServer<Socket>
    {
        const string TERMINATOR = "\r\n";
        const string TERMINATOR_CONSOLE = "{\\r\\n}";
        public Socket socket;

        public event EventHandler OnListening = delegate { };
        public event EventHandler<Socket> OnAccepted = delegate { };
        public event EventHandler<Message<Socket>> OnMessage = delegate { };
        public event EventHandler<Socket> OnDisconnected = delegate { };
        public event EventHandler OnStopListening = delegate { };
        public event EventHandler<Exception> OnError = delegate { };

        public TCPServer(int port) : this("", port) { }

        public TCPServer(string hostNameOrAddress, int port)
        {
            StartServer(hostNameOrAddress, port);
        }

        private void StartServer(string hostNameOrAddress, int port)
        {
            IPEndPoint localEndPoint = null;
            if (hostNameOrAddress != "")
            {
                Console.WriteLine($"TCPServer: Starting on {hostNameOrAddress}:{port}...");
                var ipHostInfo = Dns.GetHostEntry(hostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
                localEndPoint = new IPEndPoint(ipAddress, port);
            }
            else
            {
                Console.WriteLine($"TCPServer: Starting on IPAddress.Any:{port}...");
                localEndPoint = new IPEndPoint(IPAddress.Any, port);
            }
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(localEndPoint);
            socket.Listen(100);
            var doNotWait = AcceptNewClient();
            Console.WriteLine($"TCPServer: Listening...");
        }

        private async Task AcceptNewClient()
        {
            Console.WriteLine("TCPServer: Waiting for a new client connection...");
            var socket = await this.socket.AcceptAsync();
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            Console.WriteLine($"TCPServer: A new client has connected {ip}:{port}...");
            OnAccepted.Invoke(this, socket);
            StartReceivingFromGameClient(socket);
            var doNotWait = AcceptNewClient();
        }

        private async void StartReceivingFromGameClient(Socket socket)
        {
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            try
            {
                var receivedMessage = "";
                while (socket.Connected)
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
                            Console.WriteLine($"TCPServer: Received from {ip}:{port}: {message}{TERMINATOR_CONSOLE}");
                            OnMessage.Invoke(this, new Message<Socket>(socket, message));
                        }
                    }
                    while (receivedMessage.Contains(TERMINATOR))
                        receivedMessage = receivedMessage.Substring(receivedMessage.IndexOf(TERMINATOR) + TERMINATOR.Length);
                }
            }
            catch (Exception exception)
            {
                OnError.Invoke(this, exception);
            }
            finally
            {
                Disconnect(socket);
            }
        }

        private void Disconnect(Socket socket)
        {
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            try
            {
                if (socket.Connected) socket.Disconnect(false);
                OnDisconnected.Invoke(this, socket);
                Console.WriteLine($"TCPServer: Client {ip}:{port} disconnected normally.");
            }
            catch (Exception exception)
            {
                DisconnectError(socket, exception);
            }
        }

        private void DisconnectError(Socket socket, Exception exception)
        {
            OnError.Invoke(this, exception);
            OnDisconnected.Invoke(this, socket);
            Console.WriteLine($"TCPServer: Client unexpectadely disconnected. {exception.Message}");
        }

        public void Send(Socket socket, string message)
        {
            var doNotWait = SendAsync(socket, message);
        }

        public async Task SendAsync(Socket socket, string message)
        {
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message + TERMINATOR));
            await socket.SendAsync(bytes, SocketFlags.None);
            Console.WriteLine($"TCPServer: Sent to {ip}:{port}: {message}{TERMINATOR_CONSOLE}");
        }
    }
}
