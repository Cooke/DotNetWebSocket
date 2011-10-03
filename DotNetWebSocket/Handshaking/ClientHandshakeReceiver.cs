using System;
using System.Net.Sockets;
using System.Text;
using DotNetWebSocket.Utils;

namespace DotNetWebSocket.Handshaking
{
    internal class ClientHandshakeReceiver
    {
        private readonly byte[] receiveBuffer = new byte[1024];
        private readonly Socket socket;
        private AsyncResult<ClientHandshake> asyncResult;
        private int receiveBufferOffset;
        private int lastReceivedSize;
        private bool completedAsync = true;

        public ClientHandshakeReceiver(Socket socket)
        {
            this.socket = socket;
        }

        public Socket Socket
        {
            get { return socket; }
        }

        private bool IsClientHandshakeComplete
        {
            get
            {
                return receiveBufferOffset > 3 && Encoding.ASCII.GetString(receiveBuffer, receiveBufferOffset - 4, 4) == "\r\n\r\n";
            }
        }

        public IAsyncResult BeginReceiveHandshake(AsyncCallback asyncCallback, object state)
        {
            if (asyncResult != null)
            {
                throw new InvalidOperationException("Can only receive a client handshake once per " + GetType().Name + " instance");
            }

            asyncResult = new AsyncResult<ClientHandshake>(asyncCallback, state);

            BeginReceive();

            return asyncResult;
        }

        public ClientHandshake EndReceiveHandshake(IAsyncResult ar)
        {
            if (ar != asyncResult)
            {
                throw new ArgumentException("The given async result is not valid in this context");
            }

            return asyncResult.EndInvoke();
        }

        private void BeginReceive()
        {
            try
            {
                Socket.BeginReceive(
                    receiveBuffer,
                    receiveBufferOffset,
                    receiveBuffer.Length - receiveBufferOffset,
                    SocketFlags.None,
                    HandleReceiveCompleted,
                    null);
            }
            catch (Exception ex)
            {
                asyncResult.SetCompleted(ex, completedAsync);
            }            
        }

        private void HandleReceiveCompleted(IAsyncResult ar)
        {
            completedAsync &= ar.CompletedSynchronously;

            try
            {
                lastReceivedSize = Socket.EndReceive(ar);
            }
            catch (Exception ex)
            {
                asyncResult.SetCompleted(ex, completedAsync);
                return;
            }
            
            receiveBufferOffset += lastReceivedSize;

            if (lastReceivedSize == 0)
            {
                asyncResult.SetCompleted(new HandshakeException("Remote peer gracefully shutdown the tcp connection during while sending the handshake"), completedAsync);
            }
            else if (receiveBufferOffset == receiveBuffer.Length)
            {
                asyncResult.SetCompleted(new HandshakeException("Receive buffer overflowed before the entire client handshake was received"), completedAsync);
            }
            else if (IsClientHandshakeComplete)
            {
                ReturnClientHandshake();
            }
            else
            {
                BeginReceive();
            }
        }

        private void ReturnClientHandshake()
        {
            ClientHandshake clientHandshake;

            try
            {
                clientHandshake = ClientHandshake.Parse(receiveBuffer, receiveBufferOffset);
            }
            catch (Exception ex)
            {
                asyncResult.SetCompleted(ex, completedAsync);
                return;
            }

            asyncResult.SetCompleted(clientHandshake, completedAsync);
        }
    }
}