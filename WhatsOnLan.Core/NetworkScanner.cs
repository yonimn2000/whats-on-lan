using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using WhatsOnLan.Core.Hardware;
using WhatsOnLan.Core.Network;
using WhatsOnLan.Core.OUI;

namespace WhatsOnLan.Core
{
    public class NetworkScanner
    {
        public bool SendPings { get; set; } = true;
        public bool ResolveHostnames { get; set; } = true;
        public TimeSpan ArpTimeout { get; set; } = TimeSpan.FromSeconds(1);
        public IOuiMatcher? OuiMatcher { get; set; }
        
        private ISet<PcapNetworkInterface> Interfaces { get; }

        public NetworkScanner()
        {
            Interfaces = NetworkInterfaceHelpers.GetAllPcapNetworkInterfaces().DistinctBy(i => i.Network).ToHashSet();
        }

        public async Task<IpScanResult> ScanIpAddressAsync(IPAddress address)
        {
            PcapNetworkInterface networkInterface = Interfaces.First(i => IpAddressHelpers.IsOnSameNetwork(i.IpAddress, address, i.Subnet));
            IpScanResult scanResult = new();
            PhysicalAddress macAddress = await Task.Run(() => ArpResolver.GetMacAddress(address, networkInterface, ArpTimeout));

            scanResult.IpAddress = address;
            if (!macAddress.Equals(PhysicalAddress.None))
            {
                scanResult.MacAddress = macAddress;
                if (SendPings)
                    scanResult.RespondedToPing = await Pinger.PingAsync(address);
                scanResult.Hostname = await HostnameResolver.GetHostnameAsync(address);
                scanResult.Manufacturer = OuiMatcher?.GetOrganizationName(macAddress);
            }

            return scanResult;
        }

        public IEnumerable<IpScanResult> Scan() => Interfaces.SelectMany(i => ScanIpAddressesForInterface(i));

        public IEnumerable<IpScanResult> ScanIpAddressesForInterface(PcapNetworkInterface networkInterface)
        {
            Debug.WriteLine("Getting all IP addresses...");
            IEnumerable<IPAddress> addresses = networkInterface.GetAllReachableIpAddresses();

            Debug.WriteLine("Getting all MAC addresses...");
            IDictionary<IPAddress, PhysicalAddress> macs = ArpResolver.GetMacAddresses(addresses, networkInterface, ArpTimeout);

            IDictionary<IPAddress, bool> pings = null;
            if (SendPings)
            {
                Debug.WriteLine("Sending all pings...");
                pings = Pinger.Ping(addresses);
            }

            IEnumerable<IPAddress> respondingHosts = macs.Where(m => !m.Value.Equals(PhysicalAddress.None)).Select(m => m.Key);

            if (SendPings)
                respondingHosts = respondingHosts.Union(pings.Where(p => p.Value).Select(p => p.Key));

            IDictionary<IPAddress, string> hostnames = null;
            if (ResolveHostnames)
            {
                Debug.WriteLine("Resolving all responding hostnames...");
                hostnames = HostnameResolver.GetHostnames(respondingHosts);
            }

            Debug.WriteLine("Generating scan results...");
            foreach (IPAddress ip in addresses)
            {
                IpScanResult scanResult = new();
                PhysicalAddress macAddress = macs[ip];

                scanResult.IpAddress = ip;
                if (!macAddress.Equals(PhysicalAddress.None))
                {
                    scanResult.MacAddress = macAddress;
                    if (SendPings)
                        scanResult.RespondedToPing = pings[ip];
                    if (ResolveHostnames)
                        scanResult.Hostname = hostnames[ip];
                    scanResult.Manufacturer = OuiMatcher?.GetOrganizationName(macAddress);
                }

                yield return scanResult;
            }
        }
    }
}