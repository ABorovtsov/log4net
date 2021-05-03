namespace log4net.tools
{
    public interface IMetricsWriter
    {
        void WriteLatency(LatencyWithContext latency);
    }
}