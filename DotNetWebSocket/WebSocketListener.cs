using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace DotNetWebSocket
{
    public class WebSocketListener : IWebSocketListener
    {
        private readonly ConcurrentQueue<WebSocket> newWebSockets = new ConcurrentQueue<WebSocket>();
        private readonly WebSocketServer server;

        public WebSocketListener(IPAddress listenAddress, int port)
        {
            server = new WebSocketServer(listenAddress, port);
        }

        public event EventHandler<EventArgs> NewClientAvailable;

        public event EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        public void Start()
        {
            server.Start();

            BeginAcceptSocket();
        }

        public void Stop()
        {
            server.Stop();
        }

        public void Dispose()
        {
            server.Dispose();
        }

        public IWebSocketClient GetNewClientOrDefault()
        {
            WebSocket webSocket;
            newWebSockets.TryDequeue(out webSocket);
            return new WebSocketClient(webSocket);
        }

        public IEnumerable<IWebSocketClient> GetNewClients()
        {
            List<IWebSocketClient> newClients = null;

            WebSocket socketSession;
            while (newWebSockets.TryDequeue(out socketSession))
            {
                if (newClients == null)
                {
                    newClients = new List<IWebSocketClient>();
                }

                newClients.Add(new WebSocketClient(socketSession));
            }

            return newClients ?? Enumerable.Empty<IWebSocketClient>();
        }

        private void BeginAcceptSocket()
        {
            try
            {
                server.BeginAccept(HandleAcceptSocketCompleted, null);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                OnUnhandledException(ex);
            }
        }

        private void HandleAcceptSocketCompleted(IAsyncResult ar)
        {
            try
            {
                var socket = server.EndAccept(ar);
                server.BeginHandshake(socket, HandleHandshakeCompleted, null);
                
                BeginAcceptSocket();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
                BeginAcceptSocket();        
            }
            catch (Exception ex)
            {
                OnUnhandledException(ex);                
            }
        }

        private void HandleHandshakeCompleted(IAsyncResult ar)
        {
            try
            {
                var webSocket = server.EndHandshake(ar);
                newWebSockets.Enqueue(webSocket);
                OnNewWebSocketsAvailable();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (HandshakeException)
            {
            }
            catch (Exception ex)
            {
                OnUnhandledException(ex);
            }
        }

        private void OnUnhandledException(Exception ex)
        {
            EventHandler<UnhandledExceptionEventArgs> unhandledException = UnhandledException;
            if (unhandledException != null)
            {
                unhandledException(this, new UnhandledExceptionEventArgs(ex));
            }
        }

        private void OnNewWebSocketsAvailable()
        {
            var newWebSocketsAvailable = NewClientAvailable;
            if (newWebSocketsAvailable != null)
            {
                newWebSocketsAvailable(this, EventArgs.Empty);
            }
        }
    }
}
