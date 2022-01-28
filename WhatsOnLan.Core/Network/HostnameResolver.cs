using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace YonatanMankovich.WhatsOnLan.Core.Network
{
    /// <summary>
    /// Provides mehtods for resolving the hostnames of <see cref="IPAddress"/>es.
    /// </summary>
    public static class HostnameResolver
    {
        /// <summary>
        /// Resolves the hostnames of the provided <see cref="IPAddress"/>es.
        /// </summary>
        /// <param name="ipAddresses">The <see cref="IPAddress"/>es to resolve hostnames for.</param>
        /// <param name="stripDnsSuffix">
        /// The DNS suffix to strip of the resolved hostnames. For example, "host.domain.local"
        /// will become "host" for a given suffix of "domain.local".
        /// </param>
        /// <returns>
        /// The resolved hostnames of the given <see cref="IPAddress"/>es as an <see cref="IDictionary{TKey, TValue}"/>.
        /// If a hostname is not found, <see cref="string.Empty"/> is returned.
        /// </returns>
        public static IDictionary<IPAddress, string> ResolveHostnames(IEnumerable<IPAddress> ipAddresses, string stripDnsSuffix = "")
        {
            Dictionary<IPAddress, string> resolutions = ipAddresses.ToDictionary(ip => ip, ip => string.Empty);
            
            Task.WaitAll(ipAddresses.Select(ip => Task.Run(async () =>
            {
                resolutions[ip] = await ResolveHostnameAsync(ip, stripDnsSuffix);
            })).ToArray());
            
            return resolutions;
        }

        /// <summary>
        /// Resolves the hostname of the provided <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/> to resolve a hostname for.</param>
        /// <param name="stripDnsSuffix">
        /// The DNS suffix to strip of the resolved hostname. For example, "host.domain.local"
        /// will become "host" for a given suffix of "domain.local".
        /// </param>
        /// <returns>
        /// The resolved hostname of the given <see cref="IPAddress"/>.
        /// If a hostname is not found, <see cref="string.Empty"/> is returned.
        /// </returns>
        public static async Task<string> ResolveHostnameAsync(IPAddress ipAddress, string stripDnsSuffix = "")
        {
            string hostname = string.Empty;

            try
            {
                IPHostEntry host = await Dns.GetHostEntryAsync(ipAddress);
                hostname = host.HostName;

                if (!string.IsNullOrWhiteSpace(stripDnsSuffix))
                    hostname = hostname.Replace('.' + stripDnsSuffix, "", StringComparison.InvariantCultureIgnoreCase);
            }
            catch (SocketException)
            {
                Debug.WriteLine($"Cannot find hostname of the IP address of {ipAddress}.");
            }

            return hostname;
        }
    }
}