using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace WhatsOnLan.Core
{
    public static class HostnameHelpers
    {
        public static string GetHostname(IPAddress address)
        {
            string hostname = string.Empty;

            try
            {
                hostname = Dns.GetHostEntry(address).HostName;
            }
            catch (SocketException)
            {
                Debug.WriteLine($"Cannot find hostname of {address}.");
            }

            return hostname;
        }
    }
}