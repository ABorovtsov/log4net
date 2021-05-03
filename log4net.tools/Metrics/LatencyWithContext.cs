using System;

namespace log4net.tools
{
    public class LatencyWithContext
    {
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Latency in microseconds
        /// </summary>
        public double LatencyUs { get; set; }

        public int BufferSize { get; set; }

        /// <summary>
        /// Name of a method which was measured
        /// </summary>
        public string CallerName { get; set; }
    }
}