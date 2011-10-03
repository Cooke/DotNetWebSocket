using System;
using System.Net.Sockets;

namespace DotNetWebSocket
{
    public interface IWebSocket : IDisposable
    {
        Socket Socket { get; }

        IRequest Request { get; }

        bool Connected { get; }

        IAsyncResult BeginReceiveMessage(AsyncCallback asyncCallback, object state);

        string EndReceiveMessage(IAsyncResult asyncResult);

        void Close();

        void Abort();

        IAsyncResult BeginSendMessage(string message, AsyncCallback asyncCallback, object state);

        void EndSendMessage(IAsyncResult asyncResult);
    }
}