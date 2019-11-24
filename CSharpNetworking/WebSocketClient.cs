using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public class WebSocketClient : IClient
    {
        public Uri uri;
        public Socket socket;
        public Stream stream;

        public event EventHandler OnSocketConnected = delegate { };
        public event EventHandler OnConnected = delegate { };
        public event EventHandler<Message> OnMessage = delegate { };
        public event EventHandler OnDisconnected = delegate { };
        public event EventHandler<Exception> OnError = delegate { };

        public WebSocketClient(string uriString)
        {
            var doNotWait = Connect(uriString);
        }

        public async Task Connect(string uriString)
        {
            uri = new Uri(uriString);
            var host = uri.Host;
            var port = uri.Port;
            try
            {
                Console.Write($"WebSocketClient: Connecting to {uri}...");
                var ipHostInfo = Dns.GetHostEntry(host);
                var ipAddress = ipHostInfo.AddressList.Where((i) => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
                var localEndPoint = new IPEndPoint(ipAddress, port);
                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(localEndPoint);
                OnSocketConnected.Invoke(this, null);
                Console.WriteLine($"Connected!");
                stream = WebSocket.GetNetworkStream(socket, uri);
                var doNotWait = StartHandshakeWithServer();
            }
            catch (Exception exception)
            {
                OnError.Invoke(this, exception);
            }
        }

        public async Task StartHandshakeWithServer()
        {
            var httpEOF = Encoding.UTF8.GetBytes("\r\n\r\n");
            var received = new List<byte>();
            var buffer = new byte[2048];
            var host = uri.Host;
            var path = uri.PathAndQuery;
            var eol = "\r\n";
            var handshake = "GET " + path + " HTTP/1.1" + eol;
            handshake += "Host: " + host + eol;
            handshake += "Upgrade: websocket" + eol;
            handshake += "Connection: Upgrade" + eol;
            handshake += "Sec-WebSocket-Key: V2ViU29ja2V0Q2xpZW50" + eol;
            handshake += "Sec-WebSocket-Version: 13" + eol;
            handshake += eol;
            var handshakeBytes = Encoding.UTF8.GetBytes(handshake);
            await stream.WriteAsync(handshakeBytes, 0, handshakeBytes.Length);
            while (received.IndexOf(httpEOF) == -1)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                received.AddRange(buffer.Take(bytesRead));
            }
            OnConnected.Invoke(this, null);
            var doNotWait = StartReceivingFromServer();
        }

        public async Task StartReceivingFromServer()
        {
            var received = new List<byte>();
            var buffer = new ArraySegment<byte>(new byte[2048]);
            try
            {
                while (true)
                {
                    var bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);
                    received.AddRange(buffer.Take(bytesRead));
                    if (!WebSocket.IsDiconnectPacket(received))
                    {
                        while (received.Count >= WebSocket.PacketLength(received))
                        {
                            var message = WebSocket.BytesToString(received.ToArray());
                            OnMessage.Invoke(this, new Message(message));
                            Console.WriteLine($"WebSocketClient: Received from {uri}: {message}");
                            received.RemoveRange(0, (int)WebSocket.PacketLength(received));
                        }
                    }
                    else break; // aka disconnect
                }
            }
            catch (Exception exception)
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
            var bytes = WebSocket.StringToBytes(message);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            Console.WriteLine($"WebSocketClient: Sent to {uri}: {message}");
        }

        public void Disconnect()
        {
            try
            {
                if (socket.Connected) socket.Disconnect(false);
                OnDisconnected.Invoke(this, null);
                Console.WriteLine($"WebSocketClient: Disconnected normally.");
            }
            catch (Exception exception)
            {
                DisconnectError(socket, exception);
            }
        }

        private void DisconnectError(Socket socket, Exception exception)
        {
            OnError.Invoke(this, exception);
            OnDisconnected.Invoke(this, null);
            Console.WriteLine($"WebSocketClient: Unexpectadely disconnected. {exception.Message}");
        }
    }
}
