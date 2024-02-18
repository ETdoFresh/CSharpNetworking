using System;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public abstract class Server<TClient>
    {
        public event Action ServerOpened;
        public event Action ServerClosed;
        public event Action<Exception> ServerError;
        public event Action<TClient> ClientConnected;
        public event Action<TClient> ClientDisconnected;
        public event Action<TClient, byte[]> ClientReceived;
        public event Action<TClient, byte[]> ClientSent;
        public event Action<TClient, Exception> ClientError;

        public int BufferSize { get; protected set; }
        
        public abstract Task OpenAsync();
        public abstract void Close();
        public abstract Task SendAsync(TClient client, byte[] bytes);
        public abstract Task SendAsync(TClient client, string message);
        
        protected void InvokeServerOpenedEvent() => ServerOpened?.Invoke();
        protected void InvokeServerClosedEvent() => ServerClosed?.Invoke();
        protected void InvokeServerErrorEvent(Exception exception) => ServerError?.Invoke(exception);
        protected void InvokeOpenedEvent(TClient client) => ClientConnected?.Invoke(client);
        protected void InvokeReceivedEvent(TClient client, byte[] bytes) => ClientReceived?.Invoke(client, bytes);
        protected void InvokeSentEvent(TClient client, byte[] bytes) => ClientSent?.Invoke(client, bytes);
        protected void InvokeClosedEvent(TClient client) => ClientDisconnected?.Invoke(client);
        protected void InvokeClientErrorEvent(TClient client, Exception exception) => ClientError?.Invoke(client, exception);
    }
}
