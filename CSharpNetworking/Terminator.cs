using System.Text;

namespace CSharpNetworking
{
    internal class Terminator
    {
        public const string VALUE = "\r\n";
        public const string HTTP = "\r\n\r\n";
        public const string CONSOLE = "{\\r\\n}";
        public static readonly byte[] VALUE_BYTES = Encoding.UTF8.GetBytes(VALUE);
        public static readonly byte[] HTTP_BYTES = Encoding.UTF8.GetBytes(HTTP);
    }
}
