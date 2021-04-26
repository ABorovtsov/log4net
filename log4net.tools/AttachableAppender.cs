using System;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace log4net.tools
{
    /// <summary>
    /// Implements the IAppenderAttachable behaviour
    /// </summary>
    public class AttachableAppender : IAppenderAttachable, IDisposable
    {
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly IErrorLogger _errorLogger;
        private readonly int _lockTimeoutMs;

        private AppenderAttachedImpl _appenderAttached;

        protected AttachableAppender(IErrorLogger errorLogger, int lockTimeoutMs = 128)
        {
            _errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
            _lockTimeoutMs = lockTimeoutMs;
        }

        public void AddAppender(IAppender newAppender)
        {
            if (newAppender == null)
            {
                throw new ArgumentNullException(nameof(newAppender));
            }

            using (new Locker(_locker, _lockTimeoutMs, _errorLogger, exclusive: true))
            {
                if (_appenderAttached == null)
                {
                    _appenderAttached = new AppenderAttachedImpl();
                }

                _appenderAttached.AddAppender(newAppender);
            }
        }

        public AppenderCollection Appenders
        {
            get
            {
                using (new Locker(_locker, _lockTimeoutMs, _errorLogger))
                {
                    return _appenderAttached == null 
                        ? AppenderCollection.EmptyCollection 
                        : _appenderAttached.Appenders;
                }
            }
        }

        public IAppender GetAppender(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            using (new Locker(_locker, _lockTimeoutMs, _errorLogger))
            {
                return _appenderAttached?.GetAppender(name);
            }
        }

        public void RemoveAllAppenders()
        {
            using (new Locker(_locker, _lockTimeoutMs, _errorLogger, exclusive: true))
            {
                _appenderAttached?.RemoveAllAppenders();
                _appenderAttached = null;
            }
        }

        public IAppender RemoveAppender(IAppender appender)
        {
            if (appender == null)
            {
                return null;
            }

            using (new Locker(_locker, _lockTimeoutMs, _errorLogger, exclusive: true))
            {
                return _appenderAttached?.RemoveAppender(appender);
            }
        }

        public IAppender RemoveAppender(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            using (new Locker(_locker, _lockTimeoutMs, _errorLogger, exclusive: true))
            {
                return _appenderAttached?.RemoveAppender(name);
            }
        }

        protected void AppendLoopOnAppenders(LoggingEvent loggingEvent)
        {
            using (new Locker(_locker, _lockTimeoutMs, _errorLogger))
            {
                _appenderAttached?.AppendLoopOnAppenders(loggingEvent);
            }
        }

        public void Dispose()
        {
            _locker?.Dispose();
        }
    }
}