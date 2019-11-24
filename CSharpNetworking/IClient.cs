using System;

namespace CSharpNetworking
{
    public interface IClient
    {
        event EventHandler OnConnected;
        event EventHandler OnMessage;
        event EventHandler OnDisconnected;
        event EventHandler<Exception> OnError;
    }
}
