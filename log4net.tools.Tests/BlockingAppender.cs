using System;
using System.Threading;
using log4net.Appender;
using log4net.Core;

namespace log4net.tools.Tests
{
    public class BlockingAppender : AppenderSkeleton
    {
        private readonly byte _blockingTimeSec;

        public BlockingAppender(byte blockingTimeSec)
        {
            if (blockingTimeSec <= 0)
            {
                throw new ArgumentException($"{nameof(blockingTimeSec)} must be more than zero");
            }

            _blockingTimeSec = blockingTimeSec;
        }

        protected override void Append(LoggingEvent logEvent)
        {
            Thread.Sleep(_blockingTimeSec * 1000);
        }
    }
}
