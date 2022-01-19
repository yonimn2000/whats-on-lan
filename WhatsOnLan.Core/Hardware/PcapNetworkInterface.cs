using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WhatsOnLan.Core.Hardware
{
    /// <summary>
    /// Provides a network interface with its main properties.
    /// </summary>
    public class PcapNetworkInterface
    {
        /// <summary>
        /// Gets the name of the network interface.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the DnsSuffix of the interface.
        /// </summary>
        public string DnsSuffix { get; }

        /// <summary>
        /// Gets the MAC address of the interface.
        /// </summary>
        public PhysicalAddress MacAddress { get; }

        /// <summary>
        /// Gets the IPv4 address of the interface.
        /// </summary>
        public IPAddress IpAddress { get; }

        /// <summary>
        /// Gets the subnet mask of the interface.
        /// </summary>
        public IPAddress SubnetMask { get; }

        /// <summary>
        /// Gets the network address of the interface.
        /// </summary>
        public IPAddress Network { get; }

        /// <summary>
        /// Gets the boradcast address of the interface.
        /// </summary>
        public IPAddress Broadcast { get; }

        /// <summary>
        /// Gets the number of scannable hosts on the interface.
        /// </summary>
        public int NumberOfScannableHosts => IpAddressHelpers.GetNumberOfHostAddresses(IpAddress, SubnetMask);

        /// <summary>
        /// Gets the <see cref="LibPcapLiveDevice"/> associated with the interface.
        /// </summary>
        internal LibPcapLiveDevice Device { get; }

        /// <summary>
        /// Initializes an instance of the <see cref="PcapNetworkInterface"/> class 
        /// to the properties of the provided <see cref="NetworkInterface"/> instance.
        /// </summary>
        /// <param name="networkInterface">A <see cref="NetworkInterface"/> to initialize the instance to.</param>
        internal PcapNetworkInterface(NetworkInterface networkInterface)
        {
            IPInterfaceProperties ipProps = networkInterface.GetIPProperties();
            UnicastIPAddressInformation ipInfo = ipProps.UnicastAddresses
                            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork).First();

            Name = networkInterface.Description;
            MacAddress = networkInterface.GetPhysicalAddress();
            DnsSuffix = ipProps.DnsSuffix;
            IpAddress = ipInfo.Address;
            SubnetMask = ipInfo.IPv4Mask;
            Network = IpAddressHelpers.GetNetworkAddress(IpAddress, SubnetMask);
            Broadcast = IpAddressHelpers.GetBroadcastAddress(IpAddress, SubnetMask);
            Device = LibPcapLiveDeviceList.Instance.Where(d => d.Name.Contains(networkInterface.Id)).First();
        }

        /// <summary>
        /// Returns all the host IP addresses of the network the current interface is a part of.
        /// </summary>
        public IEnumerable<IPAddress> GetAllNetworkHostIpAddresses()
        {
            return IpAddressHelpers.GetAllHostAddresses(IpAddress, SubnetMask);
        }

        public override string ToString()
        {
            return $"{Name} {MacAddress} {IpAddress}";
        }
    }
}