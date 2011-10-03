using System;

namespace DotNetWebSocket
{
    public class UnhandledExceptionEventArgs : EventArgs
    {
        public UnhandledExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }

        protected Exception Exception { get; private set; }
    }
}