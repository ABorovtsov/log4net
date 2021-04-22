using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace log4net.tools
{
    /// <summary>
    /// Appender forwards LoggingEvents to a list of attached appenders asynchronously
    /// </summary>
    public class ForwardingAppenderAsync : IAppender, IAppenderAttachable
    {
        public string Name { get; set; }
        public FixFlags Fix { get; set; } = FixFlags.All;
        public int BufferSize { get; set; }

        private const int TakeLockTimeoutMs = 100;
        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly BlockingCollection<LoggingEvent> _queue;
        private readonly Task _worker;
        private AppenderAttachedImpl _appenderAttached;

        public ForwardingAppenderAsync()
        {
            _queue = BufferSize > 0
                ? new BlockingCollection<LoggingEvent>(BufferSize) // call to Add may block until space is available to store the provided item (https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.blockingcollection-1.add?view=net-5.0#System_Collections_Concurrent_BlockingCollection_1_Add__0_)
                : new BlockingCollection<LoggingEvent>();
            _worker = Task.Factory.StartNew(() => Append(_queue.GetConsumingEnumerable()), TaskCreationOptions.LongRunning);
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                return;
            }

            loggingEvent.Fix = Fix;
            if (!_queue.TryAdd(loggingEvent))
            {
                Trace.TraceError("Cannot add the loggingEvent in to the queue");
            }
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

        private void Append(IEnumerable<LoggingEvent> loggingEvents)
        {
            foreach (LoggingEvent loggingEvent in loggingEvents)
            {
                using (new AppenderLocker(Lock, TakeLockTimeoutMs))
                {
                    _appenderAttached?.AppendLoopOnAppenders(loggingEvent);
                }
            }
        }
    }
}
