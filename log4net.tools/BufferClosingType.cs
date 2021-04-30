namespace log4net.tools
{
    /// <summary>
    /// It defines what is needed when an app crashes or stops but the buffer is not empty
    /// </summary>
    public enum BufferClosingType
    {
        /// <summary>
        /// Buffered items which have not been processed yet are lost in this mode.
        /// The fastest option.
        /// </summary>
        Immediate,

        /// <summary>
        /// Buffered items which have not been processed yet go to the ErrorHandler.
        /// </summary>
        DumpToErrorHandler,

        /// <summary>
        /// Buffered items which have not been processed yet go to the attached appenders synchronously.
        /// </summary>
        DumpToLog
    }
}