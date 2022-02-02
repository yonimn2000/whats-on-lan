using System.Collections;
using System.Net;
using YonatanMankovich.WhatsOnLan.Core.Exceptions;
using YonatanMankovich.WhatsOnLan.Core.Hardware;

namespace YonatanMankovich.WhatsOnLan.Core
{
    /// <summary>
    /// Provides methods for working with a collection of <see cref="INetworkScanner"/> objects.
    /// </summary>
    public class NetworkScanners : IEnumerable<INetworkScanner>, INetworkScanner
    {
        /// <summary>
        /// A set of <see cref="INetworkScanner"/> objects.
        /// </summary>
        public ISet<INetworkScanner> Scanners { get; set; } = new HashSet<INetworkScanner>();

        /// <summary>
        /// The network scanners options.
        /// </summary>
        public NetworkScannerOptions Options { get; set; } = new NetworkScannerOptions();

        /// <summary>
        /// Initilizes <see cref="NetworkScanners"/> with all network interfaces available on the current machine.
        /// </summary>
        public void InitializeWithAllActiveInterfaces()
        {
            foreach (PcapNetworkInterface iface in NetworkInterfaceHelpers.GetAllDistinctPcapNetworkInterfaces())
                Scanners.Add(new NetworkScanner(iface)
                {
                    Options = Options
                });
        }

        public Task<IpScanResult> ScanIpAddressAsync(IPAddress ipAddress)
        {
            INetworkScanner? scanner = Scanners.FirstOrDefault(s => s.IsIpAddressOnCurrentNetwork(ipAddress));
            
            if (scanner == null)
                throw new IpAddressNotOnNetworkException(ipAddress);

            return scanner.ScanIpAddressAsync(ipAddress);
        }

        public bool IsIpAddressOnCurrentNetwork(IPAddress ipAddress)
        {
            foreach (INetworkScanner scanner in Scanners)
                if (scanner.IsIpAddressOnCurrentNetwork(ipAddress))
                    return true;

            return false;
        }

        public Task<IEnumerable<IpScanResult>> ScanNetworkAsync() => Task.Run(ScanNetwork);

        public IEnumerable<IpScanResult> ScanNetwork() => Scanners.SelectMany(s => s.ScanNetwork());

        public IEnumerator<INetworkScanner> GetEnumerator() => Scanners.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Scanners.GetEnumerator();
        }
    }
}