using System;
using System.Net.Sockets;
using DotNetWebSocket.Utils;

namespace DotNetWebSocket.Handshaking
{
    internal class ServerHandshakeSender
    {
        private readonly Socket socket;
        private AsyncResultNoReturnValue asyncResult;
        private byte[] sendBuffer;
        private int sendBufferOffset;
        private bool completedAsync = true;

        public ServerHandshakeSender(Socket socket)
        {
            this.socket = socket;
        }

        public Socket Socket
        {
            get { return socket; }
        }

        public IAsyncResult BeginSendHandshake(ServerHandshake handshake, AsyncCallback asyncCallback, object state)
        {
            return DoBeginSend(handshake.ToByteArray(), asyncCallback, state);
        }

        public void EndSendHandshake(IAsyncResult ar)
        {
            if (ar != asyncResult)
            {
                throw new ArgumentException("The given async result is not valid in this context");
            }

            asyncResult.EndInvoke();
        }

        private IAsyncResult DoBeginSend(byte[] bytesToSend, AsyncCallback asyncCallback, object state)
        {
            if (asyncResult != null)
            {
                throw new InvalidOperationException("Can only send a server handshake once per " + GetType().Name + " instance");
            }

            asyncResult = new AsyncResultNoReturnValue(asyncCallback, state);

            sendBuffer = bytesToSend;
            socket.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, HandleSendCompleted, null);

            return asyncResult;
        }

        private void HandleSendCompleted(IAsyncResult ar)
        {
            completedAsync &= ar.CompletedSynchronously;

            try
            {
                var sentBytes = socket.EndSend(ar);
                sendBufferOffset += sentBytes;

                if (sendBufferOffset < sendBuffer.Length)
                {
                    socket.BeginSend(sendBuffer, sendBufferOffset, sendBuffer.Length - sendBufferOffset, SocketFlags.None, HandleSendCompleted, null);
                }
                else
                {
                    asyncResult.SetCompleted(null, completedAsync);
                }
            }
            catch (Exception ex)
            {
                asyncResult.SetCompleted(ex, completedAsync);
            }
        }
    }
}