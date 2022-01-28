using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace YonatanMankovich.WhatsOnLan.Core.Network
{
    /// <summary>
    /// Provides mehtods for pinging <see cref="IPAddress"/>es.
    /// </summary>
    public static class Pinger
    {
        /// <summary>
        /// Pings the provided <see cref="IPAddress"/>es.
        /// </summary>
        /// <param name="ipAddresses">The <see cref="IPAddress"/>es to ping.</param>
        /// <returns>A dictionary of the ping status of each IP address.</returns>
        public static IDictionary<IPAddress, bool> Ping(IEnumerable<IPAddress> ipAddresses)
        {
            Dictionary<IPAddress, bool> pings = ipAddresses.ToDictionary(ip => ip, ip => false);

            Task.WaitAll(ipAddresses.Select(ip => Task.Run(async () =>
            {
                pings[ip] = await PingAsync(ip);
            })).ToArray());

            return pings;
        }

        /// <summary>
        /// Pings a single <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to ping.</param>
        /// <returns><see langword="true"/> if ping was successful; <see langword="false"/> otherwise.</returns>
        public static async Task<bool> PingAsync(IPAddress ip)
        {
            try
            {
                PingReply reply = await new Ping().SendPingAsync(ip);
                return reply.Status == IPStatus.Success;
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