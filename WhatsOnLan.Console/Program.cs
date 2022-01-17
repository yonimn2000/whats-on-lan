using WhatsOnLan.Core;
using WhatsOnLan.Core.Hardware;
using WhatsOnLan.Core.OUI;

string path = @"C:\Users\Yonatan\Downloads\oui.csv";
//await OuiHelpers.DownloadOuiCsvFileAsync(path);

Console.WriteLine("Reading OUI file...");
IOuiMatcher ouiMatcher = new OuiMatcher(OuiHelpers.ReadOuiCsvFileLines(path));

Console.WriteLine("Getting all network interfaces...");
HashSet<PcapNetworkInterface> interfaces = NetworkInterfaceHelpers.GetAllDistinctPcapNetworkInterfaces().ToHashSet();

Console.WriteLine();
Console.WriteLine("Active network interfaces:");
foreach (PcapNetworkInterface iface in interfaces)
    Console.WriteLine(iface);
Console.WriteLine();

NetworkScanner networkScanner = new NetworkScanner(interfaces)
{
    OuiMatcher = ouiMatcher,
    SendPings = false,
    ResolveHostnames = true,
    StripDnsSuffix = true
};

Console.WriteLine("Scanning...");
IList<IpScanResult> results = networkScanner.Scan().Where(r => r.IsOnline).ToList();

string format = "{0,-15}| {1,-13}| {2,-20}| {3}";
Console.WriteLine();
Console.WriteLine(results.Count + " devices found:\n");
Console.WriteLine(string.Format(format, "IP", "MAC", "Hostname", "Manufacturer"));
Console.WriteLine(new string('-', format.Length + 15 + 13 + 20));
foreach (IpScanResult result in results)
    Console.WriteLine(string.Format(format, result.IpAddress, result.MacAddress, result.Hostname, result.Manufacturer));