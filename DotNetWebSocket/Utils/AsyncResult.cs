using System;

namespace DotNetWebSocket.Utils
{
    internal class AsyncResult<T> : AsyncResultNoReturnValue
    {
        private T result;

        public AsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public new T EndInvoke()
        {
            base.EndInvoke();
            return result;
        }

        public void SetCompleted(T resultValue, bool completedSync)
        {
            result = resultValue;
// ReSharper disable RedundantBaseQualifier
            base.SetCompleted(null, completedSync);
// ReSharper restore RedundantBaseQualifier
        }
    }
}
