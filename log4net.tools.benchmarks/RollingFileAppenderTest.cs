using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace log4net.tools.benchmarks
{
    public class RollingFileAppenderTest
    {
        private readonly ILog _bufferingLogger;
        private readonly ILog _forwardingLogger;
        private readonly ILog _forwardingWithMetricsLogger;
        private readonly ILog _rollingFileLogger;

        public RollingFileAppenderTest()
        {
            Config.XmlConfigurator.Configure();
            _bufferingLogger = LogManager.GetLogger("BufferingForwardingLogger");
            _forwardingLogger = LogManager.GetLogger("Forwarding2RollingFileLogger");
            _forwardingWithMetricsLogger = LogManager.GetLogger("ForwardingWithMetrics2RollingFileLogger");
            _rollingFileLogger = LogManager.GetLogger("RollingFileLogger");
        }

        [Benchmark(Description = "Sequential Load")]
        [ArgumentsSource(nameof(Loggers))]
        public void SequentialLoggingTest(ILog logger, string message)
        {
            for (int i = 0; i < 10000; i++)
            {
                logger.Info(message);
            }
        }

        [Benchmark(Description = "Parallel Load")]
        [ArgumentsSource(nameof(Loggers))]
        public void ParallelLoggingTest(ILog logger, string message)
        {
            Parallel.For(0, 10000, i => logger.Info(message));
        }

        public IEnumerable<object[]> Loggers()
        {
            yield return new object[] { _rollingFileLogger, "Original" };
            yield return new object[] { _forwardingLogger, "Forwarding" };
            yield return new object[] { _forwardingWithMetricsLogger, "ForwardingWithMetrics" };
            yield return new object[] { _bufferingLogger, "Buffering" };
        }
    }
}