using System;
using System.Diagnostics;

namespace log4net.tools
{
    internal class ErrorTracer : IErrorLogger
    {
        public void Error(string message)
        {
            if (message != null)
            {
                var time = DateTime.Now.ToString("hh:mm:ss.f");
                Trace.TraceError($"{time} {nameof(ErrorTracer)}: Error - {message}");
            }
        }
    }
}