using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using WhatsOnLan.Core.Hardware;
using WhatsOnLan.Core.Network;
using WhatsOnLan.Core.OUI;

namespace WhatsOnLan.Core
{
    /// <summary>
    /// Represents a network scanner.
    /// </summary>
    public class NetworkScanner
    {
        /// <summary>
        /// Indicates whether to send pings to hosts during the scan.
        /// </summary>
        public bool SendPings { get; set; } = true;

        /// <summary>
        /// Indicates whether to send ARP requests to hosts during the scan.
        /// </summary>
        public bool SendArpRequest { get; set; } = true;

        /// <summary>
        /// Indicates whether to resolve hostnames during the scan.
        /// </summary>
        public bool ResolveHostnames { get; set; } = true;

        /// <summary>
        /// Indicates whether to strip the the DNS suffix from the resolved hostname. For example, "host.domain.local"
        /// will become "host" for a given suffix of "domain.local".
        /// </summary>
        public bool StripDnsSuffix { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout of waiting for ARP responses from network hosts.
        /// </summary>
        public TimeSpan ArpTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets an OUI matcher for matching MAC addresses to the corresponding organization name 
        /// (NIC manufacturer) using the IEEE OUI dataset.
        /// </summary>
        public IOuiMatcher? OuiMatcher { get; set; }

        /// <summary>
        /// Gets or sets the network interface of the scanner.
        /// </summary>
        public PcapNetworkInterface Interface { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="NetworkScanner"/> objects with a <see cref="PcapNetworkInterface"/>.
        /// </summary>
        /// <param name="interfaces">The network interface to initialize the scanner with.</param>
        public NetworkScanner(PcapNetworkInterface interfaces)
        {
            Interface = interfaces;
        }

        /// <summary>
        /// Scans a given <see cref="IPAddress"/> on the current network <see cref="Interface"/> and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="ipAddress">The IP address to scan (must be on the same network as the <see cref="Interface"/>).</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<IpScanResult> ScanIpAddressAsync(IPAddress ipAddress)
        {
            if (!IpAddressHelpers.IsOnSameNetwork(ipAddress, Interface.IpAddress, Interface.SubnetMask))
                throw new ArgumentException("The provided IP address and the set network interface are not on the same network.");

            IpScanResult scanResult = new IpScanResult();

            scanResult.IpAddress = ipAddress;
            scanResult.WasPinged = SendPings;
            scanResult.WasArpRequested = SendArpRequest;

            if (SendPings)
                scanResult.RespondedToPing = await Pinger.PingAsync(ipAddress);

            if (SendArpRequest)
            {
                ArpMapper arpMapper = new ArpMapper(Interface)
                {
                    Timeout = ArpTimeout
                };
                PhysicalAddress macAddress = await Task.Run(() => arpMapper.MapIpAddressToMacAddress(ipAddress));
                if (!macAddress.Equals(PhysicalAddress.None))
                {
                    scanResult.MacAddress = macAddress;
                    scanResult.Manufacturer = OuiMatcher?.GetOrganizationName(macAddress);
                }
            }

            if (ResolveHostnames && (scanResult.RespondedToPing || scanResult.RespondedToArp))
                scanResult.Hostname = await HostnameResolver.ResolveHostnameAsync(ipAddress, StripDnsSuffix ? Interface.DnsSuffix : "");

            return scanResult;
        }

        /// <summary>
        /// Scans all the possible IP addresses on the network of the <see cref="Interface"/>.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        public IEnumerable<IpScanResult> ScanNetwork()
        {
            Debug.WriteLine("Getting all IP addresses...");
            IEnumerable<IPAddress> ipAddresses = Interface.GetAllNetworkHostIpAddresses();
            IEnumerable<IPAddress> respondingHosts = Array.Empty<IPAddress>();
            IDictionary<IPAddress, PhysicalAddress> macs;
            IDictionary<IPAddress, bool> pings;
            IDictionary<IPAddress, string> hostnames;

            if (SendArpRequest)
            {
                Debug.WriteLine("Getting all MAC addresses...");
                ArpMapper arpMapper = new ArpMapper(Interface)
                {
                    Timeout = ArpTimeout
                };
                macs = arpMapper.MapIpAddressesToMacAddresses(ipAddresses);
                respondingHosts = macs.Where(m => !m.Value.Equals(PhysicalAddress.None)).Select(m => m.Key);
            }
            else
                macs = ipAddresses.ToDictionary(ip => ip, ip => PhysicalAddress.None);

            if (SendPings)
            {
                Debug.WriteLine("Sending all pings...");
                pings = Pinger.Ping(ipAddresses);
                respondingHosts = respondingHosts.Union(pings.Where(p => p.Value).Select(p => p.Key));
            }
            else
                pings = ipAddresses.ToDictionary(ip => ip, ip => false);

            if (ResolveHostnames)
            {
                Debug.WriteLine("Resolving all responding hostnames...");
                hostnames = HostnameResolver.ResolveHostnames(respondingHosts, StripDnsSuffix ? Interface.DnsSuffix : "");
            }
            else
                hostnames = ipAddresses.ToDictionary(ip => ip, ip => string.Empty);

            Debug.WriteLine("Generating scan results...");
            foreach (IPAddress ip in ipAddresses)
            {
                IpScanResult scanResult = new IpScanResult();

                scanResult.IpAddress = ip;
                scanResult.WasPinged = SendPings;
                scanResult.WasArpRequested = SendArpRequest;

                if (SendPings && pings.ContainsKey(ip))
                    scanResult.RespondedToPing = pings[ip];

                if (ResolveHostnames && hostnames.ContainsKey(ip))
                    scanResult.Hostname = hostnames[ip];

                if (SendArpRequest)
                {
                    PhysicalAddress macAddress = macs[ip];
                    if (!macAddress.Equals(PhysicalAddress.None))
                    {
                        scanResult.MacAddress = macAddress;
                        scanResult.Manufacturer = OuiMatcher?.GetOrganizationName(macAddress);
                    }
                }

                yield return scanResult;
            }
        }
    }
}