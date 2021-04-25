using System.Collections.Generic;
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

        [Benchmark(Description = "RollingFileAppender")]
        [ArgumentsSource(nameof(Loggers))]
        public void PssTest(ILog logger, string message)
        {
            for (int i = 0; i < 100; i++)
            {
                logger.Info(message);
            }
        }

        public IEnumerable<object[]> Loggers()
        {
            yield return new object[] { _rollingFileLogger, "Original" };
            yield return new object[] { _forwardingLogger, "Forwarded" };
        }
    }
}