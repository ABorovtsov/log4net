using System;
using System.Threading.Tasks;

namespace log4net.tools.benchmarks
{
    class Program
    {
        private const string TestMessage = "Test message";
        private const string ForwardedTestMessage = TestMessage + " (Forwarded)";

        static async Task Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            var rollingFileLogger = LogManager.GetLogger("RollingFileLogger");
            var forwardingLogger = LogManager.GetLogger("Forwarding2RollingFileLogger");

            rollingFileLogger.Info(TestMessage);
            forwardingLogger.Info(ForwardedTestMessage);

            await Task.Delay(2000); // allow loggs to be flushed
            Console.WriteLine("Done!");
        }
    }
}
