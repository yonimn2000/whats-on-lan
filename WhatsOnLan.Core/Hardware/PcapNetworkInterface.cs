using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WhatsOnLan.Core.Hardware
{
    public class PcapNetworkInterface
    {
        public string Name { get; }
        public PhysicalAddress MacAddress { get; }
        public IPAddress IpAddress { get; }
        public IPAddress Subnet { get; }
        public IPAddress Network { get; }
        public IPAddress Broadcast { get; }
        public LibPcapLiveDevice Device { get; }
        public NetworkInterface Interface { get; }

        public PcapNetworkInterface(NetworkInterface networkInterface)
        {
            Interface = networkInterface;
            Name = networkInterface.Name;
            MacAddress = networkInterface.GetPhysicalAddress();
            UnicastIPAddressInformation unicastIPAddressInformation = networkInterface.GetIPProperties().UnicastAddresses
                            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork).First();
            IpAddress = unicastIPAddressInformation.Address;
            Device = LibPcapLiveDeviceList.Instance.Where(d => d.Name.Contains(networkInterface.Id)).First();
            Subnet = unicastIPAddressInformation.IPv4Mask;
            Network = IpAddressHelpers.GetNetworkAddress(IpAddress, Subnet);
            Broadcast = IpAddressHelpers.GetBroadcastAddress(IpAddress, Subnet);
        }

        public IEnumerable<IPAddress> GetAllReachableIpAddresses()
        {
            return IpAddressHelpers.GetAllHostAddresses(IpAddress, Subnet);
        }
    }
}