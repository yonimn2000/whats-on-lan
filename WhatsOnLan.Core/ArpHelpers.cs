using SharpPcap;
using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WhatsOnLan.Core
{
    public static class ArpHelpers
    {
        public static void Resolve(IPAddress address)
        {
            ARP arper = new ARP(LibPcapLiveDeviceList.Instance.First(d => d.Description.Contains("Wireless-AC")));

            PhysicalAddress mac = arper.Resolve(address);
            if (mac == null)
            {
                Console.WriteLine(address + " timed out");
            }
            else
            {
                Console.WriteLine(address + " is at: " + mac);
            }
        }

        public static void Resolve()
        {
            // Print SharpPcap version
            var ver = Pcap.SharpPcapVersion;
            Console.WriteLine("SharpPcap {0}, Example2.ArpResolve.cs\n", ver);

            // Retrieve the device list
            var devices = LibPcapLiveDeviceList.Instance;

            // If no devices were found print an error
            if (devices.Count < 1)
            {
                Console.WriteLine("No devices were found on this machine");
                return;
            }

            Console.WriteLine("The following devices are available on this machine:");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();

            int i = 0;

            // Print out the available devices
            foreach (var dev in devices.Where(d => d.Addresses.Count > 0))
            {
                Console.WriteLine("{0}) {1} {2}", i, dev.Description,
                    string.Join(',', dev.Addresses
                    .Where(a => a.Addr.type == Sockaddr.AddressTypes.AF_INET_AF_INET6
                    && a.Addr.ipAddress.AddressFamily == AddressFamily.InterNetwork).Select(a => a.Addr)));
                i++;
            }

            Console.WriteLine();
            Console.Write("-- Please choose a device for sending the ARP request: ");
            i = int.Parse(Console.ReadLine());

            var device = devices[i];

            IPAddress ip;

            // loop until a valid ip address is parsed
            while (true)
            {
                Console.Write("-- Please enter IP address to be resolved by ARP: ");
                if (IPAddress.TryParse(Console.ReadLine(), out ip))
                    break;
                Console.WriteLine("Bad IP address format, please try again");
            }

            // Create a new ARP resolver
            ARP arper = new ARP(device);

            // print the resolved address or indicate that none was found
            var resolvedMacAddress = arper.Resolve(ip);
            if (resolvedMacAddress == null)
            {
                Console.WriteLine("Timeout, no mac address found for ip of " + ip);
            }
            else
            {
                Console.WriteLine(ip + " is at: " + resolvedMacAddress);
            }
        }
    }
}