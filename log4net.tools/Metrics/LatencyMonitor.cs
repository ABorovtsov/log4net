using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace log4net.tools
{
    public readonly struct LatencyMonitor: IDisposable
    {
        private readonly IMetricsWriter _metricsWriter;
        private readonly Stopwatch _stopWatch;
        private readonly LatencyWithContext _latencyWithContext;

        public LatencyMonitor(int bufferSize, IMetricsWriter metricsWriter, [CallerMemberName] string callerMemberName = "")
        {
            _metricsWriter = metricsWriter ?? throw new ArgumentNullException(nameof(metricsWriter));
            _latencyWithContext = new LatencyWithContext
            {
                BufferSize = bufferSize,
                CallerName = callerMemberName,
                DateTime = DateTime.Now,
                AllocatedBytes = GC.GetTotalMemory(false)
            };

            _stopWatch = new Stopwatch();
            _stopWatch.Start();
        }

        public void Dispose()
        {
            _stopWatch.Stop();
            _latencyWithContext.LatencyUs = _stopWatch.Elapsed.TotalMilliseconds * 1000;
            _metricsWriter.WriteLatency(_latencyWithContext);
        }
    }
}