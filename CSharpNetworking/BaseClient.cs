using System;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    public abstract class BaseClient
    {
        public Action Opened;
        public Action<byte[]> Received;
        public Action<byte[]> Sent;
        public Action Closed;
        public Action<Exception> Error;

        public abstract Task OpenAsync();
        public abstract Task CloseAsync();
        public abstract Task SendAsync(byte[] bytes);
        public abstract Task SendAsync(string message);
    }
}
