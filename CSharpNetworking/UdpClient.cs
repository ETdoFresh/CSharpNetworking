using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DotNetUdpClient = System.Net.Sockets.UdpClient;

namespace CSharpNetworking
{
    [Serializable]
    public class UdpClient : Client
    {
        public string HostNameOrAddress { get; }
        public int Port { get; }
        public DotNetUdpClient DotNetUdpClient { get; }

        public UdpClient(string hostNameOrAddress, int port, int bufferSize = 2048)
        {
            HostNameOrAddress = hostNameOrAddress;
            Port = port;
            DotNetUdpClient = new DotNetUdpClient();
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

            DotNetUdpClient.Connect(localEndPoint);
            InvokeOpenedEvent();

            while (true)
                await ReceiveAsync();
        }

        public override void Close()
        {
            if (DotNetUdpClient != null)
            {
                DotNetUdpClient.Close();
                DotNetUdpClient.Dispose();
            }
            InvokeClosedEvent();
        }

        public override Task SendAsync(string message)
        {
            return SendAsync(Encoding.UTF8.GetBytes(message));
        }

        public override async Task SendAsync(byte[] bytes)
        {
            await DotNetUdpClient.SendAsync(bytes, bytes.Length);
            InvokeSentEvent(bytes);
        }

        public async Task ReceiveAsync()
        {
            var result = await DotNetUdpClient.ReceiveAsync();
            var bytes = result.Buffer;
            InvokeReceivedEvent(bytes);
        }
    }
}