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
                if (value && isRunning)
                    throw new NetworkScannerRunningException();

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
        public bool IsIpAddressOnScannerNetwork(IPAddress ipAddress)
        {
            return IpAddressHelpers.IsOnSameNetwork(ipAddress, Interface.IpAddress, Interface.SubnetMask);
        }

        /// <summary>
        /// Scans all the possible IP addresses on the network of the <see cref="Interface"/>.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        public ICollection<IpScanResult> ScanNetwork()
        {
            Debug.WriteLine("Getting all reachable IP addresses...");
            IPAddress[] ipAddresses = Interface.GetAllNetworkHostIpAddresses().ToArray();
            Debug.WriteLine($"{ipAddresses.Length:N0} possible hosts on the {Interface.Network} network.");

            return ScanIpAddresses(ipAddresses).Values;
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

        /// <summary>
        /// Scans the given <see cref="IPAddress"/>es on the current network <see cref="Interface"/> 
        /// and returns the <see cref="IpScanResult"/>s.
        /// </summary>
        /// <param name="ipAddresses">The IP addresses to scan (must be on the same network as the <see cref="Interface"/>).</param>
        /// <returns>The <see cref="IpScanResult"/>s.</returns>
        /// <exception cref="ArgumentException"></exception>
        public IDictionary<IPAddress, IpScanResult> ScanIpAddresses(IEnumerable<IPAddress> ipAddresses)
        {
            IsRunning = true;

            IEnumerable<IPAddress> respondingHosts = Array.Empty<IPAddress>();
            IDictionary<IPAddress, PhysicalAddress> macs;
            IDictionary<IPAddress, bool> pings;
            IDictionary<IPAddress, string> hostnames;

            if (Options.ShuffleIpAddresses)
                ipAddresses = ipAddresses.OrderBy(ip => Guid.NewGuid()).ToArray();

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

            IDictionary<IPAddress, IpScanResult> scanResults = new Dictionary<IPAddress, IpScanResult>();

            foreach (IPAddress ip in ipAddresses)
            {
                IpScanResult scanResult = new IpScanResult
                {
                    IpAddress = ip,
                    WasPinged = Options.SendPings,
                    WasArpRequested = Options.SendArpRequest
                };

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

                scanResults[ip] = scanResult;
            }

            Debug.WriteLine("Done generating scan results!");

            IsRunning = false;

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

        /// <summary>
        /// Scans the given <see cref="PhysicalAddress"/>es on the current network <see cref="Interface"/> 
        /// and returns the <see cref="IpScanResult"/>s.
        /// </summary>
        /// <param name="macAddresses">The MAC addresses to scan.</param>
        /// <returns>The <see cref="IpScanResult"/>s.</returns>
        public IDictionary<PhysicalAddress, IpScanResult> ScanMacAddresses(IEnumerable<PhysicalAddress> macAddresses)
        {
            IsRunning = true;

            IDictionary<PhysicalAddress, IpScanResult> results = macAddresses
                .ToDictionary(mac => mac, mac => new IpScanResult
                {
                    MacAddress = mac,
                    Manufacturer = Options.OuiMatcher?.GetOrganizationName(mac),
                    WasArpRequested = Options.SendArpRequest
                });

            IDictionary<PhysicalAddress, IPAddress> macIpAddresses = CreateIpAddressResolver().ResolveIpAddresses(macAddresses);

            foreach (KeyValuePair<PhysicalAddress, IPAddress> macIpAddress in macIpAddresses)
                results[macIpAddress.Key].IpAddress = macIpAddress.Value;

            IPAddress[] validIpAddresses = macIpAddresses.Values
                        .Where(ip => !ip.Equals(IPAddress.None))
                        .ToArray();

            if (Options.SendArpRequest)
            {
                // If not found an IP, try scanning the whole network.
                if (macIpAddresses.Any(ip => ip.Equals(IPAddress.None)))
                {
                    IsRunning = false;

                    IDictionary<PhysicalAddress, IpScanResult> networkScanResults = ScanNetwork()
                        .Where(r => results.ContainsKey(r.MacAddress)) // Get only relevant results.
                        .ToDictionary(r => r.MacAddress);

                    IsRunning = true;

                    foreach (KeyValuePair<PhysicalAddress, IpScanResult> ipScanResult in networkScanResults)
                        results[ipScanResult.Key] = ipScanResult.Value;

                }
                else // If found all IP addresses, reverse scan them.
                {
                    IsRunning = false;

                    IEnumerable<IpScanResult> ipScanResults = ScanIpAddresses(validIpAddresses)
                        .Select(r => r.Value)
                        .Where(r => !r.MacAddress.Equals(PhysicalAddress.None))
                        .Where(r => results.ContainsKey(r.MacAddress)); // Get only relevant results.

                    IsRunning = true;

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
                    IDictionary<IPAddress, bool> pings = CreatePinger().PingIpAddresses(validIpAddresses);

                    foreach (KeyValuePair<IPAddress, bool> ping in pings)
                        resultsByIp[ping.Key].RespondedToPing = ping.Value;
                }

                if (Options.ResolveHostnames)
                {
                    IDictionary<IPAddress, string> hostnames = CreateHostnameResolver().ResolveHostnames(validIpAddresses);

                    foreach (KeyValuePair<IPAddress, string> ping in hostnames)
                        resultsByIp[ping.Key].Hostname = ping.Value;
                }
            }

            IsRunning = false;

            return results;
        }

        private HostnameResolver CreateHostnameResolver()
        {
            HostnameResolver resolver = new HostnameResolver
            {
                Retries = Options.Repeats,
                Timeout = Options.HostnameResolverTimeout,
            };

            if (Options.StripDnsSuffix)
                resolver.DnsSuffixToStrip = Interface.DnsSuffix;

            return resolver;
        }

        private Pinger CreatePinger()
        {
            return new Pinger
            {
                Retries = Options.Repeats,
                Timeout = Options.PingerTimeout,
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
            return new IpAddressResolver(Interface);
        }
    }
}