using System.Collections;
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
                isRunning = value;
                StateHasChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        public event EventHandler? StateHasChanged;

        /// <summary>
        /// Initilizes <see cref="NetworkScanners"/> with all network interfaces available on the current machine.
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
        public async Task<IpScanResult> ScanIpAddressAsync(IPAddress ipAddress)
        {
            if (IsRunning) throw new NetworkScannerRunningException();
            INetworkScanner? scanner = Scanners.FirstOrDefault(s => s.IsIpAddressOnCurrentNetwork(ipAddress));

            if (scanner == null)
                throw new IpAddressNotOnNetworkException(ipAddress);

            IsRunning = true;
            IpScanResult result = await scanner.ScanIpAddressAsync(ipAddress);
            IsRunning = false;
            return result;
        }

        /// <inheritdoc/>
        public async Task<IpScanResult> ScanMacAddressAsync(PhysicalAddress macAddress)
        {
            IpScanResult result = new IpScanResult();

            await Parallel.ForEachAsync(Scanners, async (scanner, cancelToken) =>
            {
                IpScanResult currentResult = await scanner.ScanMacAddressAsync(macAddress);

                if (currentResult.IsOnline)
                    result = currentResult;
            });

            return result;
        }

        /// <inheritdoc/>
        public bool IsIpAddressOnCurrentNetwork(IPAddress ipAddress)
        {
            foreach (INetworkScanner scanner in Scanners)
                if (scanner.IsIpAddressOnCurrentNetwork(ipAddress))
                    return true;

            return false;
        }

        /// <inheritdoc/>
        public Task<IList<IpScanResult>> ScanNetworkAsync() => Task.Run(ScanNetwork);

        /// <inheritdoc/>
        public IList<IpScanResult> ScanNetwork()
        {
            if (IsRunning) throw new NetworkScannerRunningException();
            IsRunning = true;
            IList<IpScanResult> results = Scanners.SelectMany(s => s.ScanNetwork()).ToList();
            IsRunning = false;
            return results;
        }

        /// <inheritdoc/>
        public IEnumerator<INetworkScanner> GetEnumerator() => Scanners.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Scanners.GetEnumerator();
        }
    }
}