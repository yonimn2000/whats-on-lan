using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using YonatanMankovich.WhatsOnLan.Core.Exceptions;
using YonatanMankovich.WhatsOnLan.Core.Hardware;
using YonatanMankovich.WhatsOnLan.Core.Helpers;
using YonatanMankovich.WhatsOnLan.Core.Network;

namespace YonatanMankovich.WhatsOnLan.Core
{
    /// <summary>
    /// Represents a network scanner.
    /// </summary>
    public class NetworkScanner : INetworkScanner
    {
        private int isRunning;

        /// <summary>
        /// The network scanner options.
        /// </summary>
        public NetworkScannerOptions Options { get; set; } = new NetworkScannerOptions();

        /// <summary>
        /// Gets or sets the network interface of the scanner.
        /// </summary>
        public PcapNetworkInterface Interface { get; }

        /// <summary>
        /// Gets the running status of the <see cref="NetworkScanner"/>.
        /// </summary>
        public bool IsRunning => Volatile.Read(ref isRunning) != 0;

        /// <summary>
        /// Initializes an instance of the <see cref="NetworkScanner"/> objects with a <see cref="PcapNetworkInterface"/>.
        /// </summary>
        /// <param name="iface">The network interface to initialize the scanner with.</param>
        public NetworkScanner(PcapNetworkInterface iface)
        {
            Interface = iface;
        }

        /// <summary>
        /// The event handler for when the state of the <see cref="NetworkScanner"/> changes.
        /// </summary>
        public event EventHandler? StateHasChanged;

        /// <summary>
        /// Gets a value indicating whether an <see cref="IPAddress"/> is on the same network as the <see cref="NetworkScanner"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/> to check.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the given <paramref name="ipAddress"/> 
        /// is on the same network as the <see cref="NetworkScanner"/>; <see langword="false"/> otherwise.
        /// </returns>
        public bool IsIpAddressOnScannerNetwork(IPAddress ipAddress)
        {
            return IpAddressHelpers.IsOnSameNetwork(ipAddress, Interface.IpAddress, Interface.SubnetMask);
        }

        /// <summary>
        /// Scans all the possible IP addresses on the network of the <see cref="Interface"/>.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        public ICollection<IpScanResult> ScanNetwork()
            => ExecuteScan(ScanNetworkCoreAsync);

        /// <inheritdoc/>
        public Task<ICollection<IpScanResult>> ScanNetworkAsync(CancellationToken cancellationToken = default)
            => ExecuteScanAsync(ScanNetworkCoreAsync, cancellationToken);

        private async Task<ICollection<IpScanResult>> ScanNetworkCoreAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.WriteLine("Getting all reachable IP addresses...");
            IPAddress[] ipAddresses = Interface.GetAllNetworkHostIpAddresses().ToArray();
            Debug.WriteLine($"{ipAddresses.Length:N0} possible hosts on the {Interface.Network} network.");

            return (await ScanIpAddressesCoreAsync(ipAddresses, cancellationToken).ConfigureAwait(false)).Values;
        }

        /// <summary>
        /// Scans the given <see cref="IPAddress"/> on the current network <see cref="Interface"/> 
        /// and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="ipAddress">The IP address to scan (must be on the same network as the <see cref="Interface"/>).</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        /// <exception cref="ArgumentException"></exception>
        public IpScanResult ScanIpAddress(IPAddress ipAddress)
            => ScanIpAddresses(new HashSet<IPAddress>(1) { ipAddress })[ipAddress];

        /// <inheritdoc/>
        public async Task<IpScanResult> ScanIpAddressAsync(
            IPAddress ipAddress, CancellationToken cancellationToken = default)
            => (await ScanIpAddressesAsync(new HashSet<IPAddress>(1) { ipAddress }, cancellationToken)
                .ConfigureAwait(false))[ipAddress];

        /// <summary>
        /// Scans the given <see cref="IPAddress"/>es on the current network <see cref="Interface"/> 
        /// and returns the <see cref="IpScanResult"/>s.
        /// </summary>
        /// <param name="ipAddresses">The IP addresses to scan (must be on the same network as the <see cref="Interface"/>).</param>
        /// <returns>The <see cref="IpScanResult"/>s.</returns>
        /// <exception cref="ArgumentException"></exception>
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

            IDictionary<IPAddress, PhysicalAddress> macs;
            IDictionary<IPAddress, bool> pings;
            IDictionary<IPAddress, string> hostnames;

            if (Options.ShuffleIpAddresses)
                addresses = addresses.OrderBy(_ => Guid.NewGuid()).ToArray();

            Task<IDictionary<IPAddress, PhysicalAddress>>? macResolutionTask = null;
            Task<IDictionary<IPAddress, bool>>? pingTask = null;

