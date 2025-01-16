using System.Diagnostics;
using log4net.Core;
using Xunit;


namespace log4net.tools.Tests
{
    public class ForwardingAppenderAsyncAppendTest: ForwardingAppenderAsyncTestBase
    {
        private const int BlockingTimeSec = 1;

        [Theory]
        [InlineData(0, 1)]
        [InlineData(100, 10)]
        [InlineData(0, 10)]
        [InlineData(100, 1)]
        public void Append_Warn_Success(int bufferSize, byte poolSize)
        {
            SetupRepository(bufferSize, poolSize);

            Assert.Equal(0, CountingAppender.Counter);

            Log();
            Thread.Sleep(1000);

            Assert.Equal(1, CountingAppender.Counter);
        }

        [Theory]
        [InlineData(100, 0)]
        [InlineData(-100, 1)]
        [InlineData(-100, 0)]
        [InlineData(0, 0)]
        public void Append_InvalidParameters_Error(int bufferSize, byte poolSize)
        {
            Assert.Throws<InvalidOperationException>(() => SetupRepository(bufferSize, poolSize));
        }

        [Theory]
        [InlineData(3, 10)]
        [InlineData(3, 1)]
        public void Append_BufferOverflowWait_Blocked(int bufferSize, byte poolSize)
        {
            SetupRepository(bufferSize, poolSize);
            var blockingAppender = new BlockingAppender(BlockingTimeSec);
            ForwardingAppender.AddAppender(blockingAppender);
            ForwardingAppender.BufferOverflowBehaviour = BufferOverflowBehaviour.Wait;

            Assert.Equal(0, CountingAppender.Counter);
            var lastLogElapsedSec = OverflowBuffer();
            Assert.True(poolSize + 1 == CountingAppender.Counter || poolSize + 2 == CountingAppender.Counter, $"Count of loggingEvents processed: {CountingAppender.Counter}");
            Assert.True(BlockingTimeSec == lastLogElapsedSec, $"Duration in seconds when client was blocked: {lastLogElapsedSec}");

            Thread.Sleep((bufferSize + 3) * BlockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(bufferSize + poolSize + 1 == CountingAppender.Counter, $"Final count of loggingEvents processed: {CountingAppender.Counter}");
        }

        [Theory]
        [InlineData(3, 10)]
        [InlineData(3, 1)]
        public void Append_BufferOverflowRejectNew_Lost(int bufferSize, byte poolSize)
        {
            SetupRepository(bufferSize, poolSize);
            var blockingAppender = new BlockingAppender(BlockingTimeSec);
            ForwardingAppender.AddAppender(blockingAppender);
            ForwardingAppender.BufferOverflowBehaviour = BufferOverflowBehaviour.RejectNew;

            Assert.Equal(0, CountingAppender.Counter);
            var lastLogElapsedSec = OverflowBuffer();
            Assert.True(poolSize == CountingAppender.Counter, $"Count of loggingEvents processed: {CountingAppender.Counter}");
            Assert.True(0 == lastLogElapsedSec, $"Duration in seconds when client was blocked: {lastLogElapsedSec}");

            Thread.Sleep((bufferSize + 3) * BlockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(bufferSize + poolSize == CountingAppender.Counter, $"Final count of loggingEvents processed: {CountingAppender.Counter}");
        }

        [Theory]
        [InlineData(3, 10, 14)]
        [InlineData(3, 1, 3)]
        public void Append_BufferOverflowDirectForwarding_BlockedInParallel(int bufferSize, byte poolSize, byte expectedProcessed)
        {
            SetupRepository(bufferSize, poolSize);
            var blockingAppender = new BlockingAppender(BlockingTimeSec);
            ForwardingAppender.AddAppender(blockingAppender);
            ForwardingAppender.BufferOverflowBehaviour = BufferOverflowBehaviour.DirectForwarding;

            Assert.Equal(0, CountingAppender.Counter);
            var lastLogElapsedSec = OverflowBuffer();

            Assert.True(expectedProcessed == CountingAppender.Counter, 
                $"Count of loggingEvents processed: {CountingAppender.Counter}. Expected: {expectedProcessed}");

            Assert.True(BlockingTimeSec == lastLogElapsedSec, $"Duration in seconds when client was blocked: {lastLogElapsedSec}" +
                                                                  $". Expected: {BlockingTimeSec}");

            Thread.Sleep((bufferSize + 3) * BlockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(bufferSize + poolSize + 1 == CountingAppender.Counter, $"Final count of loggingEvents processed: {CountingAppender.Counter}");
        }

        [Theory]
        [InlineData(3, 10)]
        [InlineData(3, 1)]
        public void Append_WithProperties_Consistent(int bufferSize, byte poolSize)
        {
            SetupRepository(bufferSize, poolSize);
            var consistencyValidatorAppender = new ConsistencyValidationAppender();
            ForwardingAppender.AddAppender(consistencyValidatorAppender);
            ForwardingAppender.BufferOverflowBehaviour = BufferOverflowBehaviour.Wait;

            var numberOfConcurrentLogs = 200;

            Assert.Equal(0, CountingAppender.Counter);
            Parallel.For(0, numberOfConcurrentLogs, (idx) => LogWithContext(idx.ToString()));

            Thread.Sleep(1000);
            Assert.True(numberOfConcurrentLogs == CountingAppender.Counter,
                $"Count of loggingEvents processed: {CountingAppender.Counter}. Expected: {numberOfConcurrentLogs}");
            
            Assert.True(numberOfConcurrentLogs == consistencyValidatorAppender.ConsistencyCounter,
                $"Count of valid loggingEvents: {consistencyValidatorAppender.ConsistencyCounter}. Expected: {numberOfConcurrentLogs}");
            
            Assert.True(consistencyValidatorAppender.InconsistentEvents.Count == 0,
                $"Count of inconsistent loggingEvents: {consistencyValidatorAppender.InconsistentEvents.Count}. Expected: {0}");
        }

        private void LogWithContext(string message)
        {
            ILogger logger = Hierarchy.GetLogger("test");

            LogicalThreadContext.Properties[message] = message;
            logger.Log(typeof(ForwardingAppenderAsync), Level.Warn, message, new ApplicationException(message));
            LogicalThreadContext.Properties.Remove(message);
        }


        private int OverflowBuffer()
        {
            Stopwatch stopWatch = new Stopwatch();

            for (int i = 0; i < MaxLogToOverflowBuffer; i++)
            {
                stopWatch.Reset();
                stopWatch.Start();
                Log();
                stopWatch.Stop();

                Thread.Sleep(10);
                if (CancelationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            return (int)Math.Round(stopWatch.Elapsed.TotalSeconds);
        }
    }
}
