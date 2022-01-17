using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace WhatsOnLan.Core
{
    public class IpScanResult
    {
        public IPAddress IpAddress { get; internal set; } = IPAddress.None;
        public PhysicalAddress MacAddress { get; internal set; } = PhysicalAddress.None;
        public string Hostname { get; internal set; } = string.Empty;
        public bool RespondedToPing { get; internal set; } = false;
        public string Manufacturer { get; set; } = string.Empty;

        public bool RespondedToArp => !MacAddress.Equals(PhysicalAddress.None);
        public bool IsOnline => RespondedToArp || RespondedToPing;
        public bool HasManufacturer => !string.IsNullOrWhiteSpace(Manufacturer);
        public bool HasHostname => !string.IsNullOrWhiteSpace(Hostname);

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append(IpAddress);

            if (IsOnline)
            {
                if (RespondedToArp)
                {
                    stringBuilder.Append('\t');
                    stringBuilder.Append(MacAddress);
                }

                if (HasManufacturer)
                {
                    stringBuilder.Append('\t');
                    stringBuilder.Append(Manufacturer);
                }

                if (HasHostname)
                {
                    stringBuilder.Append('\t');
                    stringBuilder.Append(Hostname);
                }

                if (RespondedToPing)
                    stringBuilder.Append("\t[Pings]");
            }
            else
                stringBuilder.Append("\t[Offline]");

            return stringBuilder.ToString();
        }
    }
}