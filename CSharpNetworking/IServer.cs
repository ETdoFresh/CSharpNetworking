using System;

namespace CSharpNetworking
{
    public interface IServer<TClient>
    {
        event EventHandler OnServerOpen;
        event EventHandler OnServerClose;
        event EventHandler<TClient> OnOpen;
        event EventHandler<Message<TClient>> OnMessage;
        event EventHandler<TClient> OnClose;
        event EventHandler<Exception> OnError;
    }
}
