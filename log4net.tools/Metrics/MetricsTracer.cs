using System.Diagnostics;

namespace log4net.tools
{
    class MetricsTracer : IMetricsWriter
    {
        public void WriteLatency(LatencyWithContext latency)
        {
            var data = new
            {
                latency.DateTime,
                latency.LatencyUs,
                latency.BufferSize,
                latency.CallerName
            };

            Trace.TraceInformation($"{nameof(MetricsTracer)}: {data}");
        }
    }
}