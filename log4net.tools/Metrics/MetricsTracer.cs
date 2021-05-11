using System.Diagnostics;

namespace log4net.tools
{
    class MetricsTracer : IMetricsWriter
    {
        public void WriteLatency(LatencyWithContext latency)
        {
            var data = new
            {
                DateTime = latency.DateTime.ToString("hh:mm:ss.f"),
                LatencyUs = latency.LatencyUs.ToString("F1"),
                latency.BufferSize,
                latency.CallerName,
                latency.AllocatedBytes
            };

            Trace.TraceInformation($"{nameof(MetricsTracer)}: {data}");
        }
    }
}