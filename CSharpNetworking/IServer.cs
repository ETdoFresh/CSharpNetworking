using System;

namespace CSharpNetworking
{
    public interface IServer<TClient>
    {
        event EventHandler OnListening;
        event EventHandler<TClient> OnAccepted;
        event EventHandler<Message<TClient>> OnMessage;
        event EventHandler<TClient> OnDisconnected;
        event EventHandler OnStopListening;
        event EventHandler<Exception> OnError;
    }
}
