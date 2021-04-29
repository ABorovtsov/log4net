using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace log4net.tools.benchmarks
{
    public class RollingFileAppenderTest
    {
        private readonly ILog _rollingFileLogger;
        private readonly ILog _forwardingLogger;
        private readonly ILog _bufferingLogger;
        
        public RollingFileAppenderTest()
        {
            Config.XmlConfigurator.Configure();
            _rollingFileLogger = LogManager.GetLogger("RollingFileLogger");
            _forwardingLogger = LogManager.GetLogger("Forwarding2RollingFileLogger");
            _bufferingLogger = LogManager.GetLogger("BufferingForwardingLogger");
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
            yield return new object[] { _bufferingLogger, "Buffering" };
        }
    }
}