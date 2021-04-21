using System;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using Phreesia.Common.Web.Redis;

namespace log4net.tools
{
    /// <summary>
    /// Appender forwards LoggingEvents to a list of attached appenders asynchronously
    /// </summary>
    public class ForwardingAppenderAsync : IAppender, IAppenderAttachable
    {
        public string Name { get; set; }
        public FixFlags Fix { get; set; } = FixFlags.All;
        public IQueue EventQueue { get; set; }

        private const int TakeLockTimeoutMs = 100;
        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private AppenderAttachedImpl _appenderAttached;

        public ForwardingAppenderAsync()
        {
            EventQueue = new LoggingEventQueue(Append);
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                return;
            }

            loggingEvent.Fix = Fix;
            EventQueue.Enqueue(loggingEvent);
        }

        public void AddAppender(IAppender newAppender)
        {
            if (newAppender == null)
            {
                throw new ArgumentNullException(nameof(newAppender));
            }

            using (new AppenderLocker(Lock, TakeLockTimeoutMs, exclusive: true))
            {
                if (_appenderAttached == null)
                {
                    _appenderAttached = new AppenderAttachedImpl();
                }

                _appenderAttached.AddAppender(newAppender);
            }
        }

        public void Close()
        {
            RemoveAllAppenders();
        }

        public AppenderCollection Appenders
        {
            get
            {
                using (new AppenderLocker(Lock, TakeLockTimeoutMs))
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

            using (new AppenderLocker(Lock, TakeLockTimeoutMs))
            {
                return _appenderAttached?.GetAppender(name);
            }
        }

        public void RemoveAllAppenders()
        {
            using (new AppenderLocker(Lock, TakeLockTimeoutMs, exclusive: true))
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

            using (new AppenderLocker(Lock, TakeLockTimeoutMs, exclusive: true))
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

            using (new AppenderLocker(Lock, TakeLockTimeoutMs, exclusive: true))
            {
                return _appenderAttached?.RemoveAppender(name);
            }
        }

        private void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                return;
            }

            using (new AppenderLocker(Lock, TakeLockTimeoutMs))
            {
                _appenderAttached?.AppendLoopOnAppenders(loggingEvent);
            }
        }
    }
}
