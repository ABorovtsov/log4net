using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;

namespace log4net.tools
{
    /// <summary>
    /// Appender forwards LoggingEvents to a list of attached appenders asynchronously
    /// </summary>
    public class ForwardingAppenderAsync : AttachableAppender, IAppender
    {
        public int BufferSize { get; set; }
        public FixFlags Fix { get; set; } = FixFlags.Properties | FixFlags.Exception | FixFlags.Message;
        public string Name { get; set; }

        private static readonly IErrorLogger ErrorLogger = new ErrorTracer();

        private readonly BlockingCollection<LoggingEvent> _queue;
        private readonly Task _worker;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        public ForwardingAppenderAsync() : base(ErrorLogger)
        {
            _queue = BufferSize > 0
                ? new BlockingCollection<LoggingEvent>(BufferSize) // call to Add may block until space is available to store the provided item (https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.blockingcollection-1.add?view=net-5.0#System_Collections_Concurrent_BlockingCollection_1_Add__0_)
                : new BlockingCollection<LoggingEvent>();

            var cancellationToken = _cancellation.Token;
            _worker = Task.Factory
                .StartNew(() => Append(_queue.GetConsumingEnumerable(cancellationToken), cancellationToken),
                    cancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Current);
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
                // here we lose the events
                ErrorLogger.Error("Cannot add the loggingEvent in to the queue");
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
            _cancellation?.Dispose();
        }

        private void Append(IEnumerable<LoggingEvent> loggingEvents, CancellationToken token)
        {
            foreach (LoggingEvent loggingEvent in loggingEvents)
            {
                if (token.IsCancellationRequested)
                {
                    return; // here we lose the events which was not dequeued yet
                }

                AppendLoopOnAppenders(loggingEvent);
            }
        }
    }
}
