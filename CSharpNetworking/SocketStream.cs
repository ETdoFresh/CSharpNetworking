using System.IO;
using System.Net;
using System.Net.Sockets;

namespace CSharpNetworking
{
    public class SocketStream
    {
        public Stream Stream { get; }
        public Socket Socket { get; }

        public EndPoint RemoteEndPoint => Socket.RemoteEndPoint;

        public SocketStream(Socket socket, Stream stream)
        {
            Socket = socket;
            Stream = stream;
        }
    }
}
