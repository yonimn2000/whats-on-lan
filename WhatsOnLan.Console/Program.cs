using YonatanMankovich.WhatsOnLan.Core;
using YonatanMankovich.WhatsOnLan.Core.Hardware;
using YonatanMankovich.WhatsOnLan.Core.Helpers;
using YonatanMankovich.WhatsOnLan.Core.OUI;

// The path were to save and load the OUI CSV file from.
string ouiCsvFilePath = "OUI.csv";

// If the file already exists, do not download it again.
if (!File.Exists(ouiCsvFilePath))
{
    Console.WriteLine("Downloading OUI CSV file...");
    await new OuiCsvDownloader().DownloadOuiCsvFileAsync(ouiCsvFilePath);
}

// Read the OUI CSV file into the matcher for matching later.
Console.WriteLine("Reading OUI file...");
IOuiMatcher ouiMatcher = new OuiMatcher(OuiCsvFileHelpers.ReadOuiCsvFileLines(ouiCsvFilePath));

// Get all active network interfaces.
Console.WriteLine("Getting all network interfaces...\n");
List<PcapNetworkInterface> networkInterfaces = NetworkInterfaceHelpers.GetAllDistinctPcapNetworkInterfaces().ToList();
List<PcapNetworkInterface> selectedInterfaces = new List<PcapNetworkInterface>();

if (networkInterfaces.Count == 1) // If only one interface, scan it.
{
    Console.WriteLine("Active network interface: " + networkInterfaces.First());
    selectedInterfaces = networkInterfaces;
}
else if (networkInterfaces.Count == 0) // If no interfaces, exit.
{
    Console.WriteLine("No active network interfaces found...");
    return;
}
else // If more than one interface, let the user select which ones to scan.
{
    Console.WriteLine("Select interface(s) to scan by number (comma-separated). Press Enter for all:");

    for (int i = 0; i < networkInterfaces.Count; i++)
        Console.WriteLine($" {i + 1}: " + networkInterfaces[i]);

    string? input = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(input))
        selectedInterfaces = networkInterfaces;
    else
    {
        string[] indices = input.Split(',');

        foreach (string index in indices)
        {
            if (int.TryParse(index.Trim(), out int j) && j > 0 && j <= networkInterfaces.Count)
                selectedInterfaces.Add(networkInterfaces[j - 1]);
            else
                Console.WriteLine($"Skipping invalid input: '{index}'.");
        }
    }
}

do
{
    // Run the scanner for every active interface separately.
    foreach (PcapNetworkInterface networkInterface in selectedInterfaces)
    {
        // Create a network scanner.
        NetworkScanner networkScanner = new NetworkScanner(networkInterface)
        {
            // Set the options.
            Options = new NetworkScannerOptions
            {
                OuiMatcher = ouiMatcher,
                SendPings = true,
                SendArpRequest = true,
                ResolveHostnames = true,
                StripDnsSuffix = true,
                ShuffleIpAddresses = true,
                Repeats = 5,
                ArpTimeout = TimeSpan.FromSeconds(1),
                HostnameResolverTimeout = TimeSpan.FromSeconds(3),
                PingerTimeout = TimeSpan.FromMilliseconds(250),
            }
        };

        Console.WriteLine($"\nScanning {networkInterface.NumberOfScannableHosts} hosts " +
            $"on the '{networkInterface.Name}' interface...");

        // Perform the scan and get only the results that have the device online.
        IList<IpScanResult> results = networkScanner.ScanNetwork()
            .Where(r => r.IsOnline).OrderBy(r => r.IpAddress.ToSortableString()).ToList();

        /**************** Write the results in a neat table. *******************/

        // First number is the number of the params later. Minus is left align. Last number is the column width.
        string format = " {0,-16}| {1,-18}| {2,-5}| {3,-25}| {4}";
        Console.WriteLine($"\n{results.Count} devices found:\n");
        Console.WriteLine(string.Format(format, "IP", "MAC", "Ping", "Hostname", "Manufacturer")); // Headers
        Console.WriteLine(new string('-', format.Length + 16 + 18 + 25)); // Draw a line --------- under the headers.

        // Write the results themselves.
        foreach (IpScanResult result in results)
            Console.WriteLine(string.Format(format, result.IpAddress.ToSortableString(), result.MacAddress.ToColonString(),
                result.RespondedToPing ? "Yes" : "No", result.Hostname, result.Manufacturer));
    }

    Console.WriteLine(Environment.NewLine + "Press ENTER to scan again. Press Q and ENTER to exit.");
    string? read = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(read) && read.ToUpper().Contains('Q'))
        break;
} while (true);