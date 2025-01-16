using log4net.Appender;
using log4net.Core;

namespace log4net.tools.Tests
{
    public class ConsistencyValidationAppender : IAppender
    {
        public string Name { get; set; }

        public int ConsistencyCounter => _consistencyCounter;

        private int _consistencyCounter;

        public readonly List<LoggingEvent> InconsistentEvents = new List<LoggingEvent>();

        public void DoAppend(LoggingEvent loggingEvent)
        {
            var key = loggingEvent.RenderedMessage;

            if (loggingEvent.Properties.GetKeys().Contains(key) 
                && key == loggingEvent.LookupProperty(key).ToString() 
                && key == loggingEvent.ExceptionObject.Message
                && loggingEvent.Properties.Count == 4) // the additional properties are: "log4net:HostName", "log4net:Identity", "log4net:UserName"
            {
                Interlocked.Increment(ref _consistencyCounter);
                return;
            }

            InconsistentEvents.Add(loggingEvent);
        }

        public void Close()
        {}
    }
}
