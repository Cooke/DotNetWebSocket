using System;
using System.Threading;

namespace DotNetWebSocket.Utils
{
    internal class AsyncResultSyncCompletionTracking<T> : IAsyncResult
    {
        private readonly AsyncCallback callback;
        private readonly AsyncResult<T> internalAsyncResult;
        private bool completedSynchronous = true;

        public AsyncResultSyncCompletionTracking(AsyncCallback callback, object state)
        {
            this.callback = callback;
            internalAsyncResult = new AsyncResult<T>(HandleCallback, state);
        }

        public bool IsCompleted
        {
            get { return internalAsyncResult.IsCompleted; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return internalAsyncResult.AsyncWaitHandle; }
        }

        public object AsyncState
        {
            get { return internalAsyncResult.AsyncState; }
        }

        public bool CompletedSynchronously
        {
            get { return internalAsyncResult.CompletedSynchronously; }
        }

        public void HandleNewCompletedSynchronousValue(bool newCompletedSynchronousValue)
        {
            completedSynchronous &= newCompletedSynchronousValue;
        }

        public void SetCompleted(T resultValue)
        {
            internalAsyncResult.SetCompleted(resultValue, completedSynchronous);
        }

        public void SetCompleted(Exception exception)
        {
            internalAsyncResult.SetCompleted(exception, completedSynchronous);
        }

        public T EndInvoke()
        {
            return internalAsyncResult.EndInvoke();
        }

        private void HandleCallback(IAsyncResult ar)
        {
            callback(this);
        }
    }
}