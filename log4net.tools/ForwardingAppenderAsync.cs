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
    public class ForwardingAppenderAsync : AttachableAppender, IAppender, IOptionHandler, IDisposable
    {
        public int BufferSize { get; set; }
        public FixFlags Fix { get; set; } = FixFlags.Properties | FixFlags.Exception | FixFlags.Message;
        public string Name { get; set; }
        public BufferOverflowBehaviour BufferOverflowBehaviour { get; set; } = BufferOverflowBehaviour.DirectForwarding;

        private static readonly IErrorLogger ErrorLogger = new ErrorTracer();

        private BlockingCollection<LoggingEvent> _buffer;
        private Task _worker;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        public ForwardingAppenderAsync() : base(ErrorLogger)
        {}

        public void DoAppend(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                return;
            }

            loggingEvent.Fix = Fix;

            var errorMessage = "Cannot add the loggingEvent in to the buffer";
            if (BufferSize == 0)
            {
                if (!_buffer.TryAdd(loggingEvent))
                {
                    ErrorLogger.Error(errorMessage);
                }

                return;
            }

            // bounded buffer
            switch (BufferOverflowBehaviour)
            {
                case BufferOverflowBehaviour.RejectNew:
                    if (!_buffer.TryAdd(loggingEvent))
                    {
                        ErrorLogger.Error(errorMessage);
                    }
                    break;
                case BufferOverflowBehaviour.Wait:
                    _buffer.Add(loggingEvent);
                    break;
                case BufferOverflowBehaviour.DirectForwarding:
                    if (!_buffer.TryAdd(loggingEvent))
                    {
                        ErrorLogger.Error(errorMessage + " The direct forwarding is used");
                        AppendLoopOnAppenders(loggingEvent);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Close()
        {
            SwallowHelper.TryDo(() => _cancellation?.Cancel(), ErrorLogger);
            Dispose();
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

        public new void Dispose()
        {
            if (_worker?.IsCanceled == true || _worker?.IsFaulted == true || _worker?.IsCompleted == true)
            {
                SwallowHelper.TryDo(() => _worker?.Dispose(), ErrorLogger);
            }

            SwallowHelper.TryDo(() => _buffer?.Dispose(), ErrorLogger);
            base.Dispose();
        }

        public void ActivateOptions()
        {
            _buffer = BufferSize > 0
                ? new BlockingCollection<LoggingEvent>(BufferSize) // call to Add may block until space is available to store the provided item (https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.blockingcollection-1.add?view=net-5.0#System_Collections_Concurrent_BlockingCollection_1_Add__0_)
                : new BlockingCollection<LoggingEvent>();

            var cancellationToken = _cancellation.Token;
            _worker = Task.Factory
                .StartNew(() => Append(_buffer.GetConsumingEnumerable(cancellationToken), cancellationToken),
                    cancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Current);
        }
    }
}
