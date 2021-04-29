using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace log4net.tools.benchmarks
{
    public class AdoNetAppenderTest
    {
        private readonly ILog _originalLogger;
        private readonly ILog _forwardingLogger;
        
        public AdoNetAppenderTest()
        {
            Config.XmlConfigurator.Configure();
            _originalLogger = LogManager.GetLogger("AdoNetLogger");
            _forwardingLogger = LogManager.GetLogger("ForwardingAdoNetLogger");
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
            yield return new object[] { _originalLogger, "Original" };
            yield return new object[] { _forwardingLogger, "Forwarding" };
        }
    }
}