using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace log4net.tools.benchmarks
{
    public class ForwardingAppenderTest
    {
        private readonly ILog _rollingFileLogger;
        private readonly ILog _forwardingLogger;

        public ForwardingAppenderTest()
        {
            Config.XmlConfigurator.Configure();
            _rollingFileLogger = LogManager.GetLogger("RollingFileLogger");
            _forwardingLogger = LogManager.GetLogger("Forwarding2RollingFileLogger");
        }

        [Benchmark(Description = "Sequential Load. RollingFileAppender")]
        [ArgumentsSource(nameof(Loggers))]
        public void SequentialLoggingTest(ILog logger, string message)
        {
            for (int i = 0; i < 1000; i++)
            {
                logger.Info(message);
            }
        }

        [Benchmark(Description = "Parallel Load. RollingFileAppender")]
        [ArgumentsSource(nameof(Loggers))]
        public void ParallelLoggingTest(ILog logger, string message)
        {
            Parallel.For(0, 1000, i => logger.Info(message));
        }

        public IEnumerable<object[]> Loggers()
        {
            yield return new object[] { _rollingFileLogger, "Original" };
            yield return new object[] { _forwardingLogger, "Forwarded" };
        }
    }
}