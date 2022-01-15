using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;

namespace WhatsOnLan.Core
{
    public static class NetworkScanner
    {
        public static async Task<IpScanResult> ScanIpAddress(IPAddress address, LibPcapLiveDevice device)
        {
            IpScanResult scanResult = new();
            PhysicalAddress macAddress = await Task.Run(() => ArpResolver.GetMacAddress(address, device));

            scanResult.IpAddress = address;
            if (!macAddress.Equals(PhysicalAddress.None))
            {
                scanResult.MacAddress = macAddress;
                scanResult.RespondedToPing = await Pinger.Ping(address);
                scanResult.Hostname = await HostnameResolver.GetHostname(address);
            }

            return scanResult;
        }

        public static IEnumerable<IpScanResult> ScanIpAddresses(IEnumerable<IPAddress> addresses, LibPcapLiveDevice device)
        {
            IDictionary<IPAddress, PhysicalAddress> macs = ArpResolver.GetMacAddresses(addresses, device);
            IDictionary<IPAddress, bool> pings = Pinger.Ping(addresses);
            
            IEnumerable<IPAddress> respondingHosts = macs.Where(m => !m.Value.Equals(PhysicalAddress.None)).Select(m => m.Key)
                .Union(pings.Where(p => p.Value).Select(p => p.Key));
            IDictionary<IPAddress, string> hostnames = HostnameResolver.GetHostnames(respondingHosts);

            ISet<IpScanResult> results = new HashSet<IpScanResult>();

            foreach (IPAddress ip in addresses)
            {
                IpScanResult scanResult = new();
                PhysicalAddress macAddress = macs[ip];

                scanResult.IpAddress = ip;
                if (!macAddress.Equals(PhysicalAddress.None))
                {
                    scanResult.MacAddress = macAddress;
                    scanResult.RespondedToPing = pings[ip];
                    scanResult.Hostname = hostnames[ip];
                }

                results.Add(scanResult);
            }

            return results;
        }
    }
}