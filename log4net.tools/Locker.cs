namespace log4net.tools
{
    public readonly struct Locker: IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        private readonly bool _exclusive;

        public Locker(ReaderWriterLockSlim @lock, int timeoutMs, IErrorLogger errorLogger, bool exclusive = false)
        {
            _lock = @lock;
            _exclusive = exclusive;

            if (_exclusive)
            {
                if (!_lock.TryEnterWriteLock(timeoutMs))
                {
                    errorLogger.Error("Cannot take the write lock");
                }

                return;
            }

            if (!_lock.TryEnterReadLock(timeoutMs))
            {
                errorLogger.Error("Cannot take the read lock");
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
