using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using YonatanMankovich.WhatsOnLan.Core.Hardware;

namespace YonatanMankovich.WhatsOnLan.Core.Network
{
    /// <summary>
    /// Provides methods for mapping MAC addresses to IP addresses using ARP.
    /// </summary>
    public class IpAddressResolver
    {
        private PcapNetworkInterface NetworkInterface { get; }

        /// <summary>
        /// Initializes an instance of the <see cref="IpAddressResolver"/> class 
        /// to be used using the provided <see cref="PcapNetworkInterface"/> instance.
        /// </summary>
        /// <param name="networkInterface">A <see cref="PcapNetworkInterface"/> to initialize the instance to.</param>
        public IpAddressResolver(PcapNetworkInterface networkInterface)
        {
            NetworkInterface = networkInterface;
        }

        /// <summary>
        /// Maps a <see cref="PhysicalAddress"/> to a <see cref="IPAddress"/> by sending an ARP request and waiting for a response.
        /// </summary>
        /// <param name="macAddress">The <see cref="PhysicalAddress"/> to map to a <see cref="IPAddress"/>.</param>
        /// <returns>The mapped <see cref="PhysicalAddress"/>.</returns>
        public IPAddress ResolveIpAddress(PhysicalAddress macAddress)
        {
            return ResolveIpAddresses(new PhysicalAddress[] { macAddress })[macAddress];
        }

        /// <summary>
        /// Maps many <see cref="PhysicalAddress"/>es to many <see cref="IPAddress"/>es by sending ARP requests and waiting for responses.
        /// </summary>
        /// <param name="macAddresses">
        /// The <see cref="IEnumerable{T}"/> that contains all the <see cref="PhysicalAddress"/>es
        /// to map to <see cref="PhysicalAddress"/>es.
        /// </param>
        /// <returns>The mapped <see cref="PhysicalAddress"/>es as an <see cref="IDictionary{TKey, TValue}"/>.</returns>
        public IDictionary<PhysicalAddress, IPAddress> ResolveIpAddresses(IEnumerable<PhysicalAddress> macAddresses)
        {
            Dictionary<PhysicalAddress, IPAddress> resolutions = macAddresses.ToDictionary(mac => mac, mac => IPAddress.None);

            // Add the IP of the current device to the dictionary.
            if (resolutions.ContainsKey(NetworkInterface.MacAddress))
                resolutions[NetworkInterface.MacAddress] = NetworkInterface.IpAddress;

            GetArpMacIps(resolutions);

            return resolutions;
        }

        private static void GetArpMacIps(IDictionary<PhysicalAddress, IPAddress> mip)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = OperatingSystem.IsWindows() ? "-a" : "-e -n",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            string cmdOutput = process.StandardOutput.ReadToEnd();
            string pattern = OperatingSystem.IsWindows()
                ? @"(?<ip>([0-9]{1,3}\.?){4})\s*(?<mac>([a-f0-9]{2}-?){6})"
                : @"(?<ip>([0-9]{1,3}\.?){4}).*(?<mac>([a-f0-9]{2}:?){6})";

            foreach (Match m in Regex.Matches(cmdOutput, pattern, RegexOptions.IgnoreCase).Cast<Match>())
            {
                PhysicalAddress mac = PhysicalAddress.Parse(m.Groups["mac"].Value);

                if (mip.ContainsKey(mac) && mip[mac] == IPAddress.None)
                    mip[mac] = IPAddress.Parse(m.Groups["ip"].Value);
            }
        }
    }
}