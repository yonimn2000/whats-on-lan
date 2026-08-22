using YonatanMankovich.WhatsOnLan.Core.OUI;

namespace YonatanMankovich.WhatsOnLan.Core
{
    /// <summary>
    /// Represents the options of a <see cref="NetworkScanner"/>.
    /// </summary>
    public class NetworkScannerOptions
    {
        /// <summary>
        /// The default maximum number of concurrent hostname resolutions.
        /// </summary>
        public const int DefaultHostnameResolverMaxDegreeOfParallelism = 128;

        /// <summary>
        /// The default maximum number of hosts allowed in a full network scan.
        /// </summary>
        public const int DefaultMaxScannableHosts = 65_534;

        /// <summary>
        /// The default maximum number of concurrent ping operations.
        /// </summary>
        public const int DefaultPingerMaxDegreeOfParallelism = 256;

        /// <summary>
        /// The default number of ARP probe attempts.
        /// </summary>
        public const int DefaultArpRetries = 5;

        /// <summary>
        /// The default number of ping attempts.
        /// </summary>
        public const int DefaultPingerRetries = 2;

        /// <summary>
        /// The default number of hostname-resolution attempts.
        /// </summary>
        public const int DefaultHostnameResolverRetries = 1;

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
        /// Indicates whether to shuffle IP addresses during the scan.
        /// </summary>
        public bool ShuffleIpAddresses { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of hosts a full scan may enumerate.
        /// </summary>
        public int MaxScannableHosts { get; set; } = DefaultMaxScannableHosts;

        /// <summary>
        /// Gets or sets the timeout of waiting for ARP responses from network hosts.
        /// </summary>
        public TimeSpan ArpTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the timeout of waiting for hostname resolution responses.
        /// </summary>
        public TimeSpan HostnameResolverTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum number of concurrent hostname resolutions.
        /// </summary>
        public int HostnameResolverMaxDegreeOfParallelism { get; set; } = DefaultHostnameResolverMaxDegreeOfParallelism;

        /// <summary>
        /// Gets or sets the timeout of waiting for ping responses.
        /// </summary>
        public TimeSpan PingerTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum number of concurrent ping operations.
        /// </summary>
        public int PingerMaxDegreeOfParallelism { get; set; } = DefaultPingerMaxDegreeOfParallelism;

        /// <summary>
        /// Gets or sets an OUI matcher for matching MAC addresses to the corresponding organization name 
        /// (NIC manufacturer) using the IEEE OUI dataset.
        /// </summary>
        public IOuiMatcher? OuiMatcher { get; set; }

        /// <summary>
        /// Gets or sets the number of ARP probe attempts.
        /// </summary>
        public int ArpRetries { get; set; } = DefaultArpRetries;

        /// <summary>
        /// Gets or sets the number of ping attempts.
        /// </summary>
        public int PingerRetries { get; set; } = DefaultPingerRetries;

        /// <summary>
        /// Gets or sets the number of hostname-resolution attempts.
        /// </summary>
        public int HostnameResolverRetries { get; set; } = DefaultHostnameResolverRetries;
    }
}
