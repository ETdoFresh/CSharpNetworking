﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class TCPClient : IClient
    {
        public string hostNameOrAddress;
        public int port;
        [NonSerialized] public Socket socket;

        public TCPClient(int port) : this("localhost", port) { }

        public TCPClient(string hostNameOrAddress, int port)
        {
            this.hostNameOrAddress = hostNameOrAddress;
            this.port = port;
        }

        public override async Task Open()
        {
            try
            {
                Console.Write($"TCPClient: Connecting to {hostNameOrAddress}:{port}...");
                var ipHostInfo = Dns.GetHostEntry(hostNameOrAddress);
                var ipAddress = ipHostInfo.AddressList.Where((i) => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
                var localEndPoint = new IPEndPoint(ipAddress, port);
                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(localEndPoint);
                OnOpen.Invoke();
                Console.WriteLine($"Connected!");
                var doNotWait = StartReceivingFromGameServer();
            }
            catch(Exception exception)
            {
                OnError.Invoke(exception);
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
                            OnMessage.Invoke(new Message(message));
                            Console.WriteLine($"TCPClient: Received from {ip}:{port}: {message}{Terminator.CONSOLE}");
                        }
                    }
                    while (receivedMessage.Contains(Terminator.VALUE))
                        receivedMessage = receivedMessage.Substring(receivedMessage.IndexOf(Terminator.VALUE) + Terminator.VALUE.Length);
                }
            }
            catch(Exception exception)
            {
                OnError.Invoke(exception);
            }
            finally
            {
                await Close();
            }
        }

        public override Task Send(string message)
        {
            return Send(Encoding.UTF8.GetBytes(message));
        }

        public override async Task Send(byte[] bytes)
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

        public override async Task Close()
        {
            try
            {
                if (socket.Connected) socket.DisconnectAsync(new SocketAsyncEventArgs{ DisconnectReuseSocket = false });
                OnClose.Invoke();
                Console.WriteLine($"TCPClient: Disconnected normally.");
            }
            catch (Exception exception)
            {
                OnError.Invoke(exception);
                OnClose.Invoke();
                Console.WriteLine($"TCPClient: Unexpectedly disconnected. {exception.Message}");
            }
        }
    }
}
