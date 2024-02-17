using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class TcpServer : AbstractServer<Socket>
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

        public override async Task Open()
        {
            IPEndPoint localEndPoint = null;
            
            if (string.IsNullOrEmpty(hostNameOrAddress) || hostNameOrAddress == "0.0.0.0" || hostNameOrAddress == "::/0")
            {
                Console.WriteLine($"TCPServer: Starting on IPAddress.Any:{port}...");
                localEndPoint = new IPEndPoint(IPAddress.Any, port);
            }
            else
            {
                Console.WriteLine($"TCPServer: Starting on {hostNameOrAddress}:{port}...");
                var ipHostInfo = Dns.GetHostEntry(hostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
                localEndPoint = new IPEndPoint(ipAddress, port);
            }
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(localEndPoint);
            socket.Listen(100);
            OnServerOpen.Invoke();
            await AcceptNewClient();
            Console.WriteLine($"TCPServer: Listening...");
        }

        public override async Task Close()
        {
            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
            }
            OnServerClose.Invoke();
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
            OnOpen.Invoke(socket);
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
                            OnMessage.Invoke(new Message<Socket>(socket, message));
                        }
                    }
                    while (receivedMessage.Contains(Terminator.VALUE))
                        receivedMessage = receivedMessage.Substring(receivedMessage.IndexOf(Terminator.VALUE) + Terminator.VALUE.Length);
                }
            }
            catch (Exception exception)
            {
                OnError.Invoke(exception);
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
                OnClose.Invoke(socket);
                Console.WriteLine($"TCPServer: Client {ip}:{port} disconnected normally.");
            }
            catch (Exception exception)
            {
                ClientDisconnectError(socket, exception);
            }
        }

        private void ClientDisconnectError(Socket socket, Exception exception)
        {
            OnError.Invoke(exception);
            OnClose.Invoke(socket);
            Console.WriteLine($"TCPServer: Client unexpectadely disconnected. {exception.Message}");
        }

        public override Task Send(Socket socket, string message)
        {
            return Send(socket, Encoding.UTF8.GetBytes(message));
        }

        public override async Task Send(Socket socket, byte[] bytes)
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

        public void Disconnect(Socket socket)
        {
            if (socket.Connected)
                socket.Disconnect(false);
        }
    }
}
