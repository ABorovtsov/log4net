using log4net.Appender;
using log4net.Core;

namespace log4net.tools
{
    /// <summary>
    /// The appender forwards LoggingEvents to a list of attached appenders asynchronously
    /// </summary>
    public class ForwardingAppenderAsyncWithMetrics : ForwardingAppenderAsync, IAppender
    {
        public IMetricsWriter MetricsWriter { get; set; } = new MetricsTracer();

        public new void DoAppend(LoggingEvent loggingEvent)
        {
            using (new LatencyMonitor(Buffer.Count, MetricsWriter))
            {
                base.DoAppend(loggingEvent);
            }
        }

        public new void Close()
        {
            int bufferLength = -1;

            if (SwallowHelper.TryDo(() => bufferLength = Buffer.Count))
            {
                using (new LatencyMonitor(bufferLength, MetricsWriter))
                {
                    base.Close();
                }
            }
        }

        protected override void AppendLoopOnAppenders(LoggingEvent loggingEvent)
        {
            Dequeue(loggingEvent);
        }

        private void Dequeue(LoggingEvent loggingEvent)
        {
            using (new LatencyMonitor(Buffer.Count, MetricsWriter))
            {
                base.AppendLoopOnAppenders(loggingEvent);
            }
        }
    }
}
