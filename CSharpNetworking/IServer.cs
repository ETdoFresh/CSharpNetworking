using System;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public abstract class IServer<TClient>
    {
        public Action OnServerOpen;
        public Action OnServerClose;
        public Action<TClient> OnOpen;
        public Action<Message<TClient>> OnMessage;
        public Action<TClient> OnClose;
        public Action<Exception> OnError;

        public abstract Task Open();
        public abstract Task Close();
        public abstract Task Send(TClient client, byte[] bytes);
        public abstract Task Send(TClient client, string message);
    }
}
