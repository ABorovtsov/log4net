using log4net.Core;

namespace log4net.tools
{
    internal static class LoggingEventExtensions
    {
        /// <summary>
        /// Converts LoggingEvent to plain string for logging
        /// </summary>
        /// <param name="loggingEvent"></param>
        /// <returns></returns>
        public static string Serialize(this LoggingEvent loggingEvent)
        {
            return $"{(loggingEvent.Level == null ? string.Empty : loggingEvent.Level.Name)} " +
                   $"{loggingEvent.LoggerName} " +
                   $"{loggingEvent.RenderedMessage}" +
                   $"\n{loggingEvent.ExceptionObject}";
        }
    }
}