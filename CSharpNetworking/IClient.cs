using System;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public abstract class IClient
    {
        public Action OnOpen;
        public Action<Message> OnMessage;
        public Action OnClose;
        public Action<Exception> OnError;

        public abstract Task Open();
        public abstract Task Close();
        public abstract Task Send(byte[] bytes);
        public abstract Task Send(string message);
    }
}
