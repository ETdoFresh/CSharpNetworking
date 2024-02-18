﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class WebSocketServer : Server<SocketStream>
    {
        private enum StreamType { Unsecured, SecuredLocalhost, SecuredRemote }
        
        public event Action handshakeReceived;
        public event Action handshakeSent;
        
        public Uri uri;
        public Socket serverSocket;
        
        public WebSocketServer(string uriString, int bufferSize = 2048)
        {
            _bufferSize = bufferSize;
            uri = new Uri(uriString);
        }

        public override async Task OpenAsync()
        {
            var host = uri.Host;
            var port = uri.Port;

            Console.WriteLine($"WebSocketServer: Starting on {uri}...");
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            if (host.ToLower() != "any" && host != "*")
            {
                var ipHostInfo = await Dns.GetHostEntryAsync(host);
                var ipAddress = ipHostInfo.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                localEndPoint = new IPEndPoint(ipAddress, port);
            }
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(localEndPoint);
            serverSocket.Listen(100);
            InvokeServerOpenedEvent();
            await AcceptNewClientAsync();
        }

        public override void Close()
        {
            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket.Dispose();
            }
            InvokeServerClosedEvent();
        }

        private async Task AcceptNewClientAsync()
        {
            while (true)
            {
                var clientSocket = await serverSocket.AcceptAsync();
                var stream = GetNetworkStream(clientSocket);
                var client = new SocketStream(clientSocket, stream);
                InvokeOpenedEvent(client);
                StartHandshakeWithClient(client);
            }
        }

        private async void StartHandshakeWithClient(SocketStream client)
        {
            try
            {
                var message = "";
                while (client.Socket.Connected)
                {
                    var buffer = new byte[_bufferSize];
                    var bytesRead = await client.Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    message += Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (message.Contains(Terminator.HTTP))
                    {
                        handshakeReceived?.Invoke();
                        if (Regex.IsMatch(message, "^GET", RegexOptions.IgnoreCase))
                        {
                            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                            // 3. Compute SHA-1 and Base64 hash of the new value
                            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                            var swk = Regex.Match(message, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                            var swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                            var swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                            var swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                            var outgoingMessage = "HTTP/1.1 101 Switching Protocols\r\n" +
                                "Connection: Upgrade\r\n" +
                                "Upgrade: websocket\r\n" +
                                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n";
                            byte[] response = Encoding.UTF8.GetBytes(outgoingMessage);
                            await client.Stream.WriteAsync(response, 0, response.Length);
                            handshakeSent?.Invoke();
                        }
                        else
                            throw new Exception("WebSocketServer: Incoming websocket handshake message was not in the right format.");

                        ProcessReceivedData(client);
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                InvokeClientErrorEvent(client, exception);
                Disconnect(client);
            }
        }

        private async void ProcessReceivedData(SocketStream client)
        {
            var buffer = new byte[2048];
            try
            {
                while (client.Socket.Connected)
                {
                    var bytesRead = await client.Stream.ReadAsync(buffer, 0, buffer.Length);
                    var rawBytes = buffer.Take(bytesRead).ToArray();
                    if (!WebSocketProtocol.IsDiconnectPacket(rawBytes))
                    {
                        var incomingBytes = WebSocketProtocol.NetworkingBytesToByteArray(rawBytes);
                        InvokeReceivedEvent(client, incomingBytes);
                    }
                    else break; // aka disconnect
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            finally
            {
                Disconnect(client);
            }
        }

        private void Disconnect(SocketStream client)
        {
            try
            {
                if (client.Socket.Connected) client.Socket.Disconnect(false);
                InvokeClosedEvent(client);
            }
            catch (Exception exception)
            {
                InvokeClientErrorEvent(client, exception);
                InvokeClosedEvent(client);
            }
        }

        public override Task SendAsync(SocketStream client, string message)
        {
            return SendAsync(client, Encoding.UTF8.GetBytes(message));
        }

        public override async Task SendAsync(SocketStream client, byte[] bytes)
        {
            bytes = WebSocketProtocol.ByteArrayToNetworkBytes(bytes);
            await client.Stream.WriteAsync(bytes, 0, bytes.Length);
            InvokeSentEvent(client, bytes);
        }

        private Stream GetNetworkStream(Socket socket)
        {
            var host = uri.Host;
            var streamType = StreamType.Unsecured;
            if (uri.Scheme.ToLower() == "wss")
                if (host.ToLower() == "localhost") streamType = StreamType.SecuredLocalhost;
                else if (host.ToLower() != "localhost") streamType = StreamType.SecuredRemote;

            var networkStream = new NetworkStream(socket);
            X509Certificate2 serverCertificate = null;
            byte[] bytes = null;
            SslStream sslStream = null;
            var currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var path = "";
            try
            {
                switch (streamType)
                {
                    case StreamType.Unsecured:
                        return networkStream;
                    case StreamType.SecuredLocalhost:
                        path = Path.Combine(currentDirectory, "localhost.pfx");
                        Console.WriteLine($"WebSocketServer: Reading certificate from {path}");
                        bytes = File.ReadAllBytes(path);
                        serverCertificate = new X509Certificate2(bytes, "1234");
                        Console.WriteLine("WebSocketServer: Certificate Read [localhost]...");
                        sslStream = new SslStream(networkStream);
                        sslStream.AuthenticateAsServer(serverCertificate,
                                enabledSslProtocols: SslProtocols.Tls,
                                clientCertificateRequired: false,
                                checkCertificateRevocation: false);
                        return sslStream;
                    case StreamType.SecuredRemote:
                        path = Path.Combine(currentDirectory, "letsencrypt.pfx");
                        Console.WriteLine($"WebSocketServer: Reading certificate from {path}");
                        bytes = File.ReadAllBytes(path);
                        serverCertificate = new X509Certificate2(bytes, "1234");
                        Console.WriteLine("WebSocketServer: Certificate Read [letsencrypt]...");
                        sslStream = new SslStream(networkStream);
                        sslStream.AuthenticateAsServer(serverCertificate,
                                enabledSslProtocols: SslProtocols.Tls,
                                clientCertificateRequired: false,
                                checkCertificateRevocation: false);
                        return sslStream;
                    default:
                        throw new Exception("Invalid stream type");
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); return null; }
        }
    }
}
