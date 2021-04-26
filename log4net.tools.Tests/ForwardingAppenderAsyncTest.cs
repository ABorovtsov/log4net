using System.Threading;
using log4net.Config;
using log4net.Core;
using Xunit;


namespace log4net.tools.Tests
{
    public class ForwardingAppenderAsyncTest
    {
        private ForwardingAppenderAsync _forwardingAppender;
        private CountingAppender _countingAppender;
        private Repository.Hierarchy.Hierarchy _hierarchy;

        private void SetupRepository()
        {
            _hierarchy = new Repository.Hierarchy.Hierarchy();

            _countingAppender = new CountingAppender();
            _countingAppender.ActivateOptions();

            _forwardingAppender = new ForwardingAppenderAsync {Fix = FixFlags.Partial};
            _forwardingAppender.AddAppender(_countingAppender);

            BasicConfigurator.Configure(_hierarchy, _forwardingAppender);
        }

        [Fact]
        public void AppendTest()
        {
            SetupRepository();

            Assert.Equal(0, _countingAppender.Counter);

            Log();
            Thread.Sleep(1000);

            Assert.Equal(1, _countingAppender.Counter);
        }

        [Fact]
        public void CloseTest()
        {
            SetupRepository();
            Log();

            _forwardingAppender.Close();
        }

        private void Log()
        {
            ILogger logger = _hierarchy.GetLogger("test");
            logger.Log(typeof(ForwardingAppenderAsync), Level.Warn, "Message logged", null);
        }
    }
}
