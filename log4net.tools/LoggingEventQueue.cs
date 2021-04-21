using System;
using System.Threading.Tasks;
using log4net.Core;

namespace log4net.tools
{
    class LoggingEventQueue: IQueue
    {
        private readonly Action<LoggingEvent> _appendCallback;

        public LoggingEventQueue(Action<LoggingEvent> appendCallback)
        {
            _appendCallback = appendCallback ?? throw new ArgumentNullException(nameof(appendCallback));
        }

        public void Enqueue(LoggingEvent loggingEvent)
        {
            Task.Factory.StartNew(() => _appendCallback(loggingEvent)); // todo: use a dedicated worker thread instead
        }
    }
}