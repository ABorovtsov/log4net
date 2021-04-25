using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public int BufferSize { get; set; }
        public IErrorLogger ErrorLogger { get; set; } = new ErrorTracer();
        public FixFlags Fix { get; set; } = FixFlags.All;
        public string Name { get; set; }

        private const int TakeLockTimeoutMs = 100;

        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        
        private readonly BlockingCollection<LoggingEvent> _queue;
        private readonly Task _worker;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        private AppenderAttachedImpl _appenderAttached;

        public ForwardingAppenderAsync()
        {
            _queue = BufferSize > 0
                ? new BlockingCollection<LoggingEvent>(BufferSize) // call to Add may block until space is available to store the provided item (https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.blockingcollection-1.add?view=net-5.0#System_Collections_Concurrent_BlockingCollection_1_Add__0_)
                : new BlockingCollection<LoggingEvent>();

            var cancellationToken = _cancellation.Token;
            _worker = Task.Factory
                .StartNew(() => Append(_queue.GetConsumingEnumerable(cancellationToken), cancellationToken), 
                    cancellationToken, 
                    TaskCreationOptions.LongRunning, 
                    TaskScheduler.Default);
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
                ErrorLogger.Error("Cannot add the loggingEvent in to the queue");
            }
        }

        public void AddAppender(IAppender newAppender)
        {
            if (newAppender == null)
            {
                throw new ArgumentNullException(nameof(newAppender));
            }

            using (new Locker(Lock, TakeLockTimeoutMs, ErrorLogger, exclusive: true))
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

            try
            {
                _cancellation?.Cancel();
            }
            catch (ObjectDisposedException e)
            {
            }

            if (_worker.IsCanceled || _worker.IsFaulted || _worker.IsCompleted)
            {
                _worker?.Dispose();
            }
            
            _queue?.Dispose();
            Lock?.Dispose();
            _cancellation?.Dispose();
        }

        public AppenderCollection Appenders
        {
            get
            {
                using (new Locker(Lock, TakeLockTimeoutMs, ErrorLogger))
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

            using (new Locker(Lock, TakeLockTimeoutMs, ErrorLogger))
            {
                return _appenderAttached?.GetAppender(name);
            }
        }

        public void RemoveAllAppenders()
        {
            using (new Locker(Lock, TakeLockTimeoutMs, ErrorLogger, exclusive: true))
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

            using (new Locker(Lock, TakeLockTimeoutMs, ErrorLogger, exclusive: true))
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

            using (new Locker(Lock, TakeLockTimeoutMs, ErrorLogger, exclusive: true))
            {
                return _appenderAttached?.RemoveAppender(name);
            }
        }

        private void Append(IEnumerable<LoggingEvent> loggingEvents, CancellationToken token)
        {
            foreach (LoggingEvent loggingEvent in loggingEvents)
            {
                if (token.IsCancellationRequested)
                {
                    return; // here we lose the events which was not dequeued yet
                }

                using (new Locker(Lock, TakeLockTimeoutMs, ErrorLogger))
                {
                    _appenderAttached?.AppendLoopOnAppenders(loggingEvent);
                }
            }
        }
    }
}
