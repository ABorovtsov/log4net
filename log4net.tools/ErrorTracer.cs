﻿using System.Diagnostics;

namespace log4net.tools
{
    internal class ErrorTracer : IErrorLogger
    {
        public void Error(string message)
        {
            Trace.TraceError(message);
        }
    }
}