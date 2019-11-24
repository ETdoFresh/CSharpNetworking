using System.IO;
using System.Net;
using System.Net.Sockets;

namespace CSharpNetworking
{
    public class SocketStream
    {
        public Socket socket;
        public Stream stream;

        public SocketStream(Socket socket, Stream stream)
        {
            this.socket = socket;
            this.stream = stream;
        }

        public IPAddress IP => ((IPEndPoint)socket?.RemoteEndPoint)?.Address;
        public int Port => socket != null ? ((IPEndPoint)socket.RemoteEndPoint).Port : 0;
    }
}
