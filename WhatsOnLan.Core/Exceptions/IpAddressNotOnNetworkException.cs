using System.Net;

namespace YonatanMankovich.WhatsOnLan.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when an IP address in not on a specified network.
    /// </summary>
    public class IpAddressNotOnNetworkException : Exception
    {
        /// <summary>
        /// The IP address.
        /// </summary>
        public IPAddress IpAddress { get; set; }

        /// <summary>
        /// The network address.
        /// </summary>
        public IPAddress Network { get; set; }

        /// <summary>
        /// The subnet mask.
        /// </summary>
        public IPAddress Subnet { get; set; }

        /// <summary>
        /// Creates an instance of the <see cref="IpAddressNotOnNetworkException"/> class.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <param name="network">The network address.</param>
        /// <param name="subnet">The subnet mask.</param>
        public IpAddressNotOnNetworkException(IPAddress address, IPAddress network, IPAddress subnet)
            : base($"The network {network} ({subnet}) does not have the IP address of {address}.")
        {
            IpAddress = address;
            Network = network;
            Subnet = subnet;
        }

        /// <summary>
        /// Creates an instance of the <see cref="IpAddressNotOnNetworkException"/> class.
        /// </summary>
        /// <param name="address">The IP address.</param>
        public IpAddressNotOnNetworkException(IPAddress address)
            : base($"The network does not have the IP address of {address}.")
        {
            IpAddress = address;
            Network = IPAddress.None;
            Subnet = IPAddress.None;
        }
    }
}