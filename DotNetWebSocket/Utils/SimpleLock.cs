using System;
using System.Threading;

namespace DotNetWebSocket.Utils
{
    internal class SimpleLock
    {
        private int simpleLock;

        public bool Locked
        {
            get { return simpleLock == 1; }
        }

        public bool TryAcquire()
        {
            return Interlocked.Exchange(ref simpleLock, 1) == 0;
        }

        public void Release()
        {
            if (simpleLock == 0)
            {
                throw new InvalidOperationException("Cannot call release on a simple lock that hasn't been acquired");
            }

            simpleLock = 0;
        }
    }
}