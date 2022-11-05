using System.Net;

namespace YonatanMankovich.WhatsOnLan.Core.Helpers
{
    /// <summary>
    /// Provides helper methods for working with <see cref="IPAddress"/>es.
    /// </summary>
    public static class IpAddressHelpers
    {
        /// <summary>
        /// Calculates the broadcast address of the given IP address and subnet mask.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        /// <param name="subnetMask">The subnet mask.</param>
        /// <returns>The broadcast address as an <see cref="IPAddress"/>.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IPAddress GetBroadcastAddress(IPAddress ipAddress, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = ipAddress.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));

            return new IPAddress(broadcastAddress);
        }

        /// <summary>
        /// Calculates the network address of the given IP address and subnet mask.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        /// <param name="subnetMask">The subnet mask.</param>
        /// <returns>The network address as an <see cref="IPAddress"/>.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IPAddress GetNetworkAddress(IPAddress ipAddress, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = ipAddress.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));

            return new IPAddress(broadcastAddress);
        }

        /// <summary>
        /// Checks if two <see cref="IPAddress"/>es are on the same network.
        /// </summary>
        /// <param name="ipAddress1">The first <see cref="IPAddress"/>.</param>
        /// <param name="ipAddress2">The second <see cref="IPAddress"/>.</param>
        /// <param name="networkAddress">The network address.</param>
        /// <returns><see langword="true"/> if the provided IP addresses are on the same network; <see langword="false"/> otherwise.</returns>
        public static bool IsOnSameNetwork(IPAddress ipAddress1, IPAddress ipAddress2, IPAddress networkAddress)
        {
            return GetNetworkAddress(ipAddress1, networkAddress).Equals(GetNetworkAddress(ipAddress2, networkAddress));
        }

        /// <summary>
        /// Calculates the <see cref="IPAddress"/>es of all the hosts on the given network.
        /// </summary>
        /// <param name="ipAddress">The network address or a network host IP address.</param>
        /// <param name="subnetMask">The subnet mask.</param>
        /// <returns>The <see cref="IPAddress"/>es of all the hosts on the given network</returns>
        public static IEnumerable<IPAddress> GetAllHostAddresses(IPAddress ipAddress, IPAddress subnetMask)
        {
            IPAddress network = GetNetworkAddress(ipAddress, subnetMask);
            IPAddress broadcast = GetBroadcastAddress(ipAddress, subnetMask);

            for (int ip = IpAdressToInt(network) + 1; ip < IpAdressToInt(broadcast); ip++)
                yield return IntToIpAddress(ip);
        }

        /// <summary>
        /// Calculates the number of all the hosts on the given network.
        /// </summary>
        /// <param name="ipAddress">The network address or a network host IP address.</param>
        /// <param name="subnetMask">The subnet mask.</param>
        /// <returns></returns>
        public static int GetNumberOfHostAddresses(IPAddress ipAddress, IPAddress subnetMask)
        {
            IPAddress network = GetNetworkAddress(ipAddress, subnetMask);
            IPAddress broadcast = GetBroadcastAddress(ipAddress, subnetMask);

            return IpAdressToInt(broadcast) - IpAdressToInt(network) - 1; // Exclude the broadcast address.
        }

        private static int IpAdressToInt(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToInt32(bytes, 0);
        }

        private static IPAddress IntToIpAddress(int address)
        {
            byte[] bytes = BitConverter.GetBytes(address);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return new IPAddress(bytes);
        }

        /// <summary>
        /// Creates a sortable string representation of the <see cref="IPAddress"/>
        /// where every octet is always three characters long (###.###.###.###).
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <returns>A sortable string representation of the <see cref="IPAddress"/>.</returns>
        public static string ToSortableString(this IPAddress address)
            => string.Join(".", address.GetAddressBytes().Select(b => b.ToString("D3")));
    }
}