using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace YonatanMankovich.WhatsOnLan.Core.Network
{
    /// <summary>
    /// Provides mehtods for pinging <see cref="IPAddress"/>es.
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
        /// Pings the provided <see cref="IPAddress"/>es.
        /// </summary>
        /// <param name="ipAddresses">The <see cref="IPAddress"/>es to ping.</param>
        /// <returns>A dictionary of the ping status of each IP address.</returns>
        public IDictionary<IPAddress, bool> PingIpAddresses(IEnumerable<IPAddress> ipAddresses)
        {
            Dictionary<IPAddress, bool> pings = ipAddresses.ToDictionary(ip => ip, ip => false);

            Task.WaitAll(ipAddresses.Select(ip => Task.Run(async () =>
            {
                pings[ip] = await PingIpAddressAsync(ip);
            })).ToArray());

            return pings;
        }

        /// <summary>
        /// Pings a single <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to ping.</param>
        /// <returns><see langword="true"/> if ping was successful; <see langword="false"/> otherwise.</returns>
        public async Task<bool> PingIpAddressAsync(IPAddress ip)
        {
            try
            {
                int tries = 0;

                do
                {
                    PingReply reply = await new Ping().SendPingAsync(ip, Timeout.Milliseconds);

                    if (reply.Status == IPStatus.Success)
                        return true;

                    tries++;
                } while (tries < Retries);
            }
            catch (PingException pe)
            {
                // Discard PingExceptions and return false;
                Debug.WriteLine(pe.Message);
            }

            return false;
        }
    }
}