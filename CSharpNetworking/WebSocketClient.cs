﻿using System;
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
    public class WebSocketClient : IClient
    {
        public Uri uri;
        [NonSerialized] public Socket socket;
        public Stream stream;

        public Action OnSocketConnected;
        
        public WebSocketClient(string uriString) { uri = new Uri(uriString); }

        public override async Task Open()
        {
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
                OnSocketConnected.Invoke();
                Console.WriteLine($"Connected!");
                stream = GetNetworkStream();
                await StartHandshakeWithServer();
            }
            catch (Exception exception)
            {
                OnError.Invoke(exception);
            }
        }

        private async Task StartHandshakeWithServer()
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
            OnOpen.Invoke();
            await StartReceivingFromServer();
        }

        private async Task StartReceivingFromServer()
        {
            var received = new List<byte>();
            var buffer = new byte[2048];
            try
            {
                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    received.AddRange(buffer.Take(bytesRead));
                    if (!WebSocket.IsDiconnectPacket(received))
                    {
                        while (received.Count >= WebSocket.PacketLength(received))
                        {
                            var bytes = WebSocket.NetworkingBytesToByteArray(received.ToArray());
                            var message = new Message(bytes);
                            OnMessage.Invoke(message);
                            Console.WriteLine($"WebSocketClient: Received from {uri}: {message}");
                            received.RemoveRange(0, (int)WebSocket.PacketLength(received));
                        }
                    }
                    else break; // aka disconnect
                }
            }
            catch (Exception exception)
            {
                OnError.Invoke(exception);
            }
            finally
            {
                Close();
            }
        }

        public override Task Send(string message)
        {
            return Send(Encoding.UTF8.GetBytes(message));
        }

        public override async Task Send(byte[] bytes)
        {
            var message = Encoding.UTF8.GetString(bytes);
            bytes = WebSocket.ByteArrayToNetworkBytes(bytes);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            Console.WriteLine($"WebSocketClient: Sent to {uri}: {message}");
        }

        public override async Task Close()
        {
            try
            {
                if (socket.Connected) socket.DisconnectAsync(new SocketAsyncEventArgs { DisconnectReuseSocket = false });
                OnClose.Invoke();
                Console.WriteLine($"WebSocketClient: Disconnected normally.");
            }
            catch (Exception exception)
            {
                OnError.Invoke(exception);
                OnClose.Invoke();
                Console.WriteLine($"WebSocketClient: Unexpectedly disconnected. {exception.Message}");
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

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine($"WebSocketClient: Certificate error: {sslPolicyErrors}");
            return false;
        }
    }
}
