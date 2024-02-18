using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    [Serializable]
    public class TcpClient : Client
    {
        public string HostNameOrAddress { get; }
        public int Port { get; }
        public Socket Socket { get; }
        public Stream Stream { get; private set; }

        public TcpClient(int port) : this("localhost", port) { }

        public TcpClient(string hostNameOrAddress, int port)
        {
            HostNameOrAddress = hostNameOrAddress;
            Port = port;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public override async Task OpenAsync()
        {
            try
            {
                var ipHostInfo = await Dns.GetHostEntryAsync(HostNameOrAddress);
                var ipAddress =
                    ipHostInfo.AddressList.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
                var localEndPoint = new IPEndPoint(ipAddress, Port);
                await Socket.ConnectAsync(localEndPoint);
                Stream = new NetworkStream(Socket);
                InvokeOpenedEvent();
                ProcessReceivedData();
            }
            catch (Exception exception)
            {
                InvokeErrorEvent(exception);
            }
        }

        private async void ProcessReceivedData()
        {
            var buffer = new byte[2048];
            try
            {
                while (true)
                {
                    var bytesRead = await Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    var incomingBytes = buffer.Take(bytesRead).ToArray();
                    InvokeReceivedEvent(incomingBytes);
                }
            }
            catch (Exception exception)
            {
                InvokeErrorEvent(exception);
            }
            finally
            {
                Close();
            }
        }

        public override Task SendAsync(string message)
        {
            return SendAsync(Encoding.UTF8.GetBytes(message));
        }

        public override async Task SendAsync(byte[] bytes)
        {
            await Stream.WriteAsync(bytes, 0, bytes.Length);
            await Stream.FlushAsync();
            InvokeSentEvent(bytes);
        }

        public override void Close()
        {
            try
            {
                if (Socket.Connected) Socket.Disconnect(false);
                InvokeClosedEvent();
            }
            catch (Exception exception)
            {
                InvokeErrorEvent(exception);
                InvokeClosedEvent();
            }
        }
    }
}