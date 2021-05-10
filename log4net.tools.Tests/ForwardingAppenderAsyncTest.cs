using System;
using System.Diagnostics;
using System.Threading;
using log4net.Config;
using log4net.Core;
using Xunit;


namespace log4net.tools.Tests
{
    public class ForwardingAppenderAsyncTest
    {
        private const int BlockingTimeSec = 3;
        private const int BufferSize = 3;

        private ForwardingAppenderAsync _forwardingAppender;
        private CountingAppender _countingAppender;
        private Repository.Hierarchy.Hierarchy _hierarchy;

        private void SetupRepository(int bufferSize = 0, byte poolSize = 1)
        {
            _hierarchy = new Repository.Hierarchy.Hierarchy();

            _countingAppender = new CountingAppender();
            _forwardingAppender = new ForwardingAppenderAsync 
            {
                BufferSize = bufferSize,
                WorkerPoolSize = poolSize
            };
            _forwardingAppender.ActivateOptions();
            _forwardingAppender.AddAppender(_countingAppender);

            BasicConfigurator.Configure(_hierarchy, _forwardingAppender);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(100, 10)]
        [InlineData(0, 10)]
        [InlineData(100, 1)]
        public void Append_Warn_Success(int bufferSize, byte poolSize)
        {
            SetupRepository(bufferSize, poolSize);

            Assert.Equal(0, _countingAppender.Counter);

            Log();
            Thread.Sleep(1000);

            Assert.Equal(1, _countingAppender.Counter);
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
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferOverflowBehaviour = BufferOverflowBehaviour.Wait;

            Assert.Equal(0, _countingAppender.Counter);
            var lastLogElapsedSec = OverflowBuffer();
            Assert.True(2 == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}");
            Assert.True(BlockingTimeSec == lastLogElapsedSec, $"Duration in seconds when client was blocked: {lastLogElapsedSec}");

            Thread.Sleep((bufferSize + 3) * BlockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(bufferSize + 2 == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        [Fact]
        public void Append_BufferOverflowRejectNew_Lost()
        {
            SetupRepository(BufferSize);
            var blockingAppender = new BlockingAppender(BlockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferOverflowBehaviour = BufferOverflowBehaviour.RejectNew;

            Assert.Equal(0, _countingAppender.Counter);
            var lastLogElapsedSec = OverflowBuffer();
            Assert.True(1 == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}");
            Assert.True(0 == lastLogElapsedSec, $"Duration in seconds when client was blocked: {lastLogElapsedSec}");

            Thread.Sleep((BufferSize + 3) * BlockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(BufferSize + 1 == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        [Fact]
        public void Append_BufferOverflowDirectForwarding_BlockedInParallel()
        {
            SetupRepository(BufferSize);
            var blockingAppender = new BlockingAppender(BlockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferOverflowBehaviour = BufferOverflowBehaviour.DirectForwarding;

            Assert.Equal(0, _countingAppender.Counter);
            var lastLogElapsedSec = OverflowBuffer();
            Assert.True(3 == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}");
            Assert.True(2 * BlockingTimeSec == lastLogElapsedSec, $"Duration in seconds when client was blocked: {lastLogElapsedSec}");

            Thread.Sleep((BufferSize + 3) * BlockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(BufferSize + 2 == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        [Fact]
        public void Append_Close_ImmediateClose()
        {
            SetupRepository(BufferSize);
            byte blockingTimeSec = 1;
            var blockingAppender = new BlockingAppender(blockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferClosingType = BufferClosingType.Immediate;

            Assert.Equal(0, _countingAppender.Counter);

            var closingElapsedSec = CloseAppenderWithFilledBuffer();
            Assert.True(1 == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}");
            Assert.True(closingElapsedSec <= blockingTimeSec, $"Duration in seconds when client was blocked: {closingElapsedSec}");

            Thread.Sleep((BufferSize + 3) * blockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(1 == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        [Fact]
        public void Append_Close_DumpToErrorHandler()
        {
            SetupRepository(BufferSize);
            byte blockingTimeSec = 1;
            var blockingAppender = new BlockingAppender(blockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferClosingType = BufferClosingType.DumpToErrorHandler;

            Assert.Equal(0, _countingAppender.Counter);

            var closingElapsedSec = CloseAppenderWithFilledBuffer();
            Assert.True(1 == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}");
            Assert.True(closingElapsedSec <= blockingTimeSec, $"Duration in seconds when client was blocked: {closingElapsedSec}");

            // todo: check ErrorLogger output
            Thread.Sleep((BufferSize + 3) * blockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(1 == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        [Fact]
        public void Append_Close_DumpToLog()
        {
            SetupRepository(BufferSize);
            byte blockingTimeSec = 1;
            var blockingAppender = new BlockingAppender(blockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferClosingType = BufferClosingType.DumpToLog;

            Assert.Equal(0, _countingAppender.Counter);

            var closingElapsedSec = CloseAppenderWithFilledBuffer();
            Assert.True(4 == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}");
            Assert.True(4 == closingElapsedSec, $"Duration in seconds when client was blocked: {closingElapsedSec}");
        }

        private int CloseAppenderWithFilledBuffer()
        {
            Stopwatch stopWatch = new Stopwatch();

            Log(); // 0. It's dequeued immediately
            Log(); // 1
            Log(); // 2
            Log(); // 3
            stopWatch.Start();
            _forwardingAppender.Close();
            stopWatch.Stop();
            
            return (int)stopWatch.Elapsed.TotalSeconds;
        }

        private int OverflowBuffer()
        {
            Stopwatch stopWatch = new Stopwatch();

            Log(); // 0. It's dequeued immediately
            Log(); // 1
            Log(); // 2
            Log(); // 3
            stopWatch.Start();
            Log(); // 4 => BufferOverflow state. If Appender.BufferOverflowBehaviour ==
                   //     BufferOverflowBehaviour.Wait, it's blocked while #0 is in processing
                   //     BufferOverflowBehaviour.RejectNew, it's lost, no any blocking
            stopWatch.Stop();

            return (int)stopWatch.Elapsed.TotalSeconds;
        }

        [Fact]
        public void Close_NoErrors()
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
