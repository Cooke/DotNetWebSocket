using System;
using System.Threading;

namespace DotNetWebSocket.Utils
{
    internal class AsyncResultNoReturnValue : IAsyncResult
    {
        private readonly object asyncState;
        private readonly AsyncCallback asyncCallback;

        private ManualResetEvent asyncWaitHandle;
        private Exception exception;
        private bool completedSynchronously;
        private bool isCompleted;

        public AsyncResultNoReturnValue(AsyncCallback callback, object state)
        {
            asyncState = state;
            asyncCallback = callback;
            completedSynchronously = false;
            isCompleted = false;
        }

        public object AsyncState
        {
            get { return asyncState; }
        }

        public bool CompletedSynchronously
        {
            get { return completedSynchronously; }
        }

        public bool IsCompleted
        {
            get { return isCompleted; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (asyncWaitHandle == null)
                {
                    bool done = isCompleted;
                    var preliminaryWaitHandle = new ManualResetEvent(done);

                    if (Interlocked.CompareExchange(ref asyncWaitHandle, preliminaryWaitHandle, null) != null)
                    {
                        preliminaryWaitHandle.Close();
                    }
                    else
                    {
                        if (!done && IsCompleted)
                        {
                            asyncWaitHandle.Set();
                        }
                    }
                }

                return asyncWaitHandle;
            }
        }

        public AsyncCallback AsyncCallback
        {
            get
            {
                return asyncCallback;
            }
        }

        public void SetCompleted(Exception ex, bool completedSynch)
        {
            exception = ex;
            completedSynchronously = completedSynch;

            if (isCompleted)
            {
                throw new InvalidOperationException("SetCompleted can only be called once");
            }

            isCompleted = true;

            if (asyncWaitHandle != null)
            {
                asyncWaitHandle.Set();
            }

            if (asyncCallback != null)
            {
                asyncCallback(this);
            }
        }

        public void EndInvoke()
        {
            if (!isCompleted)
            {
                AsyncWaitHandle.WaitOne();
                AsyncWaitHandle.Close();
                asyncWaitHandle = null;
            }

            if (exception != null)
            {
                throw exception;
            }
        }
    }
}
