using System;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public abstract class BaseClient
    {
        public event Action Opened;
        public event Action<byte[]> Received;
        public event Action<byte[]> Sent;
        public event Action Closed;
        public event Action<Exception> Error;

        public abstract Task OpenAsync();
        public abstract Task CloseAsync();
        public abstract Task SendAsync(byte[] bytes);
        public abstract Task SendAsync(string message);
        
        protected void InvokeOpenedEvent() => Opened?.Invoke();
        protected void InvokeReceivedEvent(byte[] bytes) => Received?.Invoke(bytes);
        protected void InvokeSentEvent(byte[] bytes) => Sent?.Invoke(bytes);
        protected void InvokeClosedEvent() => Closed?.Invoke();
        protected void InvokeErrorEvent(Exception exception) => Error?.Invoke(exception);
    }
}
