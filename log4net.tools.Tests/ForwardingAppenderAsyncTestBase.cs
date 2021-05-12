using System;
using System.Threading;
using log4net.Config;
using log4net.Core;


namespace log4net.tools.Tests
{
    public class ForwardingAppenderAsyncTestBase
    {
        protected const int MaxLogToOverflowBuffer = 100;

        protected ForwardingAppenderAsync ForwardingAppender;
        protected CountingAppender CountingAppender;
        protected CancellationToken CancelationToken;
        protected Repository.Hierarchy.Hierarchy Hierarchy;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();


        protected void SetupRepository(int bufferSize = 0, byte poolSize = 1)
        {
            Hierarchy = new Repository.Hierarchy.Hierarchy();

            CountingAppender = new CountingAppender();
            ForwardingAppender = new ForwardingAppenderAsyncWithMetrics()
            {
                BufferSize = bufferSize,
                WorkerPoolSize = poolSize
            };
            
            ForwardingAppender.BufferOverflowEvent += OnBufferOverflowEvent;
            ForwardingAppender.ActivateOptions();
            ForwardingAppender.AddAppender(CountingAppender);

            BasicConfigurator.Configure(Hierarchy, ForwardingAppender);
            CancelationToken = cancellationTokenSource.Token;
        }

        protected void Log()
        {
            ILogger logger = Hierarchy.GetLogger("test");
            logger.Log(typeof(ForwardingAppenderAsync), Level.Warn, "Message logged", null);
        }

        private void OnBufferOverflowEvent(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
        }
    }
}
