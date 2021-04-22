using System;
using System.Diagnostics;
using System.Threading;

namespace log4net.tools
{
    public struct AppenderLocker: IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        private readonly bool _exclusive;

        public AppenderLocker(ReaderWriterLockSlim @lock, bool exclusive = false)
        {
            _lock = @lock;
            _exclusive = exclusive;

            if (_exclusive)
            {
                _lock.EnterWriteLock();
                return;
            }

            _lock.EnterReadLock();
        }

        public AppenderLocker(ReaderWriterLockSlim @lock, int timeoutMs, bool exclusive = false)
        {
            _lock = @lock;
            _exclusive = exclusive;

            if (_exclusive)
            {
                if (!_lock.TryEnterWriteLock(timeoutMs))
                {
                    Trace.TraceError("Cannot take the write lock");
                }

                return;
            }

            if (!_lock.TryEnterReadLock(timeoutMs))
            {
                Trace.TraceError("Cannot take the read lock");
            }
        }

        public void Dispose()
        {

            if (_exclusive)
            {
                _lock?.ExitWriteLock();
                return;
            }

            _lock?.ExitReadLock();
        }
    }
}
