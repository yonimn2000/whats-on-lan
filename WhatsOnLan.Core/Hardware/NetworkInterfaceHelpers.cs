using System.Net.NetworkInformation;

namespace WhatsOnLan.Core.Hardware
{
    public class NetworkInterfaceHelpers
    {
        public static IEnumerable<NetworkInterface> GetAllActiveInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                    && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        }

        public static IEnumerable<PcapNetworkInterface> GetAllPcapNetworkInterfaces()
        {
            return GetAllActiveInterfaces().Select(i => new PcapNetworkInterface(i));
        }
    }
}