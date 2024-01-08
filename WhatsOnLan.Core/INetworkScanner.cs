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
        /// Gets the status of the network scans. <see langword="true"/> if the scanner is running; <see langword="false"/> otherwise.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Scans the given <see cref="IPAddress"/> and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="ipAddress">The IP address to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        IpScanResult ScanIpAddress(IPAddress ipAddress);

        /// <summary>
        /// Scans the given <see cref="IPAddress"/>es and returns the <see cref="IpScanResult"/>s.
        /// </summary>
        /// <param name="ipAddresses">The IP addresses to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>s.</returns>
        IDictionary<IPAddress, IpScanResult> ScanIpAddresses(IEnumerable<IPAddress> ipAddresses);

        /// <summary>
        /// Scans the given <see cref="PhysicalAddress"/> and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="macAddress">The MAC address to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        IpScanResult ScanMacAddress(PhysicalAddress macAddress);

        /// <summary>
        /// Scans the given <see cref="PhysicalAddress"/>es and returns the <see cref="IpScanResult"/>s.
        /// </summary>
        /// <param name="macAddresses">The MAC addresses to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>s.</returns>
        IDictionary<PhysicalAddress, IpScanResult> ScanMacAddresses(IEnumerable<PhysicalAddress> macAddresses);

        /// <summary>
        /// Scans all the possible IP addresses on the network.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        ICollection<IpScanResult> ScanNetwork();

        /// <summary>
        /// Checks if the provided <see cref="IPAddress"/> is on the network of the scanner.
        /// </summary>
        /// <param name="ipAddress"> The IP address. </param>
        /// <returns><see langword="true"/> if the IP address on the network of the scanner; <see langword="false"/> otherwise.</returns>
        bool IsIpAddressOnScannerNetwork(IPAddress ipAddress);
    }
}