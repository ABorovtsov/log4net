using System;

namespace log4net.tools
{
    internal static class SwallowHelper
    {
        public static bool TryDo(Action action, IErrorLogger errorLogger = null)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                errorLogger?.Error(e.ToString());
                return false;
            }
        }
    }
}