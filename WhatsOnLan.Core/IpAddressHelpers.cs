using System.Net;

namespace WhatsOnLan.Core
{
    public static class IpAddressHelpers
    {
        public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        public static IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
            }
            return new IPAddress(broadcastAddress);
        }

        public static bool IsOnSameNetwork(IPAddress address1, IPAddress address2, IPAddress subnet)
        {
            return GetNetworkAddress(address1, subnet).Equals(GetNetworkAddress(address2, subnet));
        }

        public static IEnumerable<IPAddress> GetAllHostAddresses(IPAddress address, IPAddress subnetMask)
        {
            IPAddress network = GetNetworkAddress(address, subnetMask);
            IPAddress broadcast = GetBroadcastAddress(address, subnetMask);

            for (int ip = IpAdressToInt(network) + 1; ip < IpAdressToInt(broadcast); ip++)
                yield return IntToIpAddress(ip);
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
    }
}