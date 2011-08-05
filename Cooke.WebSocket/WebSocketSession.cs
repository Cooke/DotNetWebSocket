using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Cooke.WebSocket.Utils;

namespace Cooke.WebSocket
{
    public class WebSocketSession : IDisposable
    {
        private byte[] inputBuffer;
        private byte[] outputBuffer;
        private int inputBufferOffset;
        private int outputBufferOffset;
        private int outputBufferEndOffset;        
        private Queue<string> result;

        public WebSocketSession(Socket socket)
        {
            Socket = socket;
            result = new Queue<string>();
            inputBuffer = new byte[10 * 1024];
            outputBuffer = new byte[10 * 1024];
            inputBufferOffset = 0;
            outputBufferOffset = 0;
            outputBufferEndOffset = 0;
        }

        internal WebSocketSession(Socket socket, byte[] inputBuffer, byte[] outputBuffer, IDictionary<string, string> cookies, string resource)
        {
            Socket = socket;
            Cookies = cookies;
            Resource = resource;
            result = new Queue<string>();
            this.inputBuffer = inputBuffer;
            this.outputBuffer = outputBuffer;
            inputBufferOffset = 0;
            outputBufferOffset = 0;
            outputBufferEndOffset = 0;
        }

        public Socket Socket { get; private set; }

        public IDictionary<string, string> Cookies { get; private set; }

        public string Resource { get; private set; }

        public int InputBufferSize
        {
            get
            {
                return inputBuffer.Length;
            }
            set
            {
                var tmp = new byte[value];
                Array.Copy(inputBuffer, tmp, Math.Min(inputBufferOffset, value));
                inputBuffer = tmp;
            }
        }

        public int MaxSendMessageLength
        {
            get
            {
                return outputBuffer.Length;
            }
            set
            {
                var tmp = new byte[value];
                Array.Copy(outputBuffer, tmp, Math.Min(outputBufferEndOffset, value));
                inputBuffer = tmp;
            }
        }

        public IAsyncResult BeginReceiveMessage(AsyncCallback asyncCallback, object state)
        {
            var wsAr = new AsyncResultNoReturnValue(asyncCallback, state);

            if (result.Count > 0)
            {
                wsAr.SetCompleted(null, true);
            }
            else
            {
                Socket.BeginReceive(inputBuffer, inputBufferOffset, inputBuffer.Length - inputBufferOffset, SocketFlags.None, ReceiveCallback, wsAr);
            }

            return wsAr;
        }

        public string EndReceiveMessage(IAsyncResult asyncResult)
        {
            ((AsyncResultNoReturnValue)asyncResult).EndInvoke();

            return result.Dequeue();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            int receivedBytes;
            var wsAr = (AsyncResultNoReturnValue)ar.AsyncState;
            int messageStartOffset = 0;

            try
            {
                receivedBytes = Socket.EndReceive(ar);
            }
            catch (Exception ex)
            {
                wsAr.SetCompleted(ex, wsAr.CompletedSynchronously);
                return;
            }
            

            if (receivedBytes == 0)
            {                
                result.Enqueue(null);
                wsAr.SetCompleted(null, ar.CompletedSynchronously);
                return;
            }

            // Search the input buffer for 0xFF byte (which marks 'end of message')
            for (int i = 0; i < receivedBytes; i++)
            {
                if (inputBuffer[inputBufferOffset + i] == 0xFF)
                {
                    if (messageStartOffset == inputBufferOffset + i)
                    {
                        // Beleive that Firefox closes the connection with 0xFF and 0x00, so here I check that 0xFF is received without any 0x00 before
                        result.Enqueue(null);
                        wsAr.SetCompleted(null, ar.CompletedSynchronously);
                        return;
                    }

                    result.Enqueue(Encoding.UTF8.GetString(inputBuffer, messageStartOffset + 1, i - 1));
                    messageStartOffset = i + 1;
                }
            }

            // Store data buffer offset
            inputBufferOffset += receivedBytes;

            // Signal that a message has successfully been received if next message starts at an offset above 0            
            if (messageStartOffset > 0)
            {
                // If there are unprocessed bytes in the buffer then copy them to be beginning of the buffer
                if (receivedBytes - messageStartOffset > 0)
                {
                    Buffer.BlockCopy(inputBuffer, messageStartOffset, inputBuffer, 0, receivedBytes - messageStartOffset);
                }

                // Change data buffer offset
                inputBufferOffset -= messageStartOffset;

                // Signal that a message has successfully been received
                wsAr.SetCompleted(null, false);
            }
            else
            {
                try
                {
                    // Continue receiving until a message has been received
                    Socket.BeginReceive(inputBuffer, inputBufferOffset, inputBuffer.Length - inputBufferOffset, SocketFlags.None, ReceiveCallback, wsAr);
                }
                catch (Exception ex)
                {
                    wsAr.SetCompleted(ex, false);
                }
            }
        }

        public IAsyncResult BeginSendMessage(string message, AsyncCallback asyncCallback, object state = null)
        {
            // Write new message to buffer with proper framing and encoding, and start sending
            WriteSendBuffer(message);
            var wsAr = new AsyncResultNoReturnValue(asyncCallback, state);
            try
            {
                Socket.BeginSend(outputBuffer, outputBufferOffset, outputBufferEndOffset - outputBufferOffset, SocketFlags.None, SendMessageCallback, wsAr);
            }
            catch (Exception ex)
            {
                wsAr.SetCompleted(ex, true);
            }
            
            return wsAr;
        }

        public void EndSendMessage(IAsyncResult asyncResult)
        {
            ((AsyncResultNoReturnValue)asyncResult).EndInvoke();
            outputBufferOffset = 0;
            outputBufferEndOffset = 0;
        }

        private void SendMessageCallback(IAsyncResult ar)
        {
            var wsAr = (AsyncResultNoReturnValue)ar.AsyncState;
            try
            {
                outputBufferOffset += Socket.EndSend(ar);

                // Check if all bytes in the buffer has been sent, otherwise continue sending the rest
                if (outputBufferOffset < outputBufferEndOffset)
                {
                    Socket.BeginSend(outputBuffer, outputBufferOffset, outputBufferEndOffset - outputBufferOffset, SocketFlags.None, SendMessageCallback, wsAr);
                }
                else
                {
                    wsAr.SetCompleted(null, false);
                }
            }
            catch (Exception ex)
            {
                wsAr.SetCompleted(ex, false);
            }
        }

        private void WriteSendBuffer(string message)
        {
            // Frame data with leading 0x00 and trailing 0xFF
            outputBuffer[0] = 0x00;
            // Encode data to UTF-8

            // TODO buffer overflow
            int bytes = Encoding.UTF8.GetBytes(message, 0, message.Length, outputBuffer, 1);
            outputBuffer[bytes + 1] = 0xFF;
            outputBufferOffset = 0;
            outputBufferEndOffset = bytes + 2;
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            // TODO better closing meachanism is needed when the WebSocket API has matured
            Socket.Close(0);            
            inputBuffer = null;
            outputBuffer = null;            
        }

        public void Abort()
        {
            Socket.Close(0);
        }
    }
}
