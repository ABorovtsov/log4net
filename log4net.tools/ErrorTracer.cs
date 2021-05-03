using System.Diagnostics;

namespace log4net.tools
{
    internal class ErrorTracer : IErrorLogger
    {
        public void Error(string message)
        {
            if (message != null)
            {
                Trace.TraceError($"{nameof(ErrorTracer)}: Error - {message}");
            }
        }
    }
}