using System.Text;

namespace NetworkingInterface
{
    public class Message
    {
        public byte[] rawData;
        public string data;

        public Message(byte[] rawData)
        {
            this.rawData = rawData;
            data = Encoding.UTF8.GetString(rawData);
        }

        public Message(string data)
        {
            this.data = data;
            rawData = Encoding.UTF8.GetBytes(data);
        }
    }

    public class Message<TClient> : Message
    {
        public TClient client;

        public Message(TClient client, byte[] rawData) : base(rawData)
        {
            this.client = client;
        }

        public Message(TClient client, string data) : base(data)
        {
            this.client = client;
        }
    }
}
