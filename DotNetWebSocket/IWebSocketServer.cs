using System;
using System.Net.Sockets;

namespace DotNetWebSocket
{
    public interface IWebSocketServer : IDisposable
    {
        void Start();

        void Stop();

        IAsyncResult BeginAccept(AsyncCallback callback, object state);

        Socket EndAccept(IAsyncResult ar);

        IAsyncResult BeginHandshake(Socket socket, AsyncCallback callback, object state);

        WebSocket EndHandshake(IAsyncResult ar);
    }
}