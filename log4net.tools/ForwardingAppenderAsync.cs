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
    /// The appender forwards LoggingEvents to a list of attached appenders asynchronously
    /// </summary>
    public class ForwardingAppenderAsync : AttachableAppender, IAppender, IOptionHandler, IDisposable
    {
        public int BufferSize { get; set; }
        public FixFlags Fix { get; set; } = FixFlags.Properties | FixFlags.Exception | FixFlags.Message;
        public string Name { get; set; }
        public BufferOverflowBehaviour BufferOverflowBehaviour { get; set; } = BufferOverflowBehaviour.DirectForwarding;
        public BufferClosingType BufferClosingType { get; set; } = BufferClosingType.Immediate;

        protected BlockingCollection<LoggingEvent> Buffer;

        private static readonly IErrorLogger ErrorLogger = new ErrorTracer();

        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private Task _worker;

        public ForwardingAppenderAsync() : base(ErrorLogger)
        { }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                return;
            }

            loggingEvent.Fix = Fix;

            try
            {
                if (BufferSize == 0)
                {
                    if (!Buffer.TryAdd(loggingEvent))
                    {
                        ErrorLogger.Error("Cannot add the loggingEvent in to the buffer");
                    }

                    return;
                }

                DoAppendBoundedBuffer(loggingEvent);
            }
            catch (Exception ex)
            {
                ErrorLogger.Error(ex.ToString());
            }
        }

        public void Close()
        {
            SwallowHelper.TryDo(() => Buffer?.CompleteAdding(), ErrorLogger);
            SwallowHelper.TryDo(() => _cancellation?.Cancel(), ErrorLogger);
            SwallowHelper.TryDo(() =>
            {
                var bufferedEventCount = Buffer?.Count ?? 0;
                if (bufferedEventCount > 0)
                {
                    ErrorLogger.Error($"There are {bufferedEventCount} LoggingEvents which are not logged yet at the moment of closing the appender");
                    CloseBuffer();
                }
            }, ErrorLogger);

            Dispose();
        }

        public new void Dispose()
        {
            if (_worker?.IsCanceled == true || _worker?.IsFaulted == true || _worker?.IsCompleted == true)
            {
                SwallowHelper.TryDo(() => _worker?.Dispose(), ErrorLogger);
            }

            SwallowHelper.TryDo(() => Buffer?.Dispose(), ErrorLogger);
            SwallowHelper.TryDo(RemoveAllAppenders, ErrorLogger);
            base.Dispose();
        }

        public void ActivateOptions()
        {
            Buffer = BufferSize > 0
                ? new BlockingCollection<LoggingEvent>(BufferSize) // call to Add may block until space is available to store the provided item (https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.blockingcollection-1.add?view=net-5.0#System_Collections_Concurrent_BlockingCollection_1_Add__0_)
                : new BlockingCollection<LoggingEvent>();

            var cancellationToken = _cancellation.Token;
            _worker = Task.Factory
                .StartNew(() => Append(Buffer.GetConsumingEnumerable(cancellationToken), cancellationToken),
                    cancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Current);
        }

        private void CloseBuffer()
        {
            switch (BufferClosingType)
            {
                case BufferClosingType.Immediate:
                    break;
                case BufferClosingType.DumpToErrorHandler:
                    foreach (var loggingEvent in Buffer)
                    {
                        ErrorLogger.Error(loggingEvent.Serialize());
                    }

                    break;
                case BufferClosingType.DumpToLog:
                    foreach (var loggingEvent in Buffer)
                    {
                        AppendLoopOnAppenders(loggingEvent);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DoAppendBoundedBuffer(LoggingEvent loggingEvent)
        {
            var errorMessage = "Cannot add the loggingEvent in to the buffer";

            switch (BufferOverflowBehaviour)
            {
                case BufferOverflowBehaviour.RejectNew:
                    if (!Buffer.TryAdd(loggingEvent))
                    {
                        ErrorLogger.Error(errorMessage);
                    }

                    break;
                case BufferOverflowBehaviour.Wait:
                    Buffer.Add(loggingEvent);
                    break;
                case BufferOverflowBehaviour.DirectForwarding:
                    if (!Buffer.TryAdd(loggingEvent))
                    {
                        ErrorLogger.Error(errorMessage + " The direct forwarding is used");
                        AppendLoopOnAppenders(loggingEvent);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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

                AppendLoopOnAppenders(loggingEvent);
            }
        }
    }
}
