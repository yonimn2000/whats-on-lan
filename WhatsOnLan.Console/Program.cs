using WhatsOnLan.Core;
using WhatsOnLan.Core.OUI;

string path = @"C:\Users\Yonatan\Downloads\oui.csv";
//await OuiHelpers.DownloadOuiCsvFileAsync(path);

Console.WriteLine("Reading OUI file...");
IOuiMatcher ouiMatcher = new OuiMatcher(OuiHelpers.ReadOuiCsvFileLines(path));

Console.WriteLine("Setting up scanner...");
NetworkScanner networkScanner = new NetworkScanner()
{
    SendPings = false,
    ResolveHostnames = true,
    OuiMatcher = ouiMatcher
};

Console.WriteLine("Scanning...");
IList<IpScanResult> results = networkScanner.Scan().Where(r => r.IsOnline).ToList();

Console.WriteLine();
Console.WriteLine(results.Count + " devices found:");
foreach (IpScanResult result in results)
    Console.WriteLine(result);