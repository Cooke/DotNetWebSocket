using System;
using System.Collections.Generic;

namespace DotNetWebSocket
{
    public interface IWebSocketListener : IDisposable
    {
        event EventHandler<EventArgs> NewClientAvailable;

        event EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        void Start();

        void Stop();

        IEnumerable<IWebSocketClient> GetNewClients();

        IWebSocketClient GetNewClientOrDefault();
    }
}