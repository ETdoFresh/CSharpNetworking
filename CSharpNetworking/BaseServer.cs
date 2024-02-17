using System;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public abstract class BaseServer<TClient>
    {
        public Action ServerOpened;
        public Action ServerClosed;
        public Action<TClient> Opened;
        public Action<Message<TClient>> ReceivedMessage;
        public Action<TClient> Closed;
        public Action<Exception> Error;

        public abstract Task OpenAsync();
        public abstract Task CloseAsync();
        public abstract Task SendAsync(TClient client, byte[] bytes);
        public abstract Task SendAsync(TClient client, string message);
    }
}
