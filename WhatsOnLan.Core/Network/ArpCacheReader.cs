using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using YonatanMankovich.WhatsOnLan.Core.Hardware;
using YonatanMankovich.WhatsOnLan.Core.Helpers;

namespace YonatanMankovich.WhatsOnLan.Core.Network
{
    /// <summary>
    /// Reads ARP cache entries from the operating system for a network interface.
    /// </summary>
    internal static class ArpCacheReader
    {
        /// <summary>
        /// Gets the cached IPv4-to-MAC mappings on the specified interface's network.
        /// </summary>
        public static IReadOnlyDictionary<IPAddress, PhysicalAddress> GetEntries(PcapNetworkInterface networkInterface)
        {
            ArgumentNullException.ThrowIfNull(networkInterface);
            Dictionary<IPAddress, PhysicalAddress> entries = new();

            if (OperatingSystem.IsWindows())
                AddEntries(entries, RunCommand("arp", "-a"), networkInterface,
                    @"(?<ip>([0-9]{1,3}\.?){4})\s*(?<mac>([a-f0-9]{2}-?){6})");
            else
            {
                // iproute2 is the standard Linux interface; net-tools' arp command is retained
                // as a fallback for older distributions.
                string neighbourOutput = RunCommand("ip", "neigh show");
                AddEntries(entries, neighbourOutput, networkInterface,
                    @"(?<ip>([0-9]{1,3}\.?){4}).*?\blladdr\s+(?<mac>([a-f0-9]{2}:){5}[a-f0-9]{2})");

                if (entries.Count == 0)
                    AddEntries(entries, RunCommand("arp", "-e -n"), networkInterface,
                        @"(?<ip>([0-9]{1,3}\.?){4}).*(?<mac>([a-f0-9]{2}:?){6})");
            }

            return entries;
        }

        private static void AddEntries(
            IDictionary<IPAddress, PhysicalAddress> entries,
            string commandOutput,
            PcapNetworkInterface networkInterface,
            string pattern)
        {
            foreach (Match match in Regex.Matches(commandOutput, pattern, RegexOptions.IgnoreCase).Cast<Match>())
            {
                if (!IPAddress.TryParse(match.Groups["ip"].Value, out IPAddress? ipAddress)
                    || !IpAddressHelpers.IsOnSameNetwork(ipAddress, networkInterface.IpAddress, networkInterface.SubnetMask))
                    continue;

                try
                {
                    entries.TryAdd(ipAddress, PhysicalAddress.Parse(match.Groups["mac"].Value));
                }
                catch (FormatException)
                {
                    // Ignore incomplete or malformed neighbour-table entries.
                }
            }
        }

        private static string RunCommand(string fileName, string arguments)
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };

            try
            {
                if (!process.Start())
                    return string.Empty;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return string.Empty;
            }
        }
    }
}
