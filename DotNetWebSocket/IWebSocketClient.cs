using System;
using System.Collections.Generic;

namespace DotNetWebSocket
{
    public interface IWebSocketClient : IDisposable
    {
        event EventHandler<EventArgs> ConnectionClosed;
        event EventHandler<EventArgs> ConnectionAborted;
        event EventHandler<EventArgs> NewMessagesAvailable;
        bool Connected { get; }
        IRequest Request { get; }

        void Close();

        void SendMessage(string message);

        string GetNewMessageOrDefault();

        IEnumerable<string> GetNewMessages();
    }
}