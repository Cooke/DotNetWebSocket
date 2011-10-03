using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using DotNetWebSocket.Handshaking;

namespace DotNetWebSocket
{
    public class WebSocketServer : IWebSocketServer
    {
        private readonly TcpListener listener;
        private readonly HandshakeHandler handshakeHandler;
        private readonly Regex resourcePattern;
        private readonly Regex originPattern;
        private readonly Regex hostnamePattern;

        public WebSocketServer(IPAddress listenAddress, int port)
        {
            listener = new TcpListener(listenAddress, port);
            resourcePattern = new Regex(string.Empty);
            originPattern = new Regex(string.Empty);
            hostnamePattern = new Regex(string.Empty);
            handshakeHandler = new HandshakeHandler(hostnamePattern, originPattern, resourcePattern);
        }

        public void Start()
        {
            listener.Start();
        }

        public void Stop()
        {
            try
            {
                listener.Stop();
            }
            catch (SocketException)
            {
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public IAsyncResult BeginAccept(AsyncCallback callback, object state)
        {
            return listener.BeginAcceptSocket(callback, state);
        }

        public Socket EndAccept(IAsyncResult ar)
        {
            return listener.EndAcceptSocket(ar);
        }

        public IAsyncResult BeginHandshake(Socket socket, AsyncCallback callback, object state)
        {
            return handshakeHandler.BeginHandshake(socket, callback, state);
        }

        public WebSocket EndHandshake(IAsyncResult ar)
        {
            return handshakeHandler.EndHandshake(ar);
        }
    }
}
