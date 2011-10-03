using System;
using System.Collections.Generic;
using System.Net.Sockets;
using DotNetWebSocket.Framing;
using DotNetWebSocket.Handshaking;
using DotNetWebSocket.Utils;

namespace DotNetWebSocket
{
    public class WebSocket : IWebSocket
    {
        private readonly Socket socket;
        private readonly Queue<string> receivedMessagesQueue = new Queue<string>();
        private readonly Request request;
        private byte[] receiveBuffer;
        private byte[] outputBuffer;
        private int receiveBufferOffset;
        private int sendBufferOffset;
        private int sendBufferSize;

        internal WebSocket(Socket socket, ClientHandshake clientHandshake)
        {
            this.socket = socket;
            receiveBuffer = new byte[10 * 1024];
            outputBuffer = new byte[10 * 1024];
            receiveBufferOffset = 0;
            sendBufferOffset = 0;
            sendBufferSize = 0;

            request = new Request(clientHandshake.Resource, clientHandshake.Cookies, clientHandshake.AllFields);
        }

        public IRequest Request
        {
            get { return request; }
        }

        public bool Connected
        {
            get { return socket.Connected; }
        }

        public Socket Socket
        {
            get { return socket; }
        }

        public IAsyncResult BeginReceiveMessage(AsyncCallback asyncCallback, object state)
        {
            var asyncResult = new AsyncResult<string>(asyncCallback, state);

            ReturnMessageOrBeginReceive(asyncResult, true);

            return asyncResult;
        }

        public string EndReceiveMessage(IAsyncResult asyncResult)
        {
            return ((AsyncResult<string>)asyncResult).EndInvoke();
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            // TODO better closing meachanism is needed when the WebSocket API has matured
            socket.Close(0);
            receiveBuffer = null;
            outputBuffer = null;
        }

        public void Abort()
        {
            socket.Close(0);
        }

        public IAsyncResult BeginSendMessage(string message, AsyncCallback asyncCallback, object state)
        {
            WriteSendBuffer(message);

            var asyncResult = new AsyncResultNoReturnValue(asyncCallback, state);

            socket.BeginSend(outputBuffer, sendBufferOffset, sendBufferSize - sendBufferOffset, SocketFlags.None, HandleSendCompleted, asyncResult);

            return asyncResult;
        }

        public void EndSendMessage(IAsyncResult asyncResult)
        {
            ((AsyncResultNoReturnValue)asyncResult).EndInvoke();
        }

        private void HandleSendCompleted(IAsyncResult ar)
        {
            var asyncResult = (AsyncResultNoReturnValue)ar.AsyncState;
            try
            {
                sendBufferOffset += socket.EndSend(ar);

                if (sendBufferOffset < sendBufferSize)
                {
                    socket.BeginSend(outputBuffer, sendBufferOffset, sendBufferSize - sendBufferOffset, SocketFlags.None, HandleSendCompleted, asyncResult);
                }
                else
                {
                    asyncResult.SetCompleted(null, false);
                }
            }
            catch (Exception ex)
            {
                asyncResult.SetCompleted(ex, false);
            }
        }

        private void WriteSendBuffer(string message)
        {
            var dataFrame = new DataFrame(message);
            int frameSize;
            dataFrame.WriteTo(outputBuffer, out frameSize);
            sendBufferOffset = 0;
            sendBufferSize = frameSize;
        }

        private void HandleReceiveCompleted(IAsyncResult ar)
        {
            var asyncResult = (AsyncResult<string>)ar.AsyncState;

            try
            {
                var receivedBytes = socket.EndReceive(ar);
                receiveBufferOffset += receivedBytes;

                ExtractMessagesFromReceiveBuffer();

                if (receivedBytes == 0)
                {
                    receivedMessagesQueue.Enqueue(null);
                }

                if (receiveBufferOffset >= receiveBuffer.Length)
                {
                    throw new WebSocketException("Receive buffer overflow. The received message was to large to fit in the internal receive buffer.");
                }

                // Note completedSynchronously could be wrong here if the callback is called sync
                ReturnMessageOrBeginReceive(asyncResult, false);
            }
            catch (Exception ex)
            {
                asyncResult.SetCompleted(ex, asyncResult.CompletedSynchronously);
            }
        }

        private void ExtractMessagesFromReceiveBuffer()
        {
            DataFrame frame;
            int usedBytes;
            while (DataFrame.TryReadFrom(receiveBuffer, receiveBufferOffset, out frame, out usedBytes))
            {
                receivedMessagesQueue.Enqueue(frame.Message);
                Buffer.BlockCopy(receiveBuffer, usedBytes, receiveBuffer, 0, usedBytes);
                receiveBufferOffset = 0;
            }
        }

        private void ReturnMessageOrBeginReceive(AsyncResult<string> asyncResult, bool completedSynchronously)
        {
            if (receivedMessagesQueue.Count > 0)
            {
                asyncResult.SetCompleted(receivedMessagesQueue.Dequeue(), completedSynchronously);
            }
            else
            {
                socket.BeginReceive(receiveBuffer, receiveBufferOffset, receiveBuffer.Length - receiveBufferOffset, SocketFlags.None, HandleReceiveCompleted, asyncResult);
            }
        }
    }
}
