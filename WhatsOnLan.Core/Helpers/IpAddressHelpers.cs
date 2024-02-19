using System.Net;
using System.Net.Sockets;

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
        /// Converts the subnet mask represented by this IPAddress into its corresponding slash notation (CIDR notation).
        /// </summary>
        /// <param name="subnetMask">The subnet mask IPAddress to convert.</param>
        /// <returns>The slash notation representing the subnet mask.</returns>
        /// <exception cref="ArgumentException">Thrown when the IPAddress is not an IPv4 address.</exception>
        public static int ToSlashNotation(this IPAddress subnetMask)
        {
            byte[] bytes = subnetMask.GetAddressBytes();

            if (bytes.Length != 4 || subnetMask.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException($"Invalid IPv4 address: {subnetMask}", nameof(subnetMask));

            if (!subnetMask.IsValidSubnetMask())
                throw new ArgumentException($"Not a valid subnet mask: {subnetMask}", nameof(subnetMask));

            uint mask = BitConverter.ToUInt32(bytes, 0);
            int slashCount = 0;

            while ((mask & 1) == 1)
            {
                slashCount++;
                mask >>= 1;
            }

            return slashCount;
        }

        /// <summary> Checks whether the IPAddress represents a valid subnet mask </summary>
        /// <param name="subnetMask">The IPAddress to check.</param>
        /// <returns>True if the IPAddress is a valid subnet mask; otherwise, false.</returns>
        /// <remarks>A valid subnet mask consists of consecutive 1 bits followed by consecutive 0 bits.</remarks>
        public static bool IsValidSubnetMask(this IPAddress subnetMask)
        {
            byte[] bytes = subnetMask.GetAddressBytes();

            if (bytes.Length != 4 || subnetMask.AddressFamily != AddressFamily.InterNetwork)
                return false;

            // A valid mask should start with ones, and end with zeros 0b111...111000...000
            // 2) XOR to flip all the bits (0b000...000111...111)
            // 3) Add 1, causing all those 1's to become 0's. (0b000...001000...000)
            // An invalid address will have leading 1's that are untouched by this step. (0b101...001000...000)
            // 4) AND the two values together, to detect if any leading 1's remain

            uint val = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes));
            uint invertedVal = val ^ uint.MaxValue;

            return (invertedVal + 1 & invertedVal) == 0;
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