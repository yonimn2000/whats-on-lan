using System.Net;
using System.Net.NetworkInformation;

namespace WhatsOnLan.Core.Network
{
    public static class Pinger
    {
        public static IDictionary<IPAddress, bool> Ping(IEnumerable<IPAddress> ipAddresses)
        {
            Dictionary<IPAddress, bool> pings = ipAddresses.ToDictionary(ip => ip, ip => false);
            List<Task> tasks = new List<Task>();
            foreach (IPAddress ip in ipAddresses)
                tasks.Add(Task.Run(async () =>
                {
                    pings[ip] = await Ping(ip);
                }));

            Task.WaitAll(tasks.ToArray());

            return pings;
        }

        public static async Task<bool> Ping(IPAddress ip)
        {
            Ping pinger = new Ping();

            try
            {
                PingReply reply = await pinger.SendPingAsync(ip);
                return reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }

            return false;
        }
    }
}