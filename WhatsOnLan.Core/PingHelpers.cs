using System.Net;
using System.Net.NetworkInformation;

namespace WhatsOnLan.Core
{
    public static class PingHelpers
    {
        public static bool PingIpAddress(IPAddress address)
        {
            bool pingable = false;
            Ping pinger = new Ping();

            try
            {
                PingReply reply = pinger.Send(address);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                    pinger.Dispose();
            }

            return pingable;
        }
    }
}