using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using WhatsOnLan.Core;
using WhatsOnLan.Core.OUI;

string path = @"C:\Users\Yonatan\Downloads\oui.csv";
//await OuiHelpers.DownloadOuiCsvFileAsync(path);
Console.WriteLine("Reading OUI file...");
OuiMatcher ouiMatcher = new OuiMatcher(OuiHelpers.ReadOuiCsvFileLines(path));

// Print out the available devices
foreach (var dev in LibPcapLiveDeviceList.Instance.Where(d => d.Addresses.Count > 0))
{
    Console.WriteLine(dev.Description + " " + string.Join(',', dev.Addresses
        .Where(a => a.Addr.type == Sockaddr.AddressTypes.AF_INET_AF_INET6
        && a.Addr.ipAddress.AddressFamily == AddressFamily.InterNetwork).Select(a => a.Addr)));
}

LibPcapLiveDevice device = LibPcapLiveDeviceList.Instance.First(d => d.Description.Contains("Wireless-AC"));
IEnumerable<IPAddress> ipAddresses = IpAddressHelpers.GetAllHostAddresses(IPAddress.Parse("192.168.1.60"), IPAddress.Parse("255.255.255.0"));

Console.WriteLine("Scanning " + ipAddresses.Count() + " devices...");
foreach (IpScanResult result in NetworkScanner.ScanIpAddresses(ipAddresses, device, ouiMatcher).Where(r => r.IsOnline))
    Console.WriteLine(result);

/*while (true)
{
    Console.WriteLine("Getting macs...");
    IDictionary<IPAddress, PhysicalAddress> macs = ArpResolver.GetMacAddresses(ipAddresses, device);
    Console.WriteLine("Getting pings...");
    IDictionary<IPAddress, bool> pings = Pinger.Ping(macs.Where(m => m.Value.Equals(PhysicalAddress.None)).Select(m => m.Key));
    Console.WriteLine("Getting hostnames...");
    IEnumerable<IPAddress> respondingHosts = macs.Where(m => !m.Value.Equals(PhysicalAddress.None)).Select(m => m.Key)
        .Union(pings.Where(p => p.Value).Select(p => p.Key));

    IDictionary<IPAddress, string> hostnames = HostnameResolver.GetHostnames(respondingHosts);

    foreach (IPAddress ip in ipAddresses)
    {
        bool doesResolve = !macs[ip].Equals(PhysicalAddress.None);
        pings.TryGetValue(ip, out bool doesPing);
        if (doesResolve || doesPing)
            Console.WriteLine(ip + "\t" + (doesResolve ? "R" : "-") + (doesPing ? "P" : "-") + "\t" + hostnames[ip]);
    }
    Console.WriteLine();
}*/