using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace WhatsOnLan.Core.Network
{
    public static class HostnameResolver
    {
        public static IDictionary<IPAddress, string> GetHostnames(IEnumerable<IPAddress> ipAddresses, string stripDnsSuffix = "")
        {
            Dictionary<IPAddress, string> resolutions = ipAddresses.ToDictionary(ip => ip, ip => string.Empty);
            List<Task> tasks = new List<Task>();
            foreach (IPAddress ip in ipAddresses)
                tasks.Add(Task.Run(async () =>
                {
                    resolutions[ip] = await GetHostnameAsync(ip, stripDnsSuffix);
                }));

            Task.WaitAll(tasks.ToArray());

            return resolutions;
        }

        public static async Task<string> GetHostnameAsync(IPAddress address, string stripDnsSuffix = "")
        {
            string hostname = string.Empty;

            try
            {
                IPHostEntry host = await Dns.GetHostEntryAsync(address);
                hostname = host.HostName;

                if (!string.IsNullOrWhiteSpace(stripDnsSuffix))
                    hostname = hostname.Replace('.' + stripDnsSuffix, "");
            }
            catch (SocketException)
            {
                Debug.WriteLine($"Cannot find hostname of {address}.");
            }

            return hostname;
        }
    }
}