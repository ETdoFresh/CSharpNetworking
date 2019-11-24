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
        public Socket socket;

        public event EventHandler OnServerOpen = delegate { };
        public event EventHandler<Socket> OnOpen = delegate { };
        public event EventHandler<Message<Socket>> OnMessage = delegate { };
        public event EventHandler<Socket> OnClose = delegate { };
        public event EventHandler OnServerClose = delegate { };
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
            OnServerOpen.Invoke(this, null);
            var doNotWait = AcceptNewClient();
            Console.WriteLine($"TCPServer: Listening...");
        }

        public void Close()
        {
            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
            }
            OnServerClose.Invoke(this, null);
            Console.WriteLine($"TCPServer: Stop Listening...");
        }

        private async Task AcceptNewClient()
        {
            Console.WriteLine("TCPServer: Waiting for a new client connection...");
            var socket = await this.socket.AcceptAsync();
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            Console.WriteLine($"TCPServer: A new client has connected {ip}:{port}...");
            OnOpen.Invoke(this, socket);
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
                    if (receivedMessage.Contains(Terminator.VALUE))
                    {
                        var messages = receivedMessage.Split(new[] { Terminator.VALUE }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var message in messages)
                        {
                            Console.WriteLine($"TCPServer: Received from {ip}:{port}: {message}{Terminator.CONSOLE}");
                            OnMessage.Invoke(this, new Message<Socket>(socket, message));
                        }
                    }
                    while (receivedMessage.Contains(Terminator.VALUE))
                        receivedMessage = receivedMessage.Substring(receivedMessage.IndexOf(Terminator.VALUE) + Terminator.VALUE.Length);
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
                OnClose.Invoke(this, socket);
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
            OnClose.Invoke(this, socket);
            Console.WriteLine($"TCPServer: Client unexpectadely disconnected. {exception.Message}");
        }

        public void Send(Socket socket, string message)
        {
            Send(socket, Encoding.UTF8.GetBytes(message));
        }

        public void Send(Socket socket, byte[] bytes)
        {
            var doNotWait = SendAsync(socket, bytes);
        }

        public async Task SendAsync(Socket socket, byte[] bytes)
        {
            var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            var ip = remoteEndPoint.Address;
            var port = remoteEndPoint.Port;
            var bytesWithTerminator = bytes.Concat(Terminator.BYTES);
            var bytesArraySegment = new ArraySegment<byte>(bytesWithTerminator.ToArray());
            await socket.SendAsync(bytesArraySegment, SocketFlags.None);
            var message = Encoding.UTF8.GetString(bytes);
            Console.WriteLine($"TCPClient: Sent to {ip}:{port}: {message}{Terminator.CONSOLE}");
        }
    }
}
