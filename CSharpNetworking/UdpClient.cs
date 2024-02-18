using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class UdpClient : Client
    {
        public string HostNameOrAddress { get; }
        public int Port { get; }
        public Socket Socket { get; }

        public UdpClient(string hostNameOrAddress, int port, int bufferSize = 2048)
        {
            HostNameOrAddress = hostNameOrAddress;
            Port = port;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            BufferSize = bufferSize;
        }

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

            Socket.Connect(localEndPoint);
            InvokeOpenedEvent();

            while (true)
                await ReceiveAsync();
        }

        public override void Close()
        {
            if (Socket != null)
            {
                Socket.Close();
                Socket.Dispose();
            }
            InvokeClosedEvent();
        }

        public override Task SendAsync(string message)
        {
            return SendAsync(Encoding.UTF8.GetBytes(message));
        }

        public override async Task SendAsync(byte[] bytes)
        {
            var arraySegment = new ArraySegment<byte>(bytes);
            await Socket.SendAsync(arraySegment, SocketFlags.None);
            InvokeSentEvent(bytes);
        }

        public async Task ReceiveAsync()
        {
            var arraySegment = new ArraySegment<byte>(new byte[BufferSize]);
            var receivedByteCount = await Socket.ReceiveAsync(arraySegment, SocketFlags.None);
            var bytes = arraySegment.Take(receivedByteCount).ToArray();
            InvokeReceivedEvent(bytes);
        }
    }
}