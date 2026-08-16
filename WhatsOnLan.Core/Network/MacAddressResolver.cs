using PacketDotNet;
using SharpPcap;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using YonatanMankovich.WhatsOnLan.Core.Hardware;

namespace YonatanMankovich.WhatsOnLan.Core.Network
{
    /// <summary>
    /// Provides methods for mapping IP addresses to MAC addresses using ARP.
    /// </summary>
    public class MacAddressResolver
    {
        /// <summary>
        /// Gets or sets the timeout of waiting for ARP responses from network hosts.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the number of times to try resolving host MAC addresses consecutively.
        /// </summary>
        public int Retries { get; set; } = 1;

        private PcapNetworkInterface NetworkInterface { get; }

        private static readonly PhysicalAddress BroadcastMacAddress = PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF");
        private static readonly PhysicalAddress AllZeroMacAddress = PhysicalAddress.Parse("00-00-00-00-00-00");

        /// <summary>
        /// Initializes an instance of the <see cref="MacAddressResolver"/> class 
        /// to be used using the provided <see cref="PcapNetworkInterface"/> instance.
        /// </summary>
        /// <param name="networkInterface">A <see cref="PcapNetworkInterface"/> to initialize the instance to.</param>
        public MacAddressResolver(PcapNetworkInterface networkInterface)
        {
            NetworkInterface = networkInterface;
        }

        /// <summary>
        /// Maps an <see cref="IPAddress"/> to a <see cref="PhysicalAddress"/> by sending an ARP request and waiting for a response.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/> to map to a <see cref="PhysicalAddress"/>.</param>
        /// <returns>The mapped <see cref="PhysicalAddress"/>.</returns>
        public PhysicalAddress ResolveMacAddress(IPAddress ipAddress)
        {
            return ResolveMacAddresses([ipAddress])[ipAddress];
        }

        /// <summary>
        /// Maps many <see cref="IPAddress"/>es to many <see cref="PhysicalAddress"/>es by sending ARP requests and waiting for responses.
        /// </summary>
        /// <param name="ipAddresses">
        /// The <see cref="IEnumerable{T}"/> that contains all the <see cref="IPAddress"/>es
        /// to map to <see cref="PhysicalAddress"/>es.
        /// </param>
        /// <returns>The mapped <see cref="IPAddress"/>es as an <see cref="IDictionary{TKey, TValue}"/>.</returns>
        public IDictionary<IPAddress, PhysicalAddress> ResolveMacAddresses(IEnumerable<IPAddress> ipAddresses)
        {
            ArgumentNullException.ThrowIfNull(ipAddresses);
            IPAddress[] addresses = ipAddresses.Distinct().ToArray();
            Dictionary<IPAddress, PhysicalAddress> resolutions = addresses.ToDictionary(ip => ip, _ => PhysicalAddress.None);

            // Add the MAC of the current device to the dictionary.
            if (resolutions.ContainsKey(NetworkInterface.IpAddress))
                resolutions[NetworkInterface.IpAddress] = NetworkInterface.MacAddress;

            bool isOpen = false;
            try
            {
                // Start listening on the device.
                NetworkInterface.Device.Open(new DeviceConfiguration
                {
                    Mode = DeviceModes.Promiscuous,
                    ReadTimeout = 20,
                    BufferSize = 4 * 1024 * 1024
                });
                isOpen = true;

                // Allow only ARP replies addressed to this adapter.
                NetworkInterface.Device.Filter = "arp and arp[6:2] = 2 and ether dst " + NetworkInterface.MacAddress.ToString();

                int tries = 0;
                int retries = Math.Max(1, Retries);
                do
                {
                    MapIpAddressesToMacAddresses(resolutions);
                    tries++;
                } while (tries < retries && resolutions.Any(r => r.Value.Equals(PhysicalAddress.None)));

                return resolutions;
            }
            finally
            {
                if (isOpen)
                    NetworkInterface.Device.Close();
            }
        }

        private void MapIpAddressesToMacAddresses(IDictionary<IPAddress, PhysicalAddress> resolutions)
        {
            IReadOnlyCollection<IPAddress> unresolvedIpAddresses
                = resolutions.Where(r => r.Value == PhysicalAddress.None).Select(kvp => kvp.Key).ToList();

            if (unresolvedIpAddresses.Count == 0)
                return;

            foreach (Packet requestPacket in unresolvedIpAddresses.Select(BuildArpRequestPacket))
                NetworkInterface.Device.SendPacket(requestPacket);

            int numberOfipAddressesToResolve = unresolvedIpAddresses.Count;

            // Attempt to resolve the addresses with the current timeout.
            if (Timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(Timeout), "The timeout must be greater than zero.");

            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < Timeout)
            {
                // Read the next packet from the network.
                if (NetworkInterface.Device.GetNextPacket(out PacketCapture packetCapture) == GetPacketStatus.PacketRead)
                {
                    RawCapture reply = packetCapture.GetPacket();

                    // Accept only a reply to an ARP request made by this adapter.
                    ArpPacket arpPacket = Packet.ParsePacket(reply.LinkLayerType, reply.Data).Extract<ArpPacket>();
                    if (arpPacket?.Operation == ArpOperation.Response
                        && arpPacket.TargetProtocolAddress.Equals(NetworkInterface.IpAddress)
                        && arpPacket.TargetHardwareAddress.Equals(NetworkInterface.MacAddress))
                    {
                        // If this is the reply we are looking for, add the result to the dictionary.
                        if (resolutions.TryGetValue(arpPacket.SenderProtocolAddress, out PhysicalAddress? currentMac)
                            && currentMac.Equals(PhysicalAddress.None))
                        {
                            resolutions[arpPacket.SenderProtocolAddress] = arpPacket.SenderHardwareAddress;
                            numberOfipAddressesToResolve--;
                            if (numberOfipAddressesToResolve == 0) // If all hosts responeded, stop waiting.
                                break;
                        }
                    }
                }
            }
        }

        private Packet BuildArpRequestPacket(IPAddress destinationIP)
        {
            return new EthernetPacket(NetworkInterface.MacAddress, BroadcastMacAddress, EthernetType.Arp)
            {
                PayloadPacket = new ArpPacket(ArpOperation.Request, AllZeroMacAddress, destinationIP,
                    NetworkInterface.MacAddress, NetworkInterface.IpAddress)
            };
        }
    }
}