            if (Options.SendArpRequest)
            {
                Debug.WriteLine("Resolving MAC addresses...");
                macResolutionTask = Task.Run(
                    () => CreateMacAddressResolver().ResolveMacAddresses(addresses), cancellationToken);
            }

            if (Options.SendPings)
            {
                Debug.WriteLine("Pinging all IP addresses...");
                pingTask = CreatePinger().PingIpAddressesAsync(addresses, cancellationToken);
            }

            if (macResolutionTask is not null && pingTask is not null)
                await Task.WhenAll(macResolutionTask, pingTask).ConfigureAwait(false);

            macs = macResolutionTask is not null
                ? await macResolutionTask.ConfigureAwait(false)
                : addresses.ToDictionary(ip => ip, _ => PhysicalAddress.None);
            pings = pingTask is not null
                ? await pingTask.ConfigureAwait(false)
                : addresses.ToDictionary(ip => ip, _ => false);

            HashSet<IPAddress> arpResponders = macs
                .Where(m => !m.Value.Equals(PhysicalAddress.None))
                .Select(m => m.Key)
                .ToHashSet();

            // A successful ping requires the operating system to know the target's MAC address.
            // Use that fresh ARP-cache entry to supplement missed packet-capture replies, but retain
            // the separate ARP response state so cache data cannot make a host appear online by itself.
            if (Options.SendArpRequest && Options.SendPings)
            {
                IReadOnlyDictionary<IPAddress, PhysicalAddress> cachedMacs = ArpCacheReader.GetEntries(Interface);

                foreach (IPAddress pingResponder in pings.Where(p => p.Value).Select(p => p.Key))
                    if (macs[pingResponder].Equals(PhysicalAddress.None)
                        && cachedMacs.TryGetValue(pingResponder, out PhysicalAddress? cachedMac)
                        && !cachedMac.Equals(PhysicalAddress.None))
                        macs[pingResponder] = cachedMac;
            }

            HashSet<IPAddress> respondingHosts = new(arpResponders);
            respondingHosts.UnionWith(pings.Where(p => p.Value).Select(p => p.Key));

