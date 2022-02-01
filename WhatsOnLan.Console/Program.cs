using YonatanMankovich.WhatsOnLan.Core;
using YonatanMankovich.WhatsOnLan.Core.Hardware;
using YonatanMankovich.WhatsOnLan.Core.OUI;

string ouiCsvFilePath = "OUI.csv";
if (!File.Exists(ouiCsvFilePath))
{
    Console.WriteLine("Downloading OUI CSV file...");
    await OuiCsvFileHelpers.DownloadOuiCsvFileAsync(ouiCsvFilePath);
}

Console.WriteLine("Reading OUI file...");
IOuiMatcher ouiMatcher = new OuiMatcher(OuiCsvFileHelpers.ReadOuiCsvFileLines(ouiCsvFilePath));

Console.WriteLine("Getting all network interfaces...");
HashSet<PcapNetworkInterface> networkInterfaces = NetworkInterfaceHelpers.GetAllDistinctPcapNetworkInterfaces().ToHashSet();

Console.WriteLine("\nActive network interfaces:");
foreach (PcapNetworkInterface networkInterface in networkInterfaces)
    Console.WriteLine("  - " + networkInterface);

foreach (PcapNetworkInterface networkInterface in networkInterfaces)
{
    NetworkScanner networkScanner = new NetworkScanner(networkInterface)
    {
        Options = new NetworkScannerOptions
        {
            OuiMatcher = ouiMatcher,
            SendPings = true,
            SendArpRequest = true,
            ResolveHostnames = true,
            StripDnsSuffix = true
        }
    };

    Console.WriteLine($"\nScanning {networkInterface.NumberOfScannableHosts} hosts on the '{networkInterface.Name}' interface...");
    IList<IpScanResult> results = networkScanner.ScanNetwork().Where(r => r.IsOnline).ToList();

    // First number is the number of the params later. Minus is left allign. Last number is the column width.
    string format = " {0,-15}| {1,-13}| {2,-5}| {3,-20}| {4}";
    Console.WriteLine($"\n{results.Count} devices found:\n");
    Console.WriteLine(string.Format(format, "IP", "MAC", "Ping", "Hostname", "Manufacturer")); // Headers
    Console.WriteLine(new string('-', format.Length + 15 + 13 + 20)); // Draw a line --------- under the headers.

    foreach (IpScanResult result in results)
        Console.WriteLine(string.Format(format, result.IpAddress, result.MacAddress,
            result.RespondedToPing ? "Yes" : "No", result.Hostname, result.Manufacturer));
}