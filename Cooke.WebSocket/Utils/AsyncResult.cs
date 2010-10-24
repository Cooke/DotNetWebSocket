using System;

namespace Cooke.WebSocket.Utils
{
    internal class AsyncResult<T> : AsyncResultNoReturnValue
    {
        private T result;

        public AsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public void SetCompleted(T resultValue, bool completedSync)
        {
            result = resultValue;
// ReSharper disable RedundantBaseQualifier
            base.SetCompleted(null, completedSync);
// ReSharper restore RedundantBaseQualifier
        }

        new public T EndInvoke()
        {
            base.EndInvoke();
            return result;
        }

    }
}
