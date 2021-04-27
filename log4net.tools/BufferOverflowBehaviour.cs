namespace log4net.tools
{
    public enum BufferOverflowBehaviour
    {
        /// <summary>
        /// New income items are not added (means lost) in the buffer while it is overflowed.
        /// The fastest option
        /// </summary>
        RejectNew,

        /// <summary>
        /// New income items wait free space in the buffer to be added.
        /// Client is blocked in this mode when overflow happens
        /// </summary>
        Wait,

        /// <summary>
        /// Incoming items avoid the buffer and go directly to the attached appenders.
        /// The item processing goes in the unordered manner but in parallel with the main worker.
        /// It is blocking behaviour but it is faster than the "Wait" mode as it goes directly
        /// and does not consume the main worker time 
        /// </summary>
        DirectForwarding
    }
}