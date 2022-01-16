using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using WhatsOnLan.Core.Hardware;

namespace WhatsOnLan.Core.Network
{
    public static class ArpResolver
    {
        public static PhysicalAddress GetMacAddress(IPAddress address, PcapNetworkInterface networkInterface, TimeSpan timeout)
        {
            return GetMacAddresses(new IPAddress[] { address }, networkInterface, timeout)[address];
        }

        public static IDictionary<IPAddress, PhysicalAddress> GetMacAddresses(
            IEnumerable<IPAddress> ipAddresses, PcapNetworkInterface networkInterface, TimeSpan timeout)
        {
            Dictionary<IPAddress, PhysicalAddress> resolutions = ipAddresses.ToDictionary(ip => ip, ip => PhysicalAddress.None);
            LibPcapLiveDevice device = networkInterface.Device;
            PcapInterface pcapInterface = device.Interface;
            IPAddress localIp = networkInterface.IpAddress;
            PhysicalAddress localMac = networkInterface.MacAddress;
            IEnumerable<Packet> requestPackets = ipAddresses.Select(ip => BuildRequest(ip, localMac, localIp));
            
            device.Open(mode: DeviceModes.Promiscuous, read_timeout: 20);

            // Create a "tcpdump" filter for allowing only arp replies to be read.
            device.Filter = "arp and ether dst " + localMac.ToString();

            foreach (Packet requestPacket in requestPackets)
                device.SendPacket(requestPacket);

            // Attempt to resolve the addresses with the current timeout.
            DateTime timeoutDateTime = DateTime.Now + timeout;
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