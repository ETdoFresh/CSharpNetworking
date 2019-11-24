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

        void Close();
        void Send(TClient client, byte[] bytes);
        void Send(TClient client, string message);
    }
}
