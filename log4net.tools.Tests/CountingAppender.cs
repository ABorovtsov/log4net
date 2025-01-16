using log4net.Appender;
using log4net.Core;

namespace log4net.tools.Tests
{
    public class CountingAppender : IAppender
    {
        public string Name { get; set; }

        public int Counter => _counter;

        private int _counter;

        public void DoAppend(LoggingEvent loggingEvent)
        {
            Interlocked.Increment(ref _counter);
        }

        public void Close()
        {}
    }
}
