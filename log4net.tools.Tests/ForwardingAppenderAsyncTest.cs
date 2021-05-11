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
        private const int MaxLogToOwerflowBuffer = 1000;

        private ForwardingAppenderAsync _forwardingAppender;
        private CountingAppender _countingAppender;
        private Repository.Hierarchy.Hierarchy _hierarchy;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken _cancelationToken;

        private void SetupRepository(int bufferSize = 0, byte poolSize = 1)
        {
            _hierarchy = new Repository.Hierarchy.Hierarchy();

            _countingAppender = new CountingAppender();
            _forwardingAppender = new ForwardingAppenderAsyncWithMetrics()
            {
                BufferSize = bufferSize,
                WorkerPoolSize = poolSize
            };
            
            _forwardingAppender.BufferOverflowEvent += OnBufferOverflowEvent;
            _forwardingAppender.ActivateOptions();
            _forwardingAppender.AddAppender(_countingAppender);

            BasicConfigurator.Configure(_hierarchy, _forwardingAppender);
            _cancelationToken = cancellationTokenSource.Token;
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
            Assert.True(poolSize + 1 == _countingAppender.Counter || poolSize + 2 == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}");
            Assert.True(BlockingTimeSec == lastLogElapsedSec, $"Duration in seconds when client was blocked: {lastLogElapsedSec}");

            Thread.Sleep((bufferSize + 3) * BlockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(bufferSize + poolSize + 1 == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        private void OnBufferOverflowEvent(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

        [Theory]
        [InlineData(3, 10)]
        [InlineData(3, 1)]
        public void Append_BufferOverflowRejectNew_Lost(int bufferSize, byte poolSize)
        {
            SetupRepository(bufferSize, poolSize);
            var blockingAppender = new BlockingAppender(BlockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferOverflowBehaviour = BufferOverflowBehaviour.RejectNew;

            Assert.Equal(0, _countingAppender.Counter);
            var lastLogElapsedSec = OverflowBuffer();
            Assert.True(poolSize == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}");
            Assert.True(0 == lastLogElapsedSec, $"Duration in seconds when client was blocked: {lastLogElapsedSec}");

            Thread.Sleep((BufferSize + 3) * BlockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(bufferSize + poolSize == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        [Theory]
        [InlineData(3, 10, 14)]
        [InlineData(3, 1, 3)]
        public void Append_BufferOverflowDirectForwarding_BlockedInParallel(int bufferSize, byte poolSize, byte expectedProcessed)
        {
            SetupRepository(bufferSize, poolSize);
            var blockingAppender = new BlockingAppender(BlockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferOverflowBehaviour = BufferOverflowBehaviour.DirectForwarding;

            Assert.Equal(0, _countingAppender.Counter);
            var lastLogElapsedSec = OverflowBuffer();

            Assert.True(expectedProcessed == _countingAppender.Counter, 
                $"Count of loggingEvents processed: {_countingAppender.Counter}. Expected: {expectedProcessed}");

            Assert.True(BlockingTimeSec == lastLogElapsedSec, $"Duration in seconds when client was blocked: {lastLogElapsedSec}" +
                                                                  $". Expected: {BlockingTimeSec}");

            Thread.Sleep((bufferSize + 3) * BlockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(bufferSize + poolSize + 1 == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        [Theory]
        [InlineData(3, 10, 14)]
        [InlineData(3, 1, 3)]
        public void Append_Close_ImmediateClose(int bufferSize, byte poolSize, byte expectedProcessed)
        {
            SetupRepository(bufferSize, poolSize);
            byte blockingTimeSec = 1;
            var blockingAppender = new BlockingAppender(blockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferClosingType = BufferClosingType.Immediate;

            Assert.Equal(0, _countingAppender.Counter);

            var closingElapsedSec = CloseAppenderWithFilledBuffer();
            Assert.True(expectedProcessed == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}. Expected: {expectedProcessed}");
            Assert.True(closingElapsedSec <= blockingTimeSec, $"Duration in seconds when client was blocked: {closingElapsedSec}");

            Thread.Sleep((bufferSize + 3) * blockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(expectedProcessed == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        [Theory]
        [InlineData(3, 10, 14)]
        [InlineData(3, 1, 3)]
        public void Append_Close_DumpToErrorHandler(int bufferSize, byte poolSize, byte expectedProcessed)
        {
            SetupRepository(bufferSize, poolSize);
            byte blockingTimeSec = 1;
            var blockingAppender = new BlockingAppender(blockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferClosingType = BufferClosingType.DumpToErrorHandler;

            Assert.Equal(0, _countingAppender.Counter);

            var closingElapsedSec = CloseAppenderWithFilledBuffer();
            Assert.True(expectedProcessed == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}");
            Assert.True(closingElapsedSec <= blockingTimeSec, $"Duration in seconds when client was blocked: {closingElapsedSec}");

            // todo: check ErrorLogger output
            Thread.Sleep((BufferSize + 3) * blockingTimeSec * 1000); // wait for all the events are processed
            Assert.True(expectedProcessed == _countingAppender.Counter, $"Final count of loggingEvents processed: {_countingAppender.Counter}");
        }

        [Theory]
        [InlineData(3, 10, 14, 0)]
        [InlineData(3, 1, 5, 2)]
        public void Append_Close_DumpToLog(int bufferSize, byte poolSize, byte expectedProcessed, byte expectedBlockDurationSec)
        {
            SetupRepository(bufferSize, poolSize);
            byte blockingTimeSec = 1;
            var blockingAppender = new BlockingAppender(blockingTimeSec);
            _forwardingAppender.AddAppender(blockingAppender);
            _forwardingAppender.BufferClosingType = BufferClosingType.DumpToLog;

            Assert.Equal(0, _countingAppender.Counter);

            var closingElapsedSec = CloseAppenderWithFilledBuffer();
            Assert.True(expectedProcessed == _countingAppender.Counter, $"Count of loggingEvents processed: {_countingAppender.Counter}. Expected: {expectedProcessed}");
            Assert.True(expectedBlockDurationSec == closingElapsedSec, $"Duration in seconds when client was blocked: {closingElapsedSec}. Expected: {4}");
        }

        private int CloseAppenderWithFilledBuffer()
        {
            Stopwatch stopWatch = new Stopwatch();

            for (int i = 0; i < MaxLogToOwerflowBuffer; i++)
            {
                Log();

                Thread.Sleep(10);
                if (_cancelationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            stopWatch.Start();
            _forwardingAppender.Close();
            stopWatch.Stop();

            return (int)stopWatch.Elapsed.TotalSeconds;
        }

        private int OverflowBuffer()
        {
            Stopwatch stopWatch = new Stopwatch();

            for (int i = 0; i < MaxLogToOwerflowBuffer; i++)
            {
                stopWatch.Reset();
                stopWatch.Start();
                Log();
                stopWatch.Stop();

                Thread.Sleep(10);
                if (_cancelationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            return (int)Math.Round(stopWatch.Elapsed.TotalSeconds);
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
