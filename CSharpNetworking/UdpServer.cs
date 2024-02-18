using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class UdpServer : Server<SocketReceiveFromResult>
    {
        public string HostNameOrAddress { get; }
        public int Port { get; }
        public Socket Socket { get; }
        
        public List<SocketReceiveFromResult> Clients { get; } = new List<SocketReceiveFromResult>();

        public UdpServer(string hostNameOrAddress, int port, int bufferSize = 2048)
        {
            HostNameOrAddress = hostNameOrAddress;
            Port = port;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            BufferSize = bufferSize;
        }

        public UdpServer(int port, int bufferSize = 2048) : this(null, port, bufferSize) { }

        public override async Task OpenAsync()
        {
            IPEndPoint localEndPoint;

            if (string.IsNullOrEmpty(HostNameOrAddress) || HostNameOrAddress == "0.0.0.0" ||
                HostNameOrAddress == "::/0")
            {
                localEndPoint = new IPEndPoint(IPAddress.Any, Port);
            }
            else
            {
                var ipHostInfo = await Dns.GetHostEntryAsync(HostNameOrAddress);
                var ipAddress =
                    ipHostInfo.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                localEndPoint = new IPEndPoint(ipAddress, Port);
            }

            Socket.Bind(localEndPoint);
            InvokeServerOpenedEvent();
            
            while (true)
                await ReceiveAsync(localEndPoint);
        }
        
        public override void Close()
        {
            if (Socket != null)
            {
                Socket.Close();
                Socket.Dispose();
            }
            InvokeServerClosedEvent();
        }
        
        public override async Task SendAsync(SocketReceiveFromResult client, string message)
        {
            await SendAsync(client, Encoding.UTF8.GetBytes(message));
        }
        
        public override async Task SendAsync(SocketReceiveFromResult client, byte[] bytes)
        {
            var arraySegment = new ArraySegment<byte>(bytes);
            await Socket.SendToAsync(arraySegment, SocketFlags.None, client.RemoteEndPoint);
            InvokeClientSentBytesEvent(client, bytes);
        }
        
        public async Task ReceiveAsync(EndPoint localEndPoint)
        {
            var buffer = new byte[BufferSize];
            var arraySegment = new ArraySegment<byte>(buffer);
            var client = await Socket.ReceiveFromAsync(arraySegment, SocketFlags.None, localEndPoint);

            if (IsNewClient(client))
            {
                Clients.Add(client);
                InvokeClientConnectedEvent(client);
            }
            
            var bytes = arraySegment.Take(client.ReceivedBytes).ToArray();
            InvokeClientReceivedBytesEvent(client, bytes);
        }
        
        public void Disconnect(SocketReceiveFromResult client)
        {
            Clients.Remove(client);
            InvokeClientDisconnectedEvent(client);
        }
        
        private bool IsNewClient(SocketReceiveFromResult client)
        {
            return !Clients.Any(c => c.RemoteEndPoint.Equals(client.RemoteEndPoint));
        }
    }
}