using System.Threading;
using log4net.Appender;
using log4net.Core;

namespace log4net.tools.Tests
{
	public class CountingAppender : AppenderSkeleton
	{
		private int _counter;

		public int Counter => _counter;

		protected override void Append(LoggingEvent logEvent)
		{
			Interlocked.Increment(ref _counter);
		}
	}
}