            if (Options.ResolveHostnames)
            {
                Debug.WriteLine("Resolving all responding hostnames...");
                hostnames = await CreateHostnameResolver().ResolveHostnamesAsync(respondingHosts, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
                hostnames = addresses.ToDictionary(ip => ip, _ => string.Empty);

            Debug.WriteLine("Generating scan results...");

            IDictionary<IPAddress, IpScanResult> scanResults = new Dictionary<IPAddress, IpScanResult>();

            foreach (IPAddress ip in addresses)
            {
                IpScanResult scanResult = new IpScanResult
                {
                    IpAddress = ip,
                    WasPinged = Options.SendPings,
                    WasArpRequested = Options.SendArpRequest
                };

                if (Options.SendPings && pings.TryGetValue(ip, out bool respondedToPing))
                    scanResult.RespondedToPing = respondedToPing;

                if (Options.ResolveHostnames && hostnames.TryGetValue(ip, out string? hostname))
                    scanResult.Hostname = hostname;

                if (Options.SendArpRequest)
                {
                    PhysicalAddress macAddress = macs[ip];

                    if (!macAddress.Equals(PhysicalAddress.None))
                    {
                        scanResult.MacAddress = macAddress;
                        scanResult.RespondedToArp = arpResponders.Contains(ip);
                        scanResult.Manufacturer = Options.OuiMatcher?.GetOrganizationName(macAddress);
                    }
                }

                scanResults[ip] = scanResult;
            }

            Debug.WriteLine("Done generating scan results!");

            return scanResults;
        }

        /// <summary>
        /// Scans the given <see cref="PhysicalAddress"/> on the current network <see cref="Interface"/> 
        /// and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="macAddress">The MAC address to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        public IpScanResult ScanMacAddress(PhysicalAddress macAddress)
            => ScanMacAddresses(new HashSet<PhysicalAddress>(1) { macAddress })[macAddress];

        /// <inheritdoc/>
        public async Task<IpScanResult> ScanMacAddressAsync(
            PhysicalAddress macAddress, CancellationToken cancellationToken = default)
            => (await ScanMacAddressesAsync(new HashSet<PhysicalAddress>(1) { macAddress }, cancellationToken)
                .ConfigureAwait(false))[macAddress];

        /// <summary>
        /// Scans the given <see cref="PhysicalAddress"/>es on the current network <see cref="Interface"/> 
        /// and returns the <see cref="IpScanResult"/>s.
        /// </summary>
        /// <param name="macAddresses">The MAC addresses to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>s.</returns>
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

            IDictionary<PhysicalAddress, IpScanResult> results = addresses
                .ToDictionary(mac => mac, mac => new IpScanResult
                {
                    MacAddress = mac,
                    Manufacturer = Options.OuiMatcher?.GetOrganizationName(mac),
                    WasArpRequested = Options.SendArpRequest
                });

            IDictionary<PhysicalAddress, IPAddress> macIpAddresses = CreateIpAddressResolver().ResolveIpAddresses(addresses);
            cancellationToken.ThrowIfCancellationRequested();

            foreach (KeyValuePair<PhysicalAddress, IPAddress> macIpAddress in macIpAddresses)
                results[macIpAddress.Key].IpAddress = macIpAddress.Value;

            IPAddress[] validIpAddresses = macIpAddresses.Values
                        .Where(ip => !ip.Equals(IPAddress.None))
                        .ToArray();

            if (Options.SendArpRequest)
            {
                // If not found an IP, try scanning the whole network.
                if (macIpAddresses.Any(ip => ip.Value.Equals(IPAddress.None)))
                {
                    IDictionary<PhysicalAddress, IpScanResult> networkScanResults = (await ScanNetworkCoreAsync(cancellationToken)
                        .ConfigureAwait(false))
                        .Where(r => results.ContainsKey(r.MacAddress)) // Get only relevant results.
                        .ToDictionary(r => r.MacAddress);

                    foreach (KeyValuePair<PhysicalAddress, IpScanResult> ipScanResult in networkScanResults)
                        results[ipScanResult.Key] = ipScanResult.Value;

                }
                else // If found all IP addresses, reverse scan them.
                {
                    IEnumerable<IpScanResult> ipScanResults = (await ScanIpAddressesCoreAsync(validIpAddresses, cancellationToken)
                        .ConfigureAwait(false))
                        .Select(r => r.Value)
                        .Where(r => !r.MacAddress.Equals(PhysicalAddress.None))
                        .Where(r => results.ContainsKey(r.MacAddress)); // Get only relevant results.

                    foreach (IpScanResult ipScanResult in ipScanResults)
                        results[ipScanResult.MacAddress] = ipScanResult;
                }
            }
            else if (Options.SendPings || Options.ResolveHostnames)
            {
                IDictionary<IPAddress, IpScanResult> resultsByIp = results
                    .Where(r => !r.Value.IpAddress.Equals(IPAddress.None))
                    .ToDictionary(r => r.Value.IpAddress, r => r.Value);

                if (Options.SendPings)
                {
                    IDictionary<IPAddress, bool> pings = await CreatePinger()
                        .PingIpAddressesAsync(validIpAddresses, cancellationToken).ConfigureAwait(false);

                    foreach (KeyValuePair<IPAddress, bool> ping in pings)
                    {
                        resultsByIp[ping.Key].WasPinged = true;
                        resultsByIp[ping.Key].RespondedToPing = ping.Value;
                    }
                }

                if (Options.ResolveHostnames)
                {
                    IDictionary<IPAddress, string> hostnames = await CreateHostnameResolver()
                        .ResolveHostnamesAsync(validIpAddresses, cancellationToken).ConfigureAwait(false);

                    foreach (KeyValuePair<IPAddress, string> ping in hostnames)
                        resultsByIp[ping.Key].Hostname = ping.Value;
                }
            }

            return results;
        }

        private T ExecuteScan<T>(Func<CancellationToken, Task<T>> scan)
        {
            return ExecuteScanAsync(
                cancellationToken => scan(cancellationToken), CancellationToken.None).GetAwaiter().GetResult();
        }

        private T ExecuteScan<T, TInput>(Func<TInput, CancellationToken, Task<T>> scan, TInput input)
        {
            return ExecuteScanAsync(
                cancellationToken => scan(input, cancellationToken), CancellationToken.None).GetAwaiter().GetResult();
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

        private HostnameResolver CreateHostnameResolver()
        {
            HostnameResolver resolver = new HostnameResolver
            {
                Retries = Options.HostnameResolverRetries ?? Options.Repeats,
                Timeout = Options.HostnameResolverTimeout,
                MaxDegreeOfParallelism = Options.HostnameResolverMaxDegreeOfParallelism,
            };

            if (Options.StripDnsSuffix)
                resolver.DnsSuffixToStrip = Interface.DnsSuffix;

            return resolver;
        }

        private Pinger CreatePinger()
        {
            return new Pinger
            {
                Retries = Options.PingerRetries ?? Options.Repeats,
                Timeout = Options.PingerTimeout,
                MaxDegreeOfParallelism = Options.PingerMaxDegreeOfParallelism,
            };
        }

        private MacAddressResolver CreateMacAddressResolver()
        {
            return new MacAddressResolver(Interface)
            {
                Timeout = Options.ArpTimeout,
                Retries = Options.ArpRetries ?? Options.Repeats
            };
        }

        private IpAddressResolver CreateIpAddressResolver()
        {
            return new IpAddressResolver(Interface);
        }
    }
}
