using System.Net.NetworkInformation;

namespace WhatsOnLan.Core.Hardware
{
    /// <summary>
    /// Provides methods for getting the network interfaces of the current machine.
    /// </summary>
    public class NetworkInterfaceHelpers
    {
        /// <summary>
        /// Gets all distinct active non-loopback network interfaces as an 
        /// <see cref="IEnumerable{T}"/> of <see cref="PcapNetworkInterface"/> objects
        /// distinguished by their network addresses.
        /// </summary>
        public static IEnumerable<PcapNetworkInterface> GetAllDistinctPcapNetworkInterfaces()
        {
            return GetAllPcapNetworkInterfaces().DistinctBy(i => i.Network);
        }

        /// <summary>
        /// Gets all active non-loopback network interfaces as an 
        /// <see cref="IEnumerable{T}"/> of <see cref="PcapNetworkInterface"/> objects.
        /// </summary>
        public static IEnumerable<PcapNetworkInterface> GetAllPcapNetworkInterfaces()
        {
            return GetAllActiveInterfaces().Select(i => new PcapNetworkInterface(i));
        }

        /// <summary>
        /// Gets all active non-loopback network interfaces.
        /// </summary>
        private static IEnumerable<NetworkInterface> GetAllActiveInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                    && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        }
    }
}