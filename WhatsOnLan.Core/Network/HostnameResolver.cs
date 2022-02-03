using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace YonatanMankovich.WhatsOnLan.Core.Network
{
    /// <summary>
    /// Provides mehtods for resolving the hostnames of <see cref="IPAddress"/>es.
    /// </summary>
    public class HostnameResolver
    {
        /// <summary>
        /// The DNS suffix to strip of the resolved hostnames. For example, "host.domain.local"
        /// will become "host" for a given suffix of "domain.local".
        /// </summary>
        public string? DnsSuffixToStrip { get; set; }

        /// <summary>
        /// Gets or sets the number of times to try resolving hostnames consecutively.
        /// </summary>
        public int Retries { get; set; } = 1;

        /// <summary>
        /// Resolves the hostnames of the provided <see cref="IPAddress"/>es.
        /// </summary>
        /// <param name="ipAddresses">The <see cref="IPAddress"/>es to resolve hostnames for.</param>
        /// <returns>
        /// The resolved hostnames of the given <see cref="IPAddress"/>es as an <see cref="IDictionary{TKey, TValue}"/>.
        /// If a hostname is not found, <see cref="string.Empty"/> is returned.
        /// </returns>
        public IDictionary<IPAddress, string> ResolveHostnames(IEnumerable<IPAddress> ipAddresses)
        {
            Dictionary<IPAddress, string> resolutions = ipAddresses.ToDictionary(ip => ip, ip => string.Empty);

            Task.WaitAll(ipAddresses.Select(ip => Task.Run(async () =>
            {
                resolutions[ip] = await ResolveHostnameAsync(ip);
            })).ToArray());

            return resolutions;
        }

        /// <summary>
        /// Resolves the hostname of the provided <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/> to resolve a hostname for.</param>
        /// <returns>
        /// The resolved hostname of the given <see cref="IPAddress"/>.
        /// If a hostname is not found, <see cref="string.Empty"/> is returned.
        /// </returns>
        public async Task<string> ResolveHostnameAsync(IPAddress ipAddress)
        {
            int tries = 0;
            do
            {
                try
                {
                    IPHostEntry host = await Dns.GetHostEntryAsync(ipAddress);
                    string hostname = host.HostName;
                    return string.IsNullOrWhiteSpace(DnsSuffixToStrip) ? hostname
                        : hostname.Replace('.' + DnsSuffixToStrip, "", StringComparison.InvariantCultureIgnoreCase);
                }
                catch (SocketException)
                {
                    Debug.WriteLine($"Cannot find the hostname of the IP address of {ipAddress}.");
                }
                tries++;
            } while (tries < Retries);

            return string.Empty;
        }
    }
}