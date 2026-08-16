using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace YonatanMankovich.WhatsOnLan.Core.Network
{
    /// <summary>
    /// Provides methods for resolving the hostnames of <see cref="IPAddress"/>es.
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
        /// Gets or sets the timeout of waiting for hostname resolution responses.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum number of hostname lookups that may run at once.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 128;

        /// <summary>
        /// Resolves the hostnames of the provided <see cref="IPAddress"/>es.
        /// </summary>
        /// <param name="ipAddresses">The <see cref="IPAddress"/>es to resolve hostnames for.</param>
        /// <returns>
        /// The resolved hostnames of the given <see cref="IPAddress"/>es as an <see cref="IDictionary{TKey, TValue}"/>.
        /// If a hostname is not found, <see cref="string.Empty"/> is returned.
        /// </returns>
        public IDictionary<IPAddress, string> ResolveHostnames(IEnumerable<IPAddress> ipAddresses)
            => ResolveHostnamesAsync(ipAddresses).GetAwaiter().GetResult();

        /// <summary>
        /// Resolves the hostnames of the provided <see cref="IPAddress"/>es asynchronously with bounded concurrency.
        /// </summary>
        /// <param name="ipAddresses">The <see cref="IPAddress"/>es to resolve hostnames for.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The resolved hostnames of the given <see cref="IPAddress"/>es.</returns>
        public async Task<IDictionary<IPAddress, string>> ResolveHostnamesAsync(
            IEnumerable<IPAddress> ipAddresses, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ipAddresses);
            if (MaxDegreeOfParallelism <= 0)
                throw new InvalidOperationException("MaxDegreeOfParallelism must be greater than zero.");

            IPAddress[] addresses = ipAddresses.Distinct().ToArray();
            ConcurrentDictionary<IPAddress, string> resolutions = new(addresses.ToDictionary(ip => ip, _ => string.Empty));

            await Parallel.ForEachAsync(addresses, new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            }, async (ip, token) =>
            {
                resolutions[ip] = await ResolveHostnameAsync(ip, token).ConfigureAwait(false);
            }).ConfigureAwait(false);

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
        public string ResolveHostname(IPAddress ipAddress)
            => ResolveHostnameAsync(ipAddress).GetAwaiter().GetResult();

        /// <summary>
        /// Resolves the hostname of the provided <see cref="IPAddress"/> asynchronously.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/> to resolve a hostname for.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The resolved hostname, or <see cref="string.Empty"/> if none is found.</returns>
        public async Task<string> ResolveHostnameAsync(IPAddress ipAddress, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ipAddress);
            if (Timeout <= TimeSpan.Zero)
                throw new InvalidOperationException("Timeout must be greater than zero.");

            int retries = Math.Max(1, Retries);
            int tries = 0;

            do
            {
                try
                {
                    using CancellationTokenSource timeoutCancellation =
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCancellation.CancelAfter(Timeout);

                    IPHostEntry entry = await Dns.GetHostEntryAsync(ipAddress.ToString(), timeoutCancellation.Token)
                        .ConfigureAwait(false);

                    string hostname = entry.HostName;
                    if (string.IsNullOrWhiteSpace(DnsSuffixToStrip))
                        return hostname;

                    string suffix = "." + DnsSuffixToStrip.Trim('.');
                    return hostname.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                        ? hostname[..^suffix.Length]
                        : hostname;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"Hostname resolution of the IP address of {ipAddress} has timed out.");
                }
                catch (SocketException)
                {
                    Debug.WriteLine($"Cannot find the hostname of the IP address of {ipAddress}.");
                }
                tries++;
            } while (tries < retries);

            return string.Empty;
        }
    }
}
