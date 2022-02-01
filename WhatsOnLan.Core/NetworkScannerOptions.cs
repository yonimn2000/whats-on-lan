using YonatanMankovich.WhatsOnLan.Core.OUI;

namespace YonatanMankovich.WhatsOnLan.Core
{
    /// <summary>
    /// Represents the options of a <see cref="NetworkScanner"/>.
    /// </summary>
    public class NetworkScannerOptions
    {
        /// <summary>
        /// Indicates whether to send pings to hosts during the scan.
        /// </summary>
        public bool SendPings { get; set; } = true;

        /// <summary>
        /// Indicates whether to send ARP requests to hosts during the scan.
        /// </summary>
        public bool SendArpRequest { get; set; } = true;

        /// <summary>
        /// Indicates whether to resolve hostnames during the scan.
        /// </summary>
        public bool ResolveHostnames { get; set; } = true;

        /// <summary>
        /// Indicates whether to strip the the DNS suffix from the resolved hostname. For example, "host.domain.local"
        /// will become "host" for a given suffix of "domain.local".
        /// </summary>
        public bool StripDnsSuffix { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout of waiting for ARP responses from network hosts.
        /// </summary>
        public TimeSpan ArpTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets an OUI matcher for matching MAC addresses to the corresponding organization name 
        /// (NIC manufacturer) using the IEEE OUI dataset.
        /// </summary>
        public IOuiMatcher? OuiMatcher { get; set; }
    }
}