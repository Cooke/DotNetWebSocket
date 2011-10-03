using System;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using DotNetWebSocket.Utils;

namespace DotNetWebSocket.Handshaking
{
    internal class HandshakeHandler
    {
        private readonly Regex hostnamePattern;
        private readonly Regex originPattern;
        private readonly Regex resourcePattern;
        private ClientHandshake clientHandshake;

        public HandshakeHandler(Regex hostnamePattern, Regex originPattern, Regex resourcePattern)
        {
            this.hostnamePattern = hostnamePattern;
            this.originPattern = originPattern;
            this.resourcePattern = resourcePattern;
        }

        public IAsyncResult BeginHandshake(Socket socket, AsyncCallback callback, object state)
        {
            var handshakeStateContainer = new HandshakeStateContainer
                {
                    HandshakeReceiver = new ClientHandshakeReceiver(socket),
                    AsyncResult = new AsyncResultSyncCompletionTracking<WebSocket>(callback, state)
                };

            handshakeStateContainer.HandshakeReceiver.BeginReceiveHandshake(HandleClientHandshakeReceived, handshakeStateContainer);

            return handshakeStateContainer.AsyncResult;
        }

        public WebSocket EndHandshake(IAsyncResult ar)
        {
            return ((AsyncResultSyncCompletionTracking<WebSocket>)ar).EndInvoke();
        }

        private static void HandleServerHandshakeSent(IAsyncResult ar)
        {
            var handshakeStateContainer = (HandshakeStateContainer)ar.AsyncState;

            handshakeStateContainer.AsyncResult.HandleNewCompletedSynchronousValue(ar.CompletedSynchronously);

            try
            {
                handshakeStateContainer.HandshakeSender.EndSendHandshake(ar);
                var webSocket = new WebSocket(handshakeStateContainer.HandshakeSender.Socket, handshakeStateContainer.ClientHandshake);
                handshakeStateContainer.AsyncResult.SetCompleted(webSocket);
            }
            catch (Exception ex)
            {
                handshakeStateContainer.AsyncResult.SetCompleted(ex);
            }
        }

        private static bool IsValidVersion(int version)
        {
            return true;
        }

        private void HandleClientHandshakeReceived(IAsyncResult ar)
        {
            var handshakeStateContainer = (HandshakeStateContainer)ar.AsyncState;

            handshakeStateContainer.AsyncResult.HandleNewCompletedSynchronousValue(ar.CompletedSynchronously);

            try
            {
                clientHandshake = handshakeStateContainer.HandshakeReceiver.EndReceiveHandshake(ar);
                handshakeStateContainer.ClientHandshake = clientHandshake;

                if (!IsValidOrigin(clientHandshake.Origin))
                {
                    // Send Forbidden 403 
                    handshakeStateContainer.HandshakeReceiver.Socket.Close();
                }
                else if (!IsValidVersion(clientHandshake.Version))
                {
                    // Send Upgrade Required 426 
                    handshakeStateContainer.HandshakeReceiver.Socket.Close();
                }
                else
                {
                    var serverHandshakeSender = new ServerHandshakeSender(handshakeStateContainer.HandshakeReceiver.Socket);
                    handshakeStateContainer.HandshakeSender = serverHandshakeSender;
                    serverHandshakeSender.BeginSendHandshake(new ServerHandshake(clientHandshake.Key), HandleServerHandshakeSent, handshakeStateContainer);
                }
            }
            catch (Exception ex)
            {
                // Send Bad Request 400
                handshakeStateContainer.AsyncResult.SetCompleted(ex);
            }     
        }

        private bool IsValidOrigin(string origin)
        {
            return originPattern.IsMatch(origin);
        }

        private class HandshakeStateContainer
        {
            public ServerHandshakeSender HandshakeSender { get; set; }

            public ClientHandshakeReceiver HandshakeReceiver { get; set; }

            public ClientHandshake ClientHandshake { get; set; }

            public AsyncResultSyncCompletionTracking<WebSocket> AsyncResult { get; set; }
        }
    }
}