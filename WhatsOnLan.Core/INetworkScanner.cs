using System.Net;
using System.Net.NetworkInformation;

namespace YonatanMankovich.WhatsOnLan.Core
{
    /// <summary>
    /// Provides the base interface for defining a network scanner.
    /// </summary>
    public interface INetworkScanner
    {
        /// <summary>
        /// The event that occurs when the running state of the scanner changes.
        /// </summary>
        event EventHandler? StateHasChanged;

        /// <summary>
        /// Gets the network scannings status. <see langword="true"/> if the scanner is running; <see langword="false"/> otherwise.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Scans a given <see cref="IPAddress"/> and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="ipAddress">The IP address to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        Task<IpScanResult> ScanIpAddressAsync(IPAddress ipAddress);

        /// <summary>
        /// Scans a given <see cref="PhysicalAddress"/> and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="macAddress">The MAC address to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        Task<IpScanResult> ScanMacAddressAsync(PhysicalAddress macAddress);

        /// <summary>
        /// Scans all the possible IP addresses on the network.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        IList<IpScanResult> ScanNetwork();

        /// <summary>
        /// Scans all the possible IP addresses on the network asynchronously.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        Task<IList<IpScanResult>> ScanNetworkAsync();

        /// <summary>
        /// Checks if the provided <see cref="IPAddress"/> is on the current network.
        /// </summary>
        /// <param name="ipAddress"> The IP address. </param>
        /// <returns><see langword="true"/> if the IP address on the current network; <see langword="false"/> otherwise.</returns>
        bool IsIpAddressOnCurrentNetwork(IPAddress ipAddress);
    }
}