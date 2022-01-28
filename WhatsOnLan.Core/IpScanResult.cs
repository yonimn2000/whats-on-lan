using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace YonatanMankovich.WhatsOnLan.Core
{
    /// <summary>
    /// Represents a network scan result per IP address.
    /// </summary>
    public class IpScanResult
    {
        /// <summary>
        /// The <see cref="IPAddress"/> of the scanned target device.
        /// </summary>
        public IPAddress IpAddress { get; internal set; } = IPAddress.None;

        /// <summary>
        /// The <see cref="PhysicalAddress"/> of the scanned target device.
        /// </summary>
        public PhysicalAddress MacAddress { get; internal set; } = PhysicalAddress.None;

        /// <summary>
        /// The hostname of the scanned target device.
        /// </summary>
        public string Hostname { get; internal set; } = string.Empty;

        /// <summary>
        /// Indicates whether the scanned target device was sent an ARP request.
        /// </summary>
        public bool WasArpRequested { get; internal set; } = false;

        /// <summary>
        /// Indicates whether the scanned target device was pinged.
        /// </summary>
        public bool WasPinged { get; internal set; } = false;

        /// <summary>
        /// Indicates whether the scanned target device responded to ping.
        /// </summary>
        public bool RespondedToPing { get; internal set; } = false;

        /// <summary>
        /// The manufacturer of the NIC of the scanned target device as determined by IEEE OUI.
        /// </summary>
        public string Manufacturer { get; internal set; } = string.Empty;

        /// <summary>
        /// Indicates whether the scanned target device responded to the ARP request if sent.
        /// </summary>
        public bool RespondedToArp => !MacAddress.Equals(PhysicalAddress.None);

        /// <summary>
        /// Indicates whether the scanned target device was online during the scan as determined by the ping or ARP responses.
        /// </summary>
        public bool IsOnline => RespondedToArp || RespondedToPing;

        /// <summary>
        /// Indicates whether the NIC manufacturer of the scanned target device was determined.
        /// </summary>
        public bool HasManufacturer => !string.IsNullOrWhiteSpace(Manufacturer);

        /// <summary>
        /// Indicates whether the hostname of the scanned target device was determined.
        /// </summary>
        public bool HasHostname => !string.IsNullOrWhiteSpace(Hostname);

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(IpAddress);

            if (IsOnline)
            {
                if (RespondedToArp)
                {
                    stringBuilder.Append(' ');
                    stringBuilder.Append(MacAddress);
                }

                if (HasManufacturer)
                {
                    stringBuilder.Append(' ');
                    stringBuilder.Append(Manufacturer);
                }

                if (HasHostname)
                {
                    stringBuilder.Append(' ');
                    stringBuilder.Append(Hostname);
                }

                if (RespondedToPing)
                    stringBuilder.Append(" [Pings]");
            }
            else
                stringBuilder.Append(" [Offline]");

            return stringBuilder.ToString();
        }
    }
}