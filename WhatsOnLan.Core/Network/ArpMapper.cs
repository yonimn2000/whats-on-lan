using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using WhatsOnLan.Core.Hardware;

namespace WhatsOnLan.Core.Network
{
    /// <summary>
    /// Provides methods for mapping IP addresses to MAC addresses using ARP.
    /// </summary>
    public class ArpMapper
    {
        /// <summary>
        /// Gets or sets the timeout of waiting for ARP responses from network hosts.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

        private PcapNetworkInterface NetworkInterface { get; }

        private static readonly PhysicalAddress BroadcastMacAddress = PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF");
        private static readonly PhysicalAddress AllZeroMacAddress = PhysicalAddress.Parse("00-00-00-00-00-00");

        /// <summary>
        /// Initializes an instance of the <see cref="ArpMapper"/> class 
        /// to be used using the provided <see cref="PcapNetworkInterface"/> instance.
        /// </summary>
        /// <param name="networkInterface">A <see cref="PcapNetworkInterface"/> to initialize the instance to.</param>
        public ArpMapper(PcapNetworkInterface networkInterface)
        {
            NetworkInterface = networkInterface;
        }

        /// <summary>
        /// Maps an <see cref="IPAddress"/> to a <see cref="PhysicalAddress"/> by sending an ARP request and waiting for a response.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/> to map to a <see cref="PhysicalAddress"/>.</param>
        /// <returns>The mapped <see cref="PhysicalAddress"/>.</returns>
        public PhysicalAddress MapIpAddressToMacAddress(IPAddress ipAddress)
        {
            return MapIpAddressesToMacAddresses(new IPAddress[] { ipAddress })[ipAddress];
        }

        /// <summary>
        /// Maps many <see cref="IPAddress"/>es to many <see cref="PhysicalAddress"/>es by sending ARP requests and waiting for responses.
        /// </summary>
        /// <param name="ipAddresses">
        /// The <see cref="IEnumerable{T}"/> that contains all the <see cref="IPAddress"/>es
        /// to map to <see cref="PhysicalAddress"/>es.
        /// </param>
        /// <returns>The mapped <see cref="IPAddress"/>es as an <see cref="IDictionary{TKey, TValue}"/>.</returns>
        public IDictionary<IPAddress, PhysicalAddress> MapIpAddressesToMacAddresses(IEnumerable<IPAddress> ipAddresses)
        {
            LibPcapLiveDevice device = NetworkInterface.Device;
            IPAddress localIp = NetworkInterface.IpAddress;
            PhysicalAddress localMac = NetworkInterface.MacAddress;

            device.Open(mode: DeviceModes.Promiscuous, read_timeout: 20);

            // Create a "tcpdump" filter for allowing only arp replies to be read.
            device.Filter = "arp and ether dst " + localMac.ToString();

            foreach (Packet requestPacket in ipAddresses.Select(ip => BuildArpRequestPacket(ip, localMac, localIp)))
                device.SendPacket(requestPacket);

            Dictionary<IPAddress, PhysicalAddress> resolutions = ipAddresses.ToDictionary(ip => ip, ip => PhysicalAddress.None);
            int numberOfipAddressesToResolve = resolutions.Count;

            // Attempt to resolve the addresses with the current timeout.
            DateTime timeoutDateTime = DateTime.Now + Timeout;
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
                        {
                            resolutions[arpPacket.SenderProtocolAddress] = arpPacket.SenderHardwareAddress;
                            numberOfipAddressesToResolve--;
                        }
                        if (numberOfipAddressesToResolve == 0) // If all hosts responeded, stop waiting.
                            break;
                    }
                }
            }

            device.Close();

            // Add the MAC of the current device to the dictionary if not there.
            if (resolutions.ContainsKey(localIp) && resolutions[localIp].Equals(PhysicalAddress.None))
                resolutions[localIp] = localMac;

            return resolutions;
        }

        private static Packet BuildArpRequestPacket(IPAddress destinationIP, PhysicalAddress localMac, IPAddress localIP)
        {
            return new EthernetPacket(localMac, BroadcastMacAddress, EthernetType.Arp)
            {
                PayloadPacket = new ArpPacket(ArpOperation.Request, AllZeroMacAddress, destinationIP, localMac, localIP)
            };
        }
    }
}