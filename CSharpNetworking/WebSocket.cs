using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    class WebSocket
    {
        public enum StreamType { Unsecured, SecuredLocalhost, SecuredRemote }

        private static Random Random { get; } = new Random();

        public static byte[] StringToBytes(string message, bool masked = true)
        {
            var mask = masked ? 0b_1000_0000 : 0;

            List<byte> bytes;

            // Bit description:
            // 0: Is final fragment of message
            // 1-3: Reserved bits. Must be 0.
            // 4-7: Opcode [0: continue, 1: text, 2: binary, 8: close, 9: ping, 10: pong]
            bytes = new List<byte> { 0b_1000_0001 };

            // 8: Maskbit
            // 9-15: Payload Length
            // 16-31: Extended Payload
            // 32-64: Extended Payload 2
            if (message.Length < 126)
            {
                bytes.Add((byte)(message.Length + mask));
            }
            else if (message.Length < 65536)
            {
                bytes.Add((byte)(126 + mask));
                bytes.AddRange(BitConverter.GetBytes((ushort)message.Length).Reverse());
            }
            else if ((ulong)message.Length <= 18446744073709551615)
            {
                bytes.Add((byte)(127 + mask));
                bytes.AddRange(BitConverter.GetBytes((ulong)message.Length).Reverse());
            }

            // +0 or +4: If masked, 4-byte mask
            var maskBytes = new List<byte>();
            if (masked)
            {
                for (int i = 0; i < 4; i++)
                    maskBytes.Add((byte)Random.Next(-128, 128));
                bytes.AddRange(maskBytes);
            }


            // +message.Length: Data!
            var messageBytes = new List<byte>();
            messageBytes.AddRange(Encoding.UTF8.GetBytes(message));

            // Mask data if necessary
            if (masked)
                for (int i = 0; i < message.Length; i++)
                {
                    var j = i % 4;
                    messageBytes[i] = (byte)(messageBytes[i] ^ maskBytes[j]);
                }

            bytes.AddRange(messageBytes);
            return bytes.ToArray();
        }

        public static long PacketLength(IEnumerable<byte> bytes)
        {
            var length = PayloadLength(bytes);
            if (length == long.MaxValue) return length;

            var packetHeader = 2;

            var lengthOffset = 0;
            if (length < 126) lengthOffset = 0;
            else if (length < 65536) lengthOffset = 2;
            else lengthOffset = 8;

            var masked = (bytes.ToArray()[1] & 0b_1000_0000) > 0;
            var maskOffset = masked ? 4 : 0;

            return packetHeader + lengthOffset + maskOffset + length;
        }

        public static long PayloadLength(IEnumerable<byte> bytes)
        {
            if (bytes.Count() < 3)
                return long.MaxValue;

            long length = bytes.ElementAt(1) & 0b_0111_1111;
            var index = 2;

            if (length == 126)
            {
                var shrt = bytes.Skip(index).Take(2).Reverse().ToArray();
                return BitConverter.ToInt16(shrt, 0);
            }
            if (length == 127)
            {
                var lng = bytes.Skip(index).Take(8).Reverse().ToArray();
                return BitConverter.ToInt64(lng, 0);
            }
            else
                return length;
        }

        public static string BytesToString(byte[] bytes)
        {
            var isFinal = (bytes[0] & 0b_1000_0000) > 0;
            var opCode = bytes[0] & 0b_0000_1111;
            var closeConnectionRequested = opCode == 0x08;
            var masked = (bytes[1] & 0b_1000_0000) > 0;
            long length = bytes[1] & 0b_0111_1111;
            var index = 2;
            if (length == 126)
            {
                var shrt = bytes.Skip(index).Take(2).Reverse().ToArray();
                length = BitConverter.ToInt16(shrt, 0);
                index += 2;
            }
            if (length == 127)
            {
                var lng = bytes.Skip(index).Take(8).Reverse().ToArray();
                length = BitConverter.ToInt64(lng, 0);
                index += 8;
            }

            var maskBytes = new List<byte>();
            if (masked)
                for (int i = 0; i < 4; i++)
                    maskBytes.Add(bytes[index + i]);

            index += masked ? 4 : 0;

            // +message.Length: Data!
            var messageBytes = new List<byte>();
            for (int i = 0; i < length; i++)
                messageBytes.Add(bytes[index + i]);

            // Mask data if necessary
            if (masked)
                for (int i = 0; i < length; i++)
                {
                    var j = i % 4;
                    messageBytes[i] = (byte)(messageBytes[i] ^ maskBytes[j]);
                }
            var message = Encoding.UTF8.GetString(messageBytes.ToArray());
            return message;
        }

        public static bool IsDiconnectPacket(IEnumerable<byte> bytes)
        {
            if (bytes.Count() == 0) return true;
            var opCode = bytes.ElementAt(0) & 0b_0000_1111;
            var closeConnectionRequested = opCode == 0x08;
            return closeConnectionRequested;
        }

        public static Stream GetNetworkStream(Socket socket, Uri uri)
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
                        Console.WriteLine($"Reading certificate from {path}");
                        bytes = File.ReadAllBytes(path);
                        serverCertificate = new X509Certificate2(bytes, "1234");
                        Console.WriteLine("Certificate Read [localhost]...");
                        sslStream = new SslStream(networkStream);
                        sslStream.AuthenticateAsServer(serverCertificate,
                                enabledSslProtocols: SslProtocols.Tls,
                                clientCertificateRequired: false,
                                checkCertificateRevocation: false);
                        return sslStream;
                    case StreamType.SecuredRemote:
                        path = Path.Combine(currentDirectory, "letsencrypt.pfx");
                        Console.WriteLine($"Reading certificate from {path}");
                        bytes = File.ReadAllBytes(path);
                        serverCertificate = new X509Certificate2(bytes, "1234");
                        Console.WriteLine("Certificate Read [letsencrypt]...");
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
