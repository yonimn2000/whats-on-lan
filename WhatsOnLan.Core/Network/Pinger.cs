using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace YonatanMankovich.WhatsOnLan.Core.Network
{
    /// <summary>
    /// Provides methods for pinging <see cref="IPAddress"/>es.
    /// </summary>
    public class Pinger
    {
        /// <summary>
        /// Gets or sets the number of times to try pinging hosts consecutively.
        /// </summary>
        public int Retries { get; set; } = 1;

        /// <summary>
        /// Gets or sets the timeout of waiting for ping responses.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum number of ping operations that may run at once.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 256;

        /// <summary>
        /// Pings the provided <see cref="IPAddress"/>es.
        /// </summary>
        /// <param name="ipAddresses">The <see cref="IPAddress"/>es to ping.</param>
        /// <returns>A dictionary of the ping status of each IP address.</returns>
        public IDictionary<IPAddress, bool> PingIpAddresses(IEnumerable<IPAddress> ipAddresses)
            => PingIpAddressesAsync(ipAddresses).GetAwaiter().GetResult();

        /// <summary>
        /// Pings the provided <see cref="IPAddress"/>es asynchronously with bounded concurrency.
        /// </summary>
        /// <param name="ipAddresses">The <see cref="IPAddress"/>es to ping.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A dictionary of the ping status of each IP address.</returns>
        public async Task<IDictionary<IPAddress, bool>> PingIpAddressesAsync(
            IEnumerable<IPAddress> ipAddresses, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ipAddresses);
            if (MaxDegreeOfParallelism <= 0)
                throw new InvalidOperationException("MaxDegreeOfParallelism must be greater than zero.");

            IPAddress[] addresses = ipAddresses.Distinct().ToArray();
            ConcurrentDictionary<IPAddress, bool> pings = new(addresses.ToDictionary(ip => ip, _ => false));

            await Parallel.ForEachAsync(addresses, new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            }, async (ip, token) =>
            {
                pings[ip] = await PingIpAddressAsync(ip, token).ConfigureAwait(false);
            }).ConfigureAwait(false);

            return pings;
        }

        /// <summary>
        /// Pings a single <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to ping.</param>
        /// <returns><see langword="true"/> if ping was successful; <see langword="false"/> otherwise.</returns>
        public bool PingIpAddress(IPAddress ip) => PingIpAddressAsync(ip).GetAwaiter().GetResult();

        /// <summary>
        /// Pings a single <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to ping.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns><see langword="true"/> if ping was successful; <see langword="false"/> otherwise.</returns>
        public async Task<bool> PingIpAddressAsync(IPAddress ip, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ip);

            int retries = Math.Max(1, Retries);
            TimeSpan timeout = GetTimeout();

            try
            {
                int tries = 0;
                using Ping ping = new();

                do
                {
                    PingReply reply = await ping.SendPingAsync(
                        ip, timeout, Array.Empty<byte>(), new PingOptions(), cancellationToken).ConfigureAwait(false);

                    if (reply.Status == IPStatus.Success)
                        return true;

                    tries++;
                } while (tries < retries);
            }
            catch (PingException pe)
            {
                // Discard PingExceptions and return false;
                Debug.WriteLine(pe.Message);
            }

            return false;
        }

        private TimeSpan GetTimeout()
        {
            if (Timeout <= TimeSpan.Zero)
                throw new InvalidOperationException("Timeout must be greater than zero.");

            return Timeout;
        }
    }
}
