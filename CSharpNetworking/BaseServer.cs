using System;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public abstract class BaseServer<TClient>
    {
        public event Action ServerOpened;
        public event Action ServerClosed;
        public event Action<TClient> Opened;
        public event Action<TClient, byte[]> Received;
        public event Action<TClient, byte[]> Sent;
        public event Action<TClient> Closed;
        public event Action<Exception> Error;

        public abstract Task OpenAsync();
        public abstract Task CloseAsync();
        public abstract Task SendAsync(TClient client, byte[] bytes);
        public abstract Task SendAsync(TClient client, string message);
        
        protected void InvokeServerOpenedEvent() => ServerOpened?.Invoke();
        protected void InvokeServerClosedEvent() => ServerClosed?.Invoke();
        protected void InvokeOpenedEvent(TClient client) => Opened?.Invoke(client);
        protected void InvokeReceivedEvent(TClient client, byte[] bytes) => Received?.Invoke(client, bytes);
        protected void InvokeSentEvent(TClient client, byte[] bytes) => Sent?.Invoke(client, bytes);
        protected void InvokeClosedEvent(TClient client) => Closed?.Invoke(client);
        protected void InvokeErrorEvent(Exception exception) => Error?.Invoke(exception);
    }
}
