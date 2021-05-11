using System;
using System.Threading;
using log4net.Appender;
using log4net.Core;

namespace log4net.tools.Tests
{
    public class BlockingAppender : IAppender
    {
        public string Name { get; set; }

        private readonly byte _blockingTimeSec;
        
        public BlockingAppender(byte blockingTimeSec)
        {
            if (blockingTimeSec <= 0)
            {
                throw new ArgumentException($"{nameof(blockingTimeSec)} must be more than zero");
            }

            _blockingTimeSec = blockingTimeSec;
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            Thread.Sleep(_blockingTimeSec * 1000);
        }

        public void Close()
        {}
    }
}
