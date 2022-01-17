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
        public string Name { get; }
        public string DnsSuffix { get; set; }
        public PhysicalAddress MacAddress { get; }
        public IPAddress IpAddress { get; }
        public IPAddress Subnet { get; }
        public IPAddress Network { get; }
        public IPAddress Broadcast { get; }
        public LibPcapLiveDevice Device { get; }
        public NetworkInterface Interface { get; }

        public PcapNetworkInterface(NetworkInterface networkInterface)
        {
            IPInterfaceProperties ipProps = networkInterface.GetIPProperties();
            UnicastIPAddressInformation unicastIPAddressInformation = ipProps.UnicastAddresses
                            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork).First();

            Interface = networkInterface;
            Name = networkInterface.Description;
            MacAddress = networkInterface.GetPhysicalAddress();
            DnsSuffix = ipProps.DnsSuffix;
            IpAddress = unicastIPAddressInformation.Address;
            Subnet = unicastIPAddressInformation.IPv4Mask;
            Network = IpAddressHelpers.GetNetworkAddress(IpAddress, Subnet);
            Broadcast = IpAddressHelpers.GetBroadcastAddress(IpAddress, Subnet);
            Device = LibPcapLiveDeviceList.Instance.Where(d => d.Name.Contains(networkInterface.Id)).First();
        }

        public IEnumerable<IPAddress> GetAllReachableIpAddresses()
        {
            return IpAddressHelpers.GetAllHostAddresses(IpAddress, Subnet);
        }

        public override string ToString()
        {
            return $"{Name} {MacAddress} {IpAddress}";
        }
    }
}