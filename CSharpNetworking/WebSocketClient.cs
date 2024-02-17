using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class WebSocketClient : Client
    {
        public Uri uri;
        public Socket socket;
        public Stream stream;

        public Action OnSocketConnected;

        public WebSocketClient(string uriString)
        {
            uri = new Uri(uriString);
        }

        public override async Task OpenAsync()
        {
            var host = uri.Host;
            var port = uri.Port;
            try
            {
                var ipHostInfo = await Dns.GetHostEntryAsync(host);
                var ipAddress =
                    ipHostInfo.AddressList.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
                var localEndPoint = new IPEndPoint(ipAddress, port);
                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(localEndPoint);
                OnSocketConnected?.Invoke();
                stream = GetNetworkStream();
                await StartHandshakeWithServer();
            }
            catch (Exception exception)
            {
                InvokeErrorEvent(exception);
            }
        }

        private async Task StartHandshakeWithServer()
        {
            var httpEOF = Terminator.HTTP_BYTES;
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
            InvokeOpenedEvent();
            ProcessReceivedData();
        }

        private async void ProcessReceivedData()
        {
            var buffer = new byte[2048];
            try
            {
                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var rawBytes = buffer.Take(bytesRead).ToArray();

                    if (!WebSocket.IsDiconnectPacket(rawBytes))
                    {
                        var incomingBytes = WebSocket.NetworkingBytesToByteArray(rawBytes);
                        InvokeReceivedEvent(incomingBytes);
                    }
                    else break; // aka disconnect
                }
            }
            catch (Exception exception)
            {
                InvokeErrorEvent(exception);
            }
            finally
            {
                CloseAsync();
            }
        }

        public override Task SendAsync(string message)
        {
            return SendAsync(Encoding.UTF8.GetBytes(message));
        }

        public override async Task SendAsync(byte[] bytes)
        {
            var webSocketBytes = WebSocket.ByteArrayToNetworkBytes(bytes);
            await stream.WriteAsync(webSocketBytes, 0, webSocketBytes.Length);
            InvokeSentEvent(bytes);
        }

        public override async Task CloseAsync()
        {
            try
            {
                if (socket.Connected)
                    socket.DisconnectAsync(new SocketAsyncEventArgs { DisconnectReuseSocket = false });
                InvokeClosedEvent();
            }
            catch (Exception exception)
            {
                InvokeErrorEvent(exception);
                InvokeClosedEvent();
            }
        }

        private Stream GetNetworkStream()
        {
            var host = uri.Host;
            stream = new NetworkStream(socket);
            if (uri.Scheme.ToLower() != "wss") return stream;

            stream = new SslStream(stream, false, ValidateServerCertificate, null);
            ((SslStream)stream).AuthenticateAsClient(host);
            return stream;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            InvokeErrorEvent(new Exception($"WebSocketClient: Certificate error: {sslPolicyErrors}"));
            return false;
        }
    }
}