using log4net.Core;

namespace log4net.tools
{
    /// <summary>
    /// The appender forwards LoggingEvents to a list of attached appenders asynchronously
    /// </summary>
    public class ForwardingAppenderAsyncWithMetrics : ForwardingAppenderAsync
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
            using (new LatencyMonitor(Buffer.Count, MetricsWriter))
            {
                base.Close();
            }
        }

        protected override void AppendLoopOnAppenders(LoggingEvent loggingEvent)
        {
            using (new LatencyMonitor(Buffer.Count, MetricsWriter))
            {
                base.AppendLoopOnAppenders(loggingEvent);
            }
        }
    }
}
