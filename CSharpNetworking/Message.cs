using System.Text;

namespace CSharpNetworking
{
    public class Message
    {
        public byte[] bytes;
        public string data;

        public Message(byte[] bytes)
        {
            this.bytes = bytes;
            data = Encoding.UTF8.GetString(bytes);
        }

        public Message(string data)
        {
            this.data = data;
            bytes = Encoding.UTF8.GetBytes(data);
        }

        public override string ToString() => data;
    }

    public class Message<TClient> : Message
    {
        public TClient client;

        public Message(TClient client, byte[] bytes) : base(bytes)
        {
            this.client = client;
        }

        public Message(TClient client, string data) : base(data)
        {
            this.client = client;
        }
    }
}
