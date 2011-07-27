using System;
using System.Net;
using System.Net.Sockets;

namespace Cooke.WebSocket
{
    public class ListenerSocketExceptionEventArgs : EventArgs
    {
        public ListenerSocketExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }

        protected Exception Exception { get; private set; }
    }

    public class HandshakeUpdatedEventArgs : EventArgs
    {
        public HandshakeUpdatedEventArgs(IPEndPoint endPoint)
        {
            IPEndPoint = endPoint;            
        }

        protected IPEndPoint IPEndPoint { get; private set; }
    }

    public class HandshakeCompletedEventArgs : EventArgs
    {
        public HandshakeCompletedEventArgs(WebSocketSession socketSession)
        {
            WebSocketSession = socketSession;
        }

        public WebSocketSession WebSocketSession { get; private set; }
    }    

    public class WebSocketListener : IDisposable
    {
        private readonly WebSocketHandshakeManager handshaker = new WebSocketHandshakeManager();
        private readonly TcpListener listener;
        private readonly int simultaniousHandshakes;

        public event EventHandler<HandshakeCompletedEventArgs> HandshakeCompleted;
        public event EventHandler<HandshakeUpdatedEventArgs> SocketAccepted;
        public event EventHandler<HandshakeUpdatedEventArgs> HandshakeFailed;
        public event EventHandler<ListenerSocketExceptionEventArgs> ListenerSocketException;

        public WebSocketListener(IPAddress ipAddress, int port, int simultaniousHandshakes)
        {
            listener = new TcpListener(ipAddress, port);
            this.simultaniousHandshakes = simultaniousHandshakes;
        }

        public void Start()
        {
            listener.Start();

            for (var i = 0; i < simultaniousHandshakes; i++)
            {
                listener.BeginAcceptSocket(HandleAcceptSocket, null);
            }
        }

        public void Stop()
        {
            listener.Stop();
        }

        private void HandleAcceptSocket(IAsyncResult ar)
        {
            try
            {
                var socket = listener.EndAcceptSocket(ar);
                handshaker.BeginHandshake(socket, HandleHandshake, socket);

                // Where should this be? Here or when handshake is finished?
                listener.BeginAcceptSocket(HandleAcceptSocket, null);

                if (SocketAccepted != null)
                {
                    var args = new HandshakeUpdatedEventArgs((IPEndPoint)socket.RemoteEndPoint);
                    SocketAccepted(this, args);
                }
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                if (ListenerSocketException != null)
                {
                    ListenerSocketException(this, new ListenerSocketExceptionEventArgs(ex));
                }
            }
        }

        private void HandleHandshake(IAsyncResult ar)
        {
            var socket = (Socket)ar.AsyncState;

            try
            {
                var webSocket = handshaker.EndHandshake(ar);

                EventHandler<HandshakeCompletedEventArgs> handshakeCompleted = HandshakeCompleted;
                if (handshakeCompleted != null)
                {
                    handshakeCompleted(this, new HandshakeCompletedEventArgs(webSocket));
                }
            }
            //catch (TimeoutException)
            //{
            // Timeout exception will happen when a websocket client compatible to version 75 tries to connect to a web socket server compatible to version 76 (which this server is).");  
            //}
            catch (Exception)
            {
                EventHandler<HandshakeUpdatedEventArgs> handshakeFailed = HandshakeFailed;
                if (handshakeFailed != null)
                {
                    handshakeFailed(this, new HandshakeUpdatedEventArgs((IPEndPoint)socket.RemoteEndPoint));
                }
            }
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}
