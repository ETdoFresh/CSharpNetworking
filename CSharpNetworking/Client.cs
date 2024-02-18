using System;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public abstract class Client
    {
        public event Action Opened;
        public event Action<byte[]> Received;
        public event Action<byte[]> Sent;
        public event Action Closed;
        public event Action<Exception> Error;
        
        public int BufferSize { get; protected set; }

        public abstract Task OpenAsync();
        public abstract void Close();
        public abstract Task SendAsync(byte[] bytes);
        public abstract Task SendAsync(string message);
        
        protected void InvokeOpenedEvent() => Opened?.Invoke();
        protected void InvokeReceivedEvent(byte[] bytes) => Received?.Invoke(bytes);
        protected void InvokeSentEvent(byte[] bytes) => Sent?.Invoke(bytes);
        protected void InvokeClosedEvent() => Closed?.Invoke();
        protected void InvokeErrorEvent(Exception exception) => Error?.Invoke(exception);
    }
}
