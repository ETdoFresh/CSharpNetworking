using System;

namespace CSharpNetworking
{
    public interface IClient
    {
        event EventHandler OnConnected;
        event EventHandler<Message> OnMessage;
        event EventHandler OnDisconnected;
        event EventHandler<Exception> OnError;
    }
}
