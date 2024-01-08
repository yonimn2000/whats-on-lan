using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using YonatanMankovich.WhatsOnLan.Core.Exceptions;
using YonatanMankovich.WhatsOnLan.Core.Hardware;

namespace YonatanMankovich.WhatsOnLan.Core
{
    /// <summary>
    /// Provides methods for working with a collection of <see cref="INetworkScanner"/> objects.
    /// This class is especially useful when working with machines that have multiple network interfaces.
    /// </summary>
    public class NetworkScanners : IEnumerable<INetworkScanner>, INetworkScanner
    {
        private bool isRunning;

        /// <summary>
        /// A set of <see cref="INetworkScanner"/> objects.
        /// </summary>
        public ISet<INetworkScanner> Scanners { get; set; } = new HashSet<INetworkScanner>();

        /// <summary>
        /// The network scanners options.
        /// </summary>
        public NetworkScannerOptions Options { get; set; } = new NetworkScannerOptions();

        /// <inheritdoc/>
        public bool IsRunning
        {
            get => isRunning;
            private set
            {
                if (value && isRunning)
                    throw new NetworkScannerRunningException();

                isRunning = value;
                StateHasChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        public event EventHandler? StateHasChanged;

        /// <summary>
        /// Initializes <see cref="NetworkScanners"/> with all network interfaces available on the current machine.
        /// </summary>
        public void InitializeWithAllActiveInterfaces()
        {
            foreach (PcapNetworkInterface iface in NetworkInterfaceHelpers.GetAllDistinctPcapNetworkInterfaces())
                Scanners.Add(new NetworkScanner(iface)
                {
                    Options = Options
                });
        }

        /// <inheritdoc/>
        public bool IsIpAddressOnScannerNetwork(IPAddress ipAddress)
        {
            foreach (INetworkScanner scanner in Scanners)
                if (scanner.IsIpAddressOnScannerNetwork(ipAddress))
                    return true;

            return false;
        }

        /// <inheritdoc/>
        public ICollection<IpScanResult> ScanNetwork()
        {
            IsRunning = true;

            ICollection<IpScanResult> results = Scanners.SelectMany(s => s.ScanNetwork()).ToList();

            IsRunning = false;

            return results;
        }

        /// <inheritdoc/>
        public IpScanResult ScanIpAddress(IPAddress ipAddress)
            => ScanIpAddresses(new HashSet<IPAddress>(1) { ipAddress })[ipAddress];

        /// <inheritdoc/>
        public IDictionary<IPAddress, IpScanResult> ScanIpAddresses(IEnumerable<IPAddress> ipAddresses)
        {
            IsRunning = true;

            IDictionary<INetworkScanner, ISet<IPAddress>> scannerIps = new Dictionary<INetworkScanner, ISet<IPAddress>>();

            // Assign each IP address to its corresponding network scanner.
            foreach (IPAddress ipAddress in ipAddresses)
            {
                INetworkScanner? scanner = Scanners.FirstOrDefault(s => s.IsIpAddressOnScannerNetwork(ipAddress))
                    ?? throw new IpAddressNotOnNetworkException(ipAddress);

                if (!scannerIps.ContainsKey(scanner))
                    scannerIps[scanner] = new HashSet<IPAddress>();

                scannerIps[scanner].Add(ipAddress);
            }

            IDictionary<IPAddress, IpScanResult> results = new ConcurrentDictionary<IPAddress, IpScanResult>();

            Parallel.ForEach(scannerIps, (KeyValuePair<INetworkScanner, ISet<IPAddress>> scannerIpSet) =>
            {
                IDictionary<IPAddress, IpScanResult> scannerResults = scannerIpSet.Key.ScanIpAddresses(scannerIpSet.Value);

                foreach (KeyValuePair<IPAddress, IpScanResult> result in scannerResults)
                    results.Add(result);
            });

            IsRunning = false;

            return results;
        }

        /// <inheritdoc/>
        public IpScanResult ScanMacAddress(PhysicalAddress macAddress)
            => ScanMacAddresses(new HashSet<PhysicalAddress>(1) { macAddress })[macAddress];

        /// <inheritdoc/>
        public IDictionary<PhysicalAddress, IpScanResult> ScanMacAddresses(IEnumerable<PhysicalAddress> macAddresses)
        {
            IsRunning = true;

            IDictionary<PhysicalAddress, IpScanResult> results = new ConcurrentDictionary<PhysicalAddress, IpScanResult>();

            Parallel.ForEach(Scanners, (scanner) =>
            {
                IDictionary<PhysicalAddress, IpScanResult> currentResults = scanner.ScanMacAddresses(macAddresses);

                foreach (KeyValuePair<PhysicalAddress, IpScanResult> result in currentResults)
                    if (!results.ContainsKey(result.Key) || (results.ContainsKey(result.Key) && !results[result.Key].IsOnline))
                        results[result.Key] = result.Value;
            });

            IsRunning = false;

            return results;
        }

        /// <inheritdoc/>
        public IEnumerator<INetworkScanner> GetEnumerator() => Scanners.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Scanners.GetEnumerator();
    }
}