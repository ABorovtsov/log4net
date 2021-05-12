using System.Diagnostics;
using System.Threading;
using Xunit;


namespace log4net.tools.Tests
{
    public class ForwardingAppenderAsyncCloseTest: ForwardingAppenderAsyncTestBase
    {
        [Theory]
        [InlineData(3, 10, 14)]
        [InlineData(3, 1, 3)]
        public void Append_Close_ImmediateClose(int bufferSize, byte poolSize, byte expectedProcessed)
        {
            SetupRepository(bufferSize, poolSize);
            byte blockingTimeSec = 1;
            var blockingAppender = new BlockingAppender(blockingTimeSec);
            ForwardingAppender.AddAppender(blockingAppender);
            ForwardingAppender.BufferClosingType = BufferClosingType.Immediate;

            Assert.Equal(0, CountingAppender.Counter);

            var closingElapsedSec = CloseAppenderWithFilledBuffer();
            Assert.True(expectedProcessed == CountingAppender.Counter, 
                $"Count of loggingEvents processed: {CountingAppender.Counter}. Expected: {expectedProcessed}");
            Assert.True(closingElapsedSec <= blockingTimeSec, 
                $"Duration in seconds when client was blocked: {closingElapsedSec}. Expected: {blockingTimeSec}");

            Thread.Sleep((bufferSize + 3) * blockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(expectedProcessed == CountingAppender.Counter, 
                $"Final count of loggingEvents processed: {CountingAppender.Counter}. Expected: {expectedProcessed}");
        }

        [Theory]
        [InlineData(3, 10, 14)]
        [InlineData(3, 1, 3)]
        public void Append_Close_DumpToErrorHandler(int bufferSize, byte poolSize, byte expectedProcessed)
        {
            SetupRepository(bufferSize, poolSize);
            byte blockingTimeSec = 1;
            var blockingAppender = new BlockingAppender(blockingTimeSec);
            ForwardingAppender.AddAppender(blockingAppender);
            ForwardingAppender.BufferClosingType = BufferClosingType.DumpToErrorHandler;

            Assert.Equal(0, CountingAppender.Counter);

            var closingElapsedSec = CloseAppenderWithFilledBuffer();
            Assert.True(expectedProcessed == CountingAppender.Counter, 
                $"Count of loggingEvents processed: {CountingAppender.Counter}");
            Assert.True(closingElapsedSec <= blockingTimeSec, 
                $"Duration in seconds when client was blocked: {closingElapsedSec}");

            // todo: check ErrorLogger output
            Thread.Sleep((bufferSize + 3) * blockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(expectedProcessed == CountingAppender.Counter, 
                $"Final count of loggingEvents processed: {CountingAppender.Counter}");
        }

        [Theory]
        [InlineData(3, 10, 14, 0)]
        [InlineData(3, 1, 5, 2)]
        public void Append_Close_DumpToLog(int bufferSize, byte poolSize, byte expectedProcessed, byte expectedBlockDurationSec)
        {
            SetupRepository(bufferSize, poolSize);
            byte blockingTimeSec = 1;
            var blockingAppender = new BlockingAppender(blockingTimeSec);
            ForwardingAppender.AddAppender(blockingAppender);
            ForwardingAppender.BufferClosingType = BufferClosingType.DumpToLog;

            Assert.Equal(0, CountingAppender.Counter);

            var closingElapsedSec = CloseAppenderWithFilledBuffer();
            Assert.True(expectedProcessed == CountingAppender.Counter, 
                $"Count of loggingEvents processed: {CountingAppender.Counter}. Expected: {expectedProcessed}");
            Assert.True(expectedBlockDurationSec == closingElapsedSec, 
                $"Duration in seconds when client was blocked: {closingElapsedSec}. Expected: {4}");
        }

        private int CloseAppenderWithFilledBuffer()
        {
            Stopwatch stopWatch = new Stopwatch();

            for (int i = 0; i < MaxLogToOverflowBuffer; i++)
            {
                Log();

                Thread.Sleep(10);
                if (CancelationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            stopWatch.Start();
            ForwardingAppender.Close();
            stopWatch.Stop();

            return (int)stopWatch.Elapsed.TotalSeconds;
        }

        [Fact]
        public void Close_NoErrors()
        {
            SetupRepository();
            Log();

            ForwardingAppender.Close();
        }
    }
}
