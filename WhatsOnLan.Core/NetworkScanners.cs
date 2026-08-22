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
        private int isRunning;
        private readonly object scannersLock = new();

        /// <summary>
        /// A set of <see cref="INetworkScanner"/> objects.
        /// </summary>
        public ISet<INetworkScanner> Scanners { get; set; } = new HashSet<INetworkScanner>();

        /// <summary>
        /// The network scanners options.
        /// </summary>
        public NetworkScannerOptions Options { get; set; } = new NetworkScannerOptions();

        /// <inheritdoc/>
        public bool IsRunning => Volatile.Read(ref isRunning) != 0;

        /// <summary>
        /// Gets a value indicating whether at least one network interface is configured.
        /// </summary>
        public bool HasConfiguredInterfaces => GetScannersSnapshot().Length > 0;

        /// <inheritdoc/>
        public event EventHandler? StateHasChanged;

        /// <summary>
        /// Initializes <see cref="NetworkScanners"/> with all network interfaces available on the current machine.
        /// </summary>
        public void InitializeWithAllActiveInterfaces()
        {
            Configure(NetworkInterfaceHelpers.GetAllDistinctPcapNetworkInterfaces(), Options);
        }

        /// <summary>
        /// Replaces the network interfaces used by this scanner collection.
        /// </summary>
        public void Configure(
            IEnumerable<PcapNetworkInterface> interfaces,
            NetworkScannerOptions options)
        {
            ArgumentNullException.ThrowIfNull(interfaces);
            ArgumentNullException.ThrowIfNull(options);

            HashSet<INetworkScanner> scanners = interfaces
                .Select(iface => (INetworkScanner)new NetworkScanner(iface)
                {
                    Options = options
                })
                .ToHashSet();

            lock (scannersLock)
            {
                Options = options;
                Scanners = scanners;
            }

            StateHasChanged?.Invoke(this, System.EventArgs.Empty);
        }

        /// <inheritdoc/>
        public bool IsIpAddressOnScannerNetwork(IPAddress ipAddress)
        {
            foreach (INetworkScanner scanner in GetScannersSnapshot())
                if (scanner.IsIpAddressOnScannerNetwork(ipAddress))
                    return true;

            return false;
        }

        /// <inheritdoc/>
        public ICollection<IpScanResult> ScanNetwork()
            => ExecuteScan(ScanNetworkCoreAsync);

        /// <inheritdoc/>
        public Task<ICollection<IpScanResult>> ScanNetworkAsync(CancellationToken cancellationToken = default)
            => ExecuteScanAsync(ScanNetworkCoreAsync, cancellationToken);

        private async Task<ICollection<IpScanResult>> ScanNetworkCoreAsync(CancellationToken cancellationToken)
        {
            INetworkScanner[] scanners = GetScannersSnapshot();
            ICollection<IpScanResult>[] scannerResults = await Task.WhenAll(
                scanners.Select(scanner => StartScannerOperation(
                    () => scanner.ScanNetworkAsync(cancellationToken), cancellationToken))).ConfigureAwait(false);

            return scannerResults.SelectMany(results => results).ToList();
        }

        /// <inheritdoc/>
        public IpScanResult ScanIpAddress(IPAddress ipAddress)
            => ScanIpAddresses(new HashSet<IPAddress>(1) { ipAddress })[ipAddress];

        /// <inheritdoc/>
        public async Task<IpScanResult> ScanIpAddressAsync(
            IPAddress ipAddress, CancellationToken cancellationToken = default)
            => (await ScanIpAddressesAsync(new HashSet<IPAddress>(1) { ipAddress }, cancellationToken)
                .ConfigureAwait(false))[ipAddress];

        /// <inheritdoc/>
        public IDictionary<IPAddress, IpScanResult> ScanIpAddresses(IEnumerable<IPAddress> ipAddresses)
            => ExecuteScan(ScanIpAddressesCoreAsync, ipAddresses);

        /// <inheritdoc/>
        public Task<IDictionary<IPAddress, IpScanResult>> ScanIpAddressesAsync(
            IEnumerable<IPAddress> ipAddresses, CancellationToken cancellationToken = default)
            => ExecuteScanAsync(ct => ScanIpAddressesCoreAsync(ipAddresses, ct), cancellationToken);

        private async Task<IDictionary<IPAddress, IpScanResult>> ScanIpAddressesCoreAsync(
            IEnumerable<IPAddress> ipAddresses, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(ipAddresses);
            cancellationToken.ThrowIfCancellationRequested();
            IPAddress[] addresses = ipAddresses.Distinct().ToArray();
            INetworkScanner[] scanners = GetScannersSnapshot();

            IDictionary<INetworkScanner, ISet<IPAddress>> scannerIps = new Dictionary<INetworkScanner, ISet<IPAddress>>();

            // Assign each IP address to its corresponding network scanner.
            foreach (IPAddress ipAddress in addresses)
            {
                INetworkScanner? scanner = scanners.FirstOrDefault(s => s.IsIpAddressOnScannerNetwork(ipAddress))
                    ?? throw new IpAddressNotOnNetworkException(ipAddress);

                if (!scannerIps.ContainsKey(scanner))
                    scannerIps[scanner] = new HashSet<IPAddress>();

                scannerIps[scanner].Add(ipAddress);
            }

            Task<IDictionary<IPAddress, IpScanResult>>[] scanTasks = scannerIps
                .Select(scannerIpSet => StartScannerOperation(
                    () => scannerIpSet.Key.ScanIpAddressesAsync(scannerIpSet.Value, cancellationToken),
                    cancellationToken))
                .ToArray();
            IDictionary<IPAddress, IpScanResult>[] scannerResults = await Task.WhenAll(scanTasks).ConfigureAwait(false);
            ConcurrentDictionary<IPAddress, IpScanResult> results = new();

            foreach (IDictionary<IPAddress, IpScanResult> scannerResult in scannerResults)
                foreach (KeyValuePair<IPAddress, IpScanResult> result in scannerResult)
                    results.TryAdd(result.Key, result.Value);

            return results;
        }

        /// <inheritdoc/>
        public IpScanResult ScanMacAddress(PhysicalAddress macAddress)
            => ScanMacAddresses(new HashSet<PhysicalAddress>(1) { macAddress })[macAddress];

        /// <inheritdoc/>
        public async Task<IpScanResult> ScanMacAddressAsync(
            PhysicalAddress macAddress, CancellationToken cancellationToken = default)
            => (await ScanMacAddressesAsync(new HashSet<PhysicalAddress>(1) { macAddress }, cancellationToken)
                .ConfigureAwait(false))[macAddress];

        /// <inheritdoc/>
        public IDictionary<PhysicalAddress, IpScanResult> ScanMacAddresses(IEnumerable<PhysicalAddress> macAddresses)
            => ExecuteScan(ScanMacAddressesCoreAsync, macAddresses);

        /// <inheritdoc/>
        public Task<IDictionary<PhysicalAddress, IpScanResult>> ScanMacAddressesAsync(
            IEnumerable<PhysicalAddress> macAddresses, CancellationToken cancellationToken = default)
            => ExecuteScanAsync(ct => ScanMacAddressesCoreAsync(macAddresses, ct), cancellationToken);

        private async Task<IDictionary<PhysicalAddress, IpScanResult>> ScanMacAddressesCoreAsync(
            IEnumerable<PhysicalAddress> macAddresses, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(macAddresses);
            cancellationToken.ThrowIfCancellationRequested();
            PhysicalAddress[] addresses = macAddresses.Distinct().ToArray();
            INetworkScanner[] scanners = GetScannersSnapshot();

            IDictionary<PhysicalAddress, IpScanResult>[] scannerResults = await Task.WhenAll(
                scanners.Select(scanner => StartScannerOperation(
                    () => scanner.ScanMacAddressesAsync(addresses, cancellationToken), cancellationToken)))
                .ConfigureAwait(false);
            ConcurrentDictionary<PhysicalAddress, IpScanResult> results = new();

            foreach (IDictionary<PhysicalAddress, IpScanResult> scannerResult in scannerResults)
                foreach (KeyValuePair<PhysicalAddress, IpScanResult> result in scannerResult)
                    results.AddOrUpdate(result.Key, result.Value, (_, existing)
                        => existing.IsOnline ? existing : result.Value);

            return results;
        }

        private static Task<T> StartScannerOperation<T>(
            Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            // NetworkScanner performs a synchronous ARP capture before its first await.
            // Start each interface operation independently so that blocking ARP work does
            // not serialize the multi-interface scan while the async ping/DNS phases remain
            // asynchronous and bounded.
            return Task.Run(operation, cancellationToken);
        }

        private T ExecuteScan<T>(Func<CancellationToken, Task<T>> scan)
        {
            return ExecuteScanAsync(scan, CancellationToken.None).GetAwaiter().GetResult();
        }

        private T ExecuteScan<T, TInput>(Func<TInput, CancellationToken, Task<T>> scan, TInput input)
        {
            return ExecuteScanAsync(ct => scan(input, ct), CancellationToken.None).GetAwaiter().GetResult();
        }

        private async Task<T> ExecuteScanAsync<T>(
            Func<CancellationToken, Task<T>> scan, CancellationToken cancellationToken)
        {
            StartScan();
            try
            {
                return await scan(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                StopScan();
            }
        }

        private void StartScan()
        {
            if (Interlocked.Exchange(ref isRunning, 1) != 0)
                throw new NetworkScannerRunningException();

            try
            {
                StateHasChanged?.Invoke(this, System.EventArgs.Empty);
            }
            catch
            {
                Volatile.Write(ref isRunning, 0);
                throw;
            }
        }

        private void StopScan()
        {
            Volatile.Write(ref isRunning, 0);
            StateHasChanged?.Invoke(this, System.EventArgs.Empty);
        }

        /// <inheritdoc/>
        public IEnumerator<INetworkScanner> GetEnumerator() => ((IEnumerable<INetworkScanner>)GetScannersSnapshot()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetScannersSnapshot().GetEnumerator();

        private INetworkScanner[] GetScannersSnapshot()
        {
            lock (scannersLock)
                return Scanners.ToArray();
        }
    }
}
