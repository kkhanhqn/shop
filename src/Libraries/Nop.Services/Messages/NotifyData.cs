namespace Nop.Services.Messages
{
    /// <summary>
    /// Message structure
    /// </summary>
    public struct NotifyData
    {
        /// <summary>
        /// Message type (success/warning/error)
        /// </summary>
        public NotifyType Type;

        /// <summary>
        /// Message text
        /// </summary>
        public string Message;

        /// <summary>
        /// A value indicating whether a message should be persisted for the next request
        /// </summary>
        public bool PersistForTheNextRequest;

    }
}
