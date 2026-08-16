namespace YonatanMankovich.WhatsOnLan.Core.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a network scanner is started while it is already running.
    /// </summary>
    public class NetworkScannerRunningException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkScannerRunningException"/> class.
        /// </summary>
        public NetworkScannerRunningException() : base("The current network scanner is already running.") { }
    }
}
