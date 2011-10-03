using System;

namespace DotNetWebSocket
{
    public class HandshakeException : WebSocketException
    {
        public HandshakeException(string message)
            : base(message)
        {
        }

        public HandshakeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}