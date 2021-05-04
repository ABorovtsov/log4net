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
                LatencyUs = latency.LatencyUs.ToString("F1"),
                latency.BufferSize,
                latency.CallerName,
                latency.AllocatedBytes
            };

            Trace.TraceInformation($"{nameof(MetricsTracer)}: {data}");
        }
    }
}