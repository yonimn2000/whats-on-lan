namespace YonatanMankovich.WhatsOnLan.Core.Exceptions
{
    public class NetworkScannerRunningException : Exception
    {
        public NetworkScannerRunningException() : base("The current network scanner is already running.") { }
    }
}