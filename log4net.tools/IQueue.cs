using log4net.Core;

namespace log4net.tools
{
    public interface IQueue
    {
        void Enqueue(LoggingEvent loggingEvent);
    }
}