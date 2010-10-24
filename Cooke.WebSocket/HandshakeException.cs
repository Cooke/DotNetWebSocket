using System;

namespace Cooke.WebSocket
{
    public class HandshakeException : Exception
    {
        public HandshakeException(string message)
            : base(message)
        {
        }        
    }
}
