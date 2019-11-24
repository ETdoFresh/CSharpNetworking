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
                    if (receivedMessage.Contains(Terminator.VALUE))
                    {
                        var messages = receivedMessage.Split(new[] { Terminator.VALUE }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var message in messages)
                        {
                            OnMessage.Invoke(this, new Message(message));
                            Console.WriteLine($"TCPClient: Received from {ip}:{port}: {message}{Terminator.CONSOLE}");
                        }
                    }
                    while (receivedMessage.Contains(Terminator.VALUE))
                        receivedMessage = receivedMessage.Substring(receivedMessage.IndexOf(Terminator.VALUE) + Terminator.VALUE.Length);
                }
            }
            catch(Exception exception)
            {
                OnError.Invoke(this, exception);
            }
            finally
            {
                Close();
            }
        }

        public void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
        }

        public void Send(byte[] bytes)
        {
            var doNotWait = SendAsync(bytes);
        }

        public async Task SendAsync(byte[] bytes)
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

        public void Close()
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
                CloseError(socket, exception);
            }
        }

        private void CloseError(Socket socket, Exception exception)
        {
            OnError.Invoke(this, exception);
            OnClose.Invoke(this, null);
            Console.WriteLine($"TCPClient: Unexpectadely disconnected. {exception.Message}");
        }
    }
}
