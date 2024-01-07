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
        private bool isRunning;

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
        public bool IsRunning
        {
            get => isRunning;
            private set
            {
                isRunning = value;
                StateHasChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

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
        public bool IsIpAddressOnCurrentNetwork(IPAddress ipAddress)
        {
            return IpAddressHelpers.IsOnSameNetwork(ipAddress, Interface.IpAddress, Interface.SubnetMask);
        }

        /// <summary>
        /// Scans a given <see cref="IPAddress"/> on the current network <see cref="Interface"/> 
        /// and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="ipAddress">The IP address to scan (must be on the same network as the <see cref="Interface"/>).</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<IpScanResult> ScanIpAddressAsync(IPAddress ipAddress)
        {
            if (IsRunning) throw new NetworkScannerRunningException();
            IsRunning = true;

            if (!IsIpAddressOnCurrentNetwork(ipAddress))
                throw new IpAddressNotOnNetworkException(ipAddress, Interface.Network, Interface.SubnetMask);

            IpScanResult scanResult = new IpScanResult
            {
                IpAddress = ipAddress,
                WasPinged = Options.SendPings,
                WasArpRequested = Options.SendArpRequest
            };

            if (Options.SendPings)
                scanResult.RespondedToPing = await CreatePinger().PingIpAddressAsync(ipAddress);

            if (Options.SendArpRequest)
            {
                PhysicalAddress macAddress = await Task.Run(() => CreateMacAddressResolver().ResolveMacAddress(ipAddress));
                if (!macAddress.Equals(PhysicalAddress.None))
                {
                    scanResult.MacAddress = macAddress;
                    scanResult.Manufacturer = Options.OuiMatcher?.GetOrganizationName(macAddress);
                }
            }

            if (Options.ResolveHostnames && (scanResult.RespondedToPing || scanResult.RespondedToArp))
                scanResult.Hostname = await CreateHostnameResolver().ResolveHostnameAsync(ipAddress);

            IsRunning = false;
            return scanResult;
        }

        /// <summary>
        /// Scans a given <see cref="PhysicalAddress"/> on the current network <see cref="Interface"/> 
        /// and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="macAddress">The MAC address to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        public async Task<IpScanResult> ScanMacAddressAsync(PhysicalAddress macAddress)
        {
            IpScanResult scanResult = new IpScanResult
            {
                MacAddress = macAddress,
                Manufacturer = Options.OuiMatcher?.GetOrganizationName(macAddress),
                WasPinged = Options.SendPings,
                WasArpRequested = Options.SendArpRequest
            };

            IPAddress ipAddress = await Task.Run(() => CreateIpAddressResolver().ResolveIpAddress(macAddress));
            bool sentArp = false;

            // If not found IP, try scanning the whole network.
            if (ipAddress == IPAddress.None && Options.SendArpRequest)
            {
                NetworkScanner networkScanner = new NetworkScanner(Interface)
                {
                    Options = new NetworkScannerOptions
                    {
                        ArpTimeout = Options.ArpTimeout,
                        SendArpRequest = true,
                        Repeats = Options.Repeats,
                        ShuffleIpAddresses = Options.ShuffleIpAddresses,
                        ResolveHostnames = false,
                        SendPings = false,
                    }
                };

                ipAddress = (await networkScanner.ScanNetworkAsync())
                    .Where(r => r.MacAddress.Equals(macAddress))
                    .Select(r => r.IpAddress)
                    .FirstOrDefault() ?? IPAddress.None;

                sentArp = true;
            }

            if (ipAddress != IPAddress.None)
            {
                scanResult.IpAddress = ipAddress;

                // If ARP is requested, reverse scan the IP address.
                if (Options.SendArpRequest && !sentArp)
                {
                    IpScanResult ipScanResult = await ScanIpAddressAsync(ipAddress);

                    if (ipScanResult.MacAddress.Equals(macAddress))
                        return ipScanResult;
                }

                if (Options.SendPings)
                    scanResult.RespondedToPing = await CreatePinger().PingIpAddressAsync(scanResult.IpAddress);

                if (Options.ResolveHostnames && (scanResult.RespondedToPing || scanResult.RespondedToArp))
                    scanResult.Hostname = await CreateHostnameResolver().ResolveHostnameAsync(scanResult.IpAddress);
            }

            return scanResult;
        }

        /// <summary>
        /// Scans all the possible IP addresses on the network of the <see cref="Interface"/> asynchronously.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        public Task<IList<IpScanResult>> ScanNetworkAsync() => Task.Run(ScanNetwork);

        /// <summary>
        /// Scans all the possible IP addresses on the network of the <see cref="Interface"/>.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        public IList<IpScanResult> ScanNetwork()
        {
            if (IsRunning) throw new NetworkScannerRunningException();
            IsRunning = true;

            Debug.WriteLine("Getting all reachable IP addresses...");
            IEnumerable<IPAddress> ipAddresses = Interface.GetAllNetworkHostIpAddresses();
            IEnumerable<IPAddress> respondingHosts = Array.Empty<IPAddress>();
            IDictionary<IPAddress, PhysicalAddress> macs;
            IDictionary<IPAddress, bool> pings;
            IDictionary<IPAddress, string> hostnames;

            if (Options.ShuffleIpAddresses)
                ipAddresses = ipAddresses.OrderBy(ip => Guid.NewGuid());

            if (Options.SendArpRequest)
            {
                Debug.WriteLine("Resolving MAC addresses...");
                macs = CreateMacAddressResolver().ResolveMacAddresses(ipAddresses);
                respondingHosts = macs.Where(m => !m.Value.Equals(PhysicalAddress.None)).Select(m => m.Key);
            }
            else
                macs = ipAddresses.ToDictionary(ip => ip, ip => PhysicalAddress.None);

            if (Options.SendPings)
            {
                Debug.WriteLine("Pinging all IP addresses...");
                pings = CreatePinger().PingIpAddresses(ipAddresses);
                respondingHosts = respondingHosts.Union(pings.Where(p => p.Value).Select(p => p.Key));
            }
            else
                pings = ipAddresses.ToDictionary(ip => ip, ip => false);

            if (Options.ResolveHostnames)
            {
                Debug.WriteLine("Resolving all responding hostnames...");
                hostnames = CreateHostnameResolver().ResolveHostnames(respondingHosts);
            }
            else
                hostnames = ipAddresses.ToDictionary(ip => ip, ip => string.Empty);

            Debug.WriteLine("Generating scan results...");
            IList<IpScanResult> scanResults = new List<IpScanResult>();
            foreach (IPAddress ip in ipAddresses)
            {
                IpScanResult scanResult = new IpScanResult();

                scanResult.IpAddress = ip;
                scanResult.WasPinged = Options.SendPings;
                scanResult.WasArpRequested = Options.SendArpRequest;

                if (Options.SendPings && pings.ContainsKey(ip))
                    scanResult.RespondedToPing = pings[ip];

                if (Options.ResolveHostnames && hostnames.ContainsKey(ip))
                    scanResult.Hostname = hostnames[ip];

                if (Options.SendArpRequest)
                {
                    PhysicalAddress macAddress = macs[ip];
                    if (!macAddress.Equals(PhysicalAddress.None))
                    {
                        scanResult.MacAddress = macAddress;
                        scanResult.Manufacturer = Options.OuiMatcher?.GetOrganizationName(macAddress);
                    }
                }

                scanResults.Add(scanResult);
            }
            Debug.WriteLine("Done generating scan results!");
            IsRunning = false;
            return scanResults;
        }

        private HostnameResolver CreateHostnameResolver()
        {
            HostnameResolver resolver = new HostnameResolver
            {
                Retries = Options.Repeats
            };

            if (Options.StripDnsSuffix)
                resolver.DnsSuffixToStrip = Interface.DnsSuffix;

            return resolver;
        }

        private Pinger CreatePinger()
        {
            return new Pinger
            {
                Retries = Options.Repeats
            };
        }

        private MacAddressResolver CreateMacAddressResolver()
        {
            return new MacAddressResolver(Interface)
            {
                Timeout = Options.ArpTimeout,
                Retries = Options.Repeats
            };
        }

        private IpAddressResolver CreateIpAddressResolver()
        {
            return new IpAddressResolver(Interface)
            {
                Timeout = Options.ArpTimeout,
                Retries = Options.Repeats
            };
        }
    }
}