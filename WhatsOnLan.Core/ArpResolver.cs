using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WhatsOnLan.Core
{
    public static class ArpResolver
    {
        public static PhysicalAddress GetMacAddress(IPAddress address, LibPcapLiveDevice device, int timeoutMillis = 1000)
        {
            return GetMacAddresses(new IPAddress[] { address }, device, timeoutMillis)[address];
        }

        public static IDictionary<IPAddress, PhysicalAddress> GetMacAddresses(
            IEnumerable<IPAddress> ipAddresses, LibPcapLiveDevice device, int timeoutMillis = 1000)
        {
            Dictionary<IPAddress, PhysicalAddress> resolutions = ipAddresses.ToDictionary(ip => ip, ip => PhysicalAddress.None);
            PcapInterface pcapInterface = device.Interface;
            IPAddress localIp = GetLocalIpAddress(pcapInterface);
            PhysicalAddress localMac = GetLocalMacAddress(pcapInterface);
            IEnumerable<Packet> requestPackets = ipAddresses.Select(ip => BuildRequest(ip, localMac, localIp));
            
            device.Open(mode: DeviceModes.Promiscuous, read_timeout: 20);

            // Create a "tcpdump" filter for allowing only arp replies to be read.
            device.Filter = "arp and ether dst " + localMac.ToString();

            foreach (Packet requestPacket in requestPackets)
                device.SendPacket(requestPacket);

            // Attempt to resolve the addresses with the current timeout.
            DateTime timeoutDateTime = DateTime.Now.AddMilliseconds(timeoutMillis);
            while (DateTime.Now < timeoutDateTime)
            {
                // Read the next packet from the network.
                if (device.GetNextPacket(out PacketCapture packetCapture) == GetPacketStatus.PacketRead)
                {
                    RawCapture reply = packetCapture.GetPacket();

                    // Parse and check if this is an arp packet.
                    ArpPacket arpPacket = Packet.ParsePacket(reply.LinkLayerType, reply.Data).Extract<ArpPacket>();
                    if (arpPacket != null)
                    {
                        // If this is the reply we are looking for, add the result to the dictionary.
                        if (resolutions.ContainsKey(arpPacket.SenderProtocolAddress))
                            resolutions[arpPacket.SenderProtocolAddress] = arpPacket.SenderHardwareAddress;
                    }
                }
            }

            device.Close();

            // Add the MAC of the current device to the dictionary if not there.
            if (resolutions.ContainsKey(localIp) && resolutions[localIp].Equals(PhysicalAddress.None))
                resolutions[localIp] = localMac;

            return resolutions;
        }

        private static PhysicalAddress GetLocalMacAddress(PcapInterface pcapInterface)
        {
            PhysicalAddress? localMAC = pcapInterface.Addresses
                .FirstOrDefault(address => address.Addr.type == Sockaddr.AddressTypes.HARDWARE)?.Addr.hardwareAddress;

            if (localMAC == null)
                throw new InvalidOperationException("Unable to find local mac address");

            return localMAC;
        }

        private static IPAddress GetLocalIpAddress(PcapInterface pcapInterface)
        {
            // Attempt to find an ipv4 address and make sure the address is IPv4.
            foreach (PcapAddress address in pcapInterface.Addresses)
                if (address.Addr.type == Sockaddr.AddressTypes.AF_INET_AF_INET6
                    && address.Addr.ipAddress.AddressFamily == AddressFamily.InterNetwork)
                        return address.Addr.ipAddress ?? IPAddress.Parse("127.0.0.1"); // Use localhost if no IPs.

            return IPAddress.None;
        }

        private static Packet BuildRequest(IPAddress destinationIP, PhysicalAddress localMac, IPAddress localIP)
        {
            // An arp packet goes inside an ethernet packet.
            EthernetPacket ethernetPacket 
                = new EthernetPacket(localMac, PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF"), EthernetType.Arp);
           
            ArpPacket arpPacket = new ArpPacket(ArpOperation.Request, PhysicalAddress.Parse("00-00-00-00-00-00"),
                destinationIP, localMac, localIP);

            // Set the arp packet as the payload of the ethernet packet.
            ethernetPacket.PayloadPacket = arpPacket;

            return ethernetPacket;
        }
    }
}