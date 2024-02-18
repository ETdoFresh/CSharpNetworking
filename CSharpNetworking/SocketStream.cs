using System.IO;
using System.Net.Sockets;

namespace CSharpNetworking
{
    public class SocketStream
    {
        public Stream Stream { get; }
        public Socket Socket { get; }

        public string RemoteEndPoint => Socket.RemoteEndPoint.ToString();

        public SocketStream(Socket socket, Stream stream)
        {
            Socket = socket;
            Stream = stream;
        }
    }
}
