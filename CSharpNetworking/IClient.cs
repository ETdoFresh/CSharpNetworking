using System;

namespace CSharpNetworking
{
    public interface IClient
    {
        event EventHandler OnOpen;
        event EventHandler<Message> OnMessage;
        event EventHandler OnClose;
        event EventHandler<Exception> OnError;
    }
}
